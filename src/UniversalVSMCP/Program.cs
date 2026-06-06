using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniversalVSMCP.IdeAbstraction;
using UniversalVSMCP.IdeAdapters;
using UniversalVSMCP.IdeRouting;
using UniversalVSMCP.Tools;
using UniversalVSMCP.Security;

namespace UniversalVSMCP;

/// <summary>
/// UniversalVSMCP (UVM) v26.2.0-rc3 - Unified MCP Server for Visual Studio and VS Code
/// 
/// Architecture: AI Agent �?MCP �?IdeRouter �?IIdeAdapter �?VS/VS Code
/// 
/// Transport Modes:
///   --stdio                    # Standard input/output
///   --http [port]              # HTTP/SSE mode
///   --hybrid                   # Both modes
/// 
/// Features:
///   - Multi-IDE support (VS 2022/2026, VS Code)
///   - Unified IIdeAdapter interface
///   - Smart routing between IDEs
///   - Security framework (Trust, Permission, Audit)
/// </summary>
public class Program
{
    private static HttpMcpServer? _httpServer;
    
    public static async Task<int> Main(string[] args)
    {
        PrintBanner();
        
        var config = ParseArgs(args);
        
        if (config.ShowHelp)
        {
            ShowHelp();
            return 0;
        }

        if (config.Verify)
        {
            return await VerifyConnectionAsync(config);
        }

        try
        {
            var host = BuildHost(config);
            
            // Initialize IDE adapters
            await InitializeIdeAdapters(host, config);
            
            // Start transports
            if (config.TransportMode == TransportMode.Stdio || config.TransportMode == TransportMode.Hybrid)
            {
                await host.RunAsync();
            }
            else if (config.TransportMode == TransportMode.Http)
            {
                await StartHttpServerAsync(host, config.HttpPort);
            }
            else if (config.TransportMode == TransportMode.Hybrid)
            {
                _ = host.RunAsync();
                await StartHttpServerAsync(host, config.HttpPort);
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

    private static void PrintBanner()
    {
        // Only print banner for non-stdio modes to avoid interfering with MCP protocol
        // Or print to stderr
        Console.Error.WriteLine("=================================================================");
        Console.Error.WriteLine("     UniversalVSMCP (UVM) v26.2.0-RC1 - Unified MCP Server");
        Console.Error.WriteLine("        AI Agent �?VS 2022/2026 | VS Code Bridge");
        Console.Error.WriteLine("=================================================================");
    }

    /// <summary>
    /// Build host with unified architecture
    /// </summary>
    private static IHost BuildHost(ServerConfig config)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddSingleton<OperationAuditor>();
                services.AddSingleton<WorkspaceTrustManager>();
                services.AddSingleton<UserConfirmationManager>();
                services.AddSingleton<ToolPermissionManager>();
                services.AddSingleton<PromptInjectionDetector>();
                
                // IDE Routing
                services.AddSingleton<IdeRouter>(sp => 
                {
                    var logger = sp.GetRequiredService<ILogger<IdeRouter>>();
                    return new IdeRouter(logger, RoutingStrategy.Auto);
                });
                
                // IDE Adapters (registered as singletons, but not connected until needed)
                services.AddSingleton<VsDteAdapter>(sp => 
                {
                    var logger = sp.GetRequiredService<ILogger<VsDteAdapter>>();
                    return new VsDteAdapter(logger);
                });
                
                services.AddSingleton<VsCodeAdapter>(sp => 
                {
                    var logger = sp.GetRequiredService<ILogger<VsCodeAdapter>>();
                    return new VsCodeAdapter(logger);
                });
                
                // VS Connection Manager
                services.AddSingleton<IVsConnectionManager, VsConnectionManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<VsConnectionManager>>();
                    return new VsConnectionManager(logger);
                });
                
                // HTTP Server
                services.AddSingleton<HttpMcpServer>(sp => 
                {
                    var logger = sp.GetRequiredService<ILogger<HttpMcpServer>>();
                    var vsManager = sp.GetRequiredService<IVsConnectionManager>();
                    var tools = sp.GetServices<McpServerTool>();
                    return new HttpMcpServer(logger, vsManager, tools);
                });
                
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    if (!string.IsNullOrEmpty(config.LogFile))
                    {
                        builder.AddProvider(new FileLoggerProvider(config.LogFile));
                    }
                });
                
                // MCP Server with unified tools
                if (config.TransportMode == TransportMode.Stdio || config.TransportMode == TransportMode.Hybrid)
                {
                    services.AddMcpServer(options =>
                    {
                        options.ServerInfo.Name = "universal-vsmcp";
                        options.ServerInfo.Version = "26.2.0-rc3";
                    })
                    .WithTools<SolutionTools>()
                    .WithTools<BuildTools>()
                    .WithTools<DebugTools>()
                    .WithTools<FileTools>()
                    .WithTools<ProjectTools>()
                    .WithTools<DiagnosticTools>();
                }
            })
            .Build();
    }

    /// <summary>
    /// Initialize IDE adapters and register with router
    /// </summary>
    private static async Task InitializeIdeAdapters(IHost host, ServerConfig config)
    {
        var router = host.Services.GetRequiredService<IdeRouter>();
        var vsAdapter = host.Services.GetRequiredService<VsDteAdapter>();
        var vscodeAdapter = host.Services.GetRequiredService<VsCodeAdapter>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        // Register adapters
        router.RegisterAdapter("vs2022", vsAdapter);
        router.RegisterAdapter("vscode", vscodeAdapter);
        
        // Set default based on config or auto-detect
        if (config.VsVersion?.StartsWith("17") == true || config.VsVersion?.StartsWith("18") == true)
        {
            router.SetDefaultAdapter("vs2022");
            logger.LogInformation("Default IDE set to Visual Studio");
        }
        else
        {
            // Try to auto-connect to preferred IDE
            logger.LogInformation("Auto-detecting IDE...");
            
            // Try VS first for .sln files
            if (!string.IsNullOrEmpty(config.SolutionPath) && config.SolutionPath.EndsWith(".sln"))
            {
                router.SetDefaultAdapter("vs2022");
            }
            else
            {
                // Prefer VS Code for general use
                router.SetDefaultAdapter("vscode");
            }
        }
        
        // Auto-connect if solution provided
        if (!string.IsNullOrEmpty(config.SolutionPath))
        {
            var criteria = new RoutingCriteria
            {
                SolutionPath = config.SolutionPath,
                AutoDiscover = true
            };
            
            logger.LogInformation("Connecting to IDE for solution: {Path}", config.SolutionPath);
            var result = await router.ConnectAsync(criteria);
            
            if (result.Success)
            {
                logger.LogInformation("Connected to {IdeName} (Instance: {InstanceId})",
                    result.Adapter?.IdeName, result.InstanceId);
            }
            else
            {
                logger.LogWarning("Failed to connect to IDE: {Error}", result.ErrorMessage);
            }
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Start HTTP server
    /// </summary>
    private static async Task StartHttpServerAsync(IHost host, int port)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var httpServer = host.Services.GetRequiredService<HttpMcpServer>();
        
        logger.LogInformation("Starting HTTP server on port {Port}...", port);
        await httpServer.StartAsync(port);
        
        Console.WriteLine($"\n�?HTTP Server ready at: http://localhost:{port}/sse");
        Console.WriteLine($"�?Health check:       http://localhost:{port}/health");
        Console.WriteLine($"�?Server info:        http://localhost:{port}/info");
        Console.WriteLine($"�?Tools list:         http://localhost:{port}/tools");
        Console.WriteLine("\nPress Ctrl+C to stop the server.");
        
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// Verify IDE connection
    /// </summary>
    private static async Task<int> VerifyConnectionAsync(ServerConfig config)
    {
        Console.WriteLine("Verifying IDE connection...\n");
        
        var host = BuildHost(config);
        var router = host.Services.GetRequiredService<IdeRouter>();
        
        // Try to connect
        var criteria = new RoutingCriteria 
        { 
            AutoDiscover = true,
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        var result = await router.ConnectAsync(criteria);
        
        if (result.Success && result.Adapter != null)
        {
            Console.WriteLine($"�?Connected to: {result.Adapter.IdeName} {result.Adapter.IdeVersion}");
            Console.WriteLine($"  Instance ID: {result.InstanceId}");
            
            var solution = await result.Adapter.GetSolutionAsync();
            if (solution?.IsOpen == true)
            {
                Console.WriteLine($"  Solution: {solution.Name}");
                Console.WriteLine($"  Projects: {solution.ProjectCount}");
            }
            
            return 0;
        }
        else
        {
            Console.WriteLine($"�?Connection failed: {result.ErrorMessage}");
            Console.WriteLine("  Make sure VS or VS Code is running with a solution open.");
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
                    
                case "--solution":
                    if (i + 1 < args.Length)
                    {
                        config.SolutionPath = args[i + 1];
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
                    
                case "--log-file":
                    if (i + 1 < args.Length)
                    {
                        config.LogFile = args[i + 1];
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
UniversalVSMCP (UVM) - Unified MCP Server for Visual Studio and VS Code

USAGE:
    universal-vsmcp [OPTIONS]

OPTIONS:
    --stdio                    Run in stdio mode (default)
    --http [PORT]              Run HTTP server on port (default: 5000)
    --hybrid [PORT]            Run both stdio and HTTP
    --solution <PATH>          Open specific solution/workspace
    --vs-version <VERSION>     Target specific VS version (17.x, 18.x)
    --log-file <PATH>          Write logs to file
    --verify                   Verify IDE connection
    --help, -h                 Show this help

EXAMPLES:
    # Stdio mode (for Claude/Cursor)
    universal-vsmcp --stdio

    # HTTP mode (for VS Code or VS 2026 MCP Manager)
    universal-vsmcp --http 5000

    # Hybrid mode (both)
    universal-vsmcp --hybrid 5000 --solution C:\Projects\MyApp.sln

    # Verify connection
    universal-vsmcp --verify

ENDPOINTS (HTTP mode):
    GET  /sse        - SSE endpoint for MCP
    GET  /health     - Health check
    GET  /info       - Server information
    GET  /tools      - List available tools
    POST /tools/call - Call a tool

SUPPORTED IDEs:
    - Visual Studio 2022 (v17.x)
    - Visual Studio 2026 (v18.x)
    - VS Code (v1.90+)

For more information: https://github.com/StarsailsClover/UniversalVSMCP
");
    }
}
