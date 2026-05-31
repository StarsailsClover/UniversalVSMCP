using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniversalVSMCP;

namespace UniversalVSMCP;

/// <summary>
/// UniversalVSMCP (UVM) - MCP Server for Visual Studio 2026/2022
/// Bridges AI Agents to Visual Studio via DTE/OM
/// 
/// Transport Modes:
///   --stdio                    # Standard input/output (default)
///   --http [port]              # HTTP/SSE mode on port (default: 5000)
///   --hybrid                   # Both stdio and HTTP
/// 
/// VS 2026 Configuration:
///   URL: http://localhost:5000/sse
/// 
/// Usage:
///   universal-vsmcp --stdio                    # stdio transport
///   universal-vsmcp --http 5000                # HTTP on port 5000
///   universal-vsmcp --hybrid                 # Both modes
///   universal-vsmcp --verify                 # Verify VS connection
///   universal-vsmcp --help                   # Show help
/// </summary>
public class Program
{
    private static HttpMcpServer? _httpServer;
    
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine("         UniversalVSMCP (UVM) v26.0.2-20260531-UVM - VS 2026/2022 MCP Server         ");
        Console.WriteLine("                  AI Agent <-> Visual Studio Bridge               ");
        Console.WriteLine("=================================================================");
        
        var config = ParseArgs(args);
        
        // Handle verify command
        if (config.Verify)
        {
            return await VerifyConnectionAsync(config);
        }

        // Show help
        if (config.ShowHelp)
        {
            ShowHelp();
            return 0;
        }

        Console.WriteLine($"Transport:  {config.TransportMode}");
        if (config.TransportMode == TransportMode.Http || config.TransportMode == TransportMode.Hybrid)
        {
            Console.WriteLine($"HTTP Port:  {config.HttpPort}");
            Console.WriteLine($"VS 2026 URL: http://localhost:{config.HttpPort}/sse");
        }
        Console.WriteLine($"VS Target:  {config.VsVersion ?? "Auto-detect latest instance"}");
        Console.WriteLine($"Log File:   {config.LogFile ?? "console only"}");
        Console.WriteLine();

        try
        {
            // Build host based on transport mode
            var host = BuildHost(config);
            
            // Start stdio mode
            if (config.TransportMode == TransportMode.Stdio || config.TransportMode == TransportMode.Hybrid)
            {
                Console.WriteLine("Starting stdio transport...");
                _ = host.RunAsync();
            }
            
            // Start HTTP mode
            if (config.TransportMode == TransportMode.Http || config.TransportMode == TransportMode.Hybrid)
            {
                Console.WriteLine($"Starting HTTP transport on port {config.HttpPort}...");
                await StartHttpServerAsync(host, config.HttpPort);
            }
            
            // Keep running
            if (config.TransportMode == TransportMode.Http)
            {
                Console.WriteLine("\nPress Ctrl+C to stop the server.");
                await Task.Delay(Timeout.Infinite);
            }
            else
            {
                await host.RunAsync();
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    /// <summary>
    /// Build host based on configuration
    /// </summary>
    private static IHost BuildHost(ServerConfig config)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register VS Connection Manager (Singleton for DTE reuse)
                services.AddSingleton<IVsConnectionManager, VsConnectionManager>();
                
                // Register tools
                services.AddSingleton<SolutionTools>();
                services.AddSingleton<ProjectTools>();
                services.AddSingleton<FileTools>();
                services.AddSingleton<BuildTools>();
                services.AddSingleton<DebugTools>();
                services.AddSingleton<DiagnosticTools>();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    if (!string.IsNullOrEmpty(config.LogFile))
                    {
                        builder.AddProvider(new FileLoggerProvider(config.LogFile));
                    }
                });
                
                // Register MCP Server for stdio mode
                if (config.TransportMode == TransportMode.Stdio || config.TransportMode == TransportMode.Hybrid)
                {
                    services.AddMcpServer(options =>
                    {
                        options.ServerInfo = new ServerInfo
                        {
                            Name = "universal-vsmcp",
                            Version = "26.0.0"
                        };
                    })
                    .WithTools<SolutionTools>()
                    .WithTools<ProjectTools>()
                    .WithTools<FileTools>()
                    .WithTools<BuildTools>()
                    .WithTools<DebugTools>()
                    .WithTools<DiagnosticTools>();
                }
            })
            .Build();
    }

    /// <summary>
    /// Start HTTP server
    /// </summary>
    private static async Task StartHttpServerAsync(IHost host, int port)
    {
        var logger = host.Services.GetRequiredService<ILogger<HttpMcpServer>>();
        var vsManager = host.Services.GetRequiredService<IVsConnectionManager>();
        
        // Get all tools
        var tools = new List<McpServerTool>();
        tools.AddRange(GetToolDefinitions<SolutionTools>());
        tools.AddRange(GetToolDefinitions<ProjectTools>());
        tools.AddRange(GetToolDefinitions<FileTools>());
        tools.AddRange(GetToolDefinitions<BuildTools>());
        tools.AddRange(GetToolDefinitions<DebugTools>());
        tools.AddRange(GetToolDefinitions<DiagnosticTools>());
        
        _httpServer = new HttpMcpServer(logger, vsManager, tools);
        await _httpServer.StartAsync(port);
        
        Console.WriteLine($"\n✓ HTTP Server ready at: http://localhost:{port}/sse");
        Console.WriteLine($"✓ Health check:       http://localhost:{port}/health");
        Console.WriteLine($"✓ Server info:        http://localhost:{port}/info");
        Console.WriteLine($"✓ Tools list:         http://localhost:{port}/tools");
    }

    /// <summary>
    /// Get tool definitions from type
    /// </summary>
    private static IEnumerable<McpServerTool> GetToolDefinitions<T>() where T : class
    {
        var type = typeof(T);
        var methods = type.GetMethods();
        
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttributes(typeof(McpServerToolAttribute), false)
                .Cast<McpServerToolAttribute>()
                .FirstOrDefault();
            
            if (attr != null)
            {
                yield return new McpServerTool
                {
                    Name = attr.Name,
                    Description = attr.Title,
                    InputSchema = GenerateInputSchema(method),
                    OutputSchema = GenerateOutputSchema(method)
                };
            }
        }
    }

    /// <summary>
    /// Generate JSON schema for method parameters
    /// </summary>
    private static object GenerateInputSchema(System.Reflection.MethodInfo method)
    {
        // Simplified schema generation
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        
        foreach (var param in method.GetParameters())
        {
            if (param.Name == "ct" || param.Name == "cancellationToken") continue;
            
            properties[param.Name!] = new
            {
                type = GetJsonType(param.ParameterType),
                description = $"Parameter {param.Name}"
            };
            
            if (!param.IsOptional && !param.HasDefaultValue)
            {
                required.Add(param.Name!);
            }
        }
        
        return new
        {
            type = "object",
            properties,
            required
        };
    }

    /// <summary>
    /// Generate JSON schema for return type
    /// </summary>
    private static object GenerateOutputSchema(System.Reflection.MethodInfo method)
    {
        return new
        {
            type = "object",
            description = $"Return type: {method.ReturnType.Name}"
        };
    }

    /// <summary>
    /// Get JSON type from CLR type
    /// </summary>
    private static string GetJsonType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "number";
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))) return "array";
        return "object";
    }

    /// <summary>
    /// Verify VS connection
    /// </summary>
    private static async Task<int> VerifyConnectionAsync(ServerConfig config)
    {
        Console.WriteLine("Verifying Visual Studio connection...\n");
        
        var host = BuildHost(config);
        var vsManager = host.Services.GetRequiredService<IVsConnectionManager>();
        
        var connected = await vsManager.ConnectAsync(config.VsVersion);
        
        if (connected)
        {
            Console.WriteLine("✓ Visual Studio connection successful!");
            Console.WriteLine($"  Version: {vsManager.ConnectedVersion}");
            return 0;
        }
        else
        {
            Console.WriteLine("✗ Visual Studio connection failed!");
            Console.WriteLine("  Make sure Visual Studio is running with a solution open.");
            return 1;
        }
    }

    /// <summary>
    /// Parse command line arguments
    /// </summary>
    private static ServerConfig ParseArgs(string[] args)
    {
        var config = new ServerConfig();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--stdio":
                    config.TransportMode = TransportMode.Stdio;
                    break;
                    
                case "--http":
                    config.TransportMode = TransportMode.Http;
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                    {
                        config.HttpPort = port;
                        i++;
                    }
                    break;
                    
                case "--hybrid":
                    config.TransportMode = TransportMode.Hybrid;
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int hybridPort))
                    {
                        config.HttpPort = hybridPort;
                        i++;
                    }
                    break;
                    
                case "--log-file":
                    if (i + 1 < args.Length)
                    {
                        config.LogFile = args[i + 1];
                        i++;
                    }
                    break;
                    
                case "--vs-version":
                    if (i + 1 < args.Length)
                    {
                        config.VsVersion = args[i + 1];
                        i++;
                    }
                    break;
                    
                case "--verify":
                    config.Verify = true;
                    break;
                    
                case "--help":
                case "-h":
                    config.ShowHelp = true;
                    break;
            }
        }
        
        return config;
    }

    /// <summary>
    /// Show help text
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine(@"
UniversalVSMCP (UVM) - MCP Server for Visual Studio 2026/2022

USAGE:
    universal-vsmcp [OPTIONS]

OPTIONS:
    --stdio                    Run in stdio mode (default)
    --http [PORT]              Run in HTTP/SSE mode (default port: 5000)
    --hybrid [PORT]            Run both stdio and HTTP modes
    --log-file <PATH>          Write logs to file
    --vs-version <VERSION>     Target specific VS version
    --verify                   Verify VS connection
    --help, -h                 Show this help

VS 2026 CONFIGURATION:
    For VS 2026 MCP Server Manager, use:
        Name: universal-vsmcp
        URL:  http://localhost:5000/sse

EXAMPLES:
    # Stdio mode (for Claude/Cursor)
    universal-vsmcp --stdio

    # HTTP mode (for VS 2026)
    universal-vsmcp --http 5000

    # Hybrid mode (both)
    universal-vsmcp --hybrid

ENDPOINTS:
    GET  /sse        - SSE endpoint for MCP (primary)
    GET  /health     - Health check
    GET  /info       - Server info
    GET  /tools      - List available tools
    POST /tools/call - Call a tool

For more information: https://github.com/StarsailsClover/UniversalVSMCP
");
    }
}
