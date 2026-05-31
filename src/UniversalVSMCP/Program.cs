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
/// Usage:
///   universal-vsmcp --stdio                    # Default: stdio transport
///   universal-vsmcp --stdio --log-file uv.log  # With file logging
///   universal-vsmcp --verify                   # Verify VS connection
///   universal-vsmcp --help                     # Show help
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine("         UniversalVSMCP (UVM) v26.0.0-20260531-UVM - VS 2026/2022 MCP Server         ");
        Console.WriteLine("                  AI Agent <-> Visual Studio Bridge               ");
        Console.WriteLine("=================================================================");
        
        var config = ParseArgs(args);
        
        // Handle verify command
        if (config.Verify)
        {
            return await VerifyConnectionAsync(config);
        }

        Console.WriteLine($"Transport:  {config.TransportMode}");
        Console.WriteLine($"VS Target:  {config.VsVersion ?? "Auto-detect latest instance"}");
        Console.WriteLine($"Log File:   {config.LogFile ?? "console only"}");
        Console.WriteLine();

        try
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register VS Connection Manager (Singleton)
                    services.AddSingleton<IVsConnectionManager, VsConnectionManager>();
                    
                    // Register tool sets
                    services.AddScoped<SolutionTools>();
                    services.AddScoped<ProjectTools>();
                    services.AddScoped<FileTools>();
                    services.AddScoped<BuildTools>();
                    services.AddScoped<DebugTools>();
                    services.AddScoped<DiagnosticTools>();
                    
                    // Register MCP Server with stdio transport
                    services.AddMcpServer(options =>
                    {
                        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                        {
                            Name = "universal-vsmcp",
                            Version = "4.0.0"
                        };
                        options.ProtocolVersion = "2024-11-05";
                    })
                    .WithToolsFromAssembly(typeof(Program).Assembly);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(options =>
                    {
                        options.TimestampFormat = "[HH:mm:ss] ";
                    });
                    
                    if (!string.IsNullOrEmpty(config.LogFile))
                    {
                        logging.AddProvider(new FileLoggerProvider(config.LogFile));
                    }
                    
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return 1;
        }
        
        return 0;
    }

    private static async Task<int> VerifyConnectionAsync(ServerConfig config)
    {
        Console.WriteLine("=== Connection Verification ===");
        Console.WriteLine();
        
        // Create a simple service provider to test VS connection
        var services = new ServiceCollection();
        services.AddSingleton<IVsConnectionManager, VsConnectionManager>();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        var provider = services.BuildServiceProvider();
        var vsManager = provider.GetRequiredService<IVsConnectionManager>();
        
        Console.WriteLine("Checking Visual Studio connection...");
        var connected = await vsManager.ConnectAsync(config.VsVersion);
        
        if (connected)
        {
            Console.WriteLine($"✓ Connected to Visual Studio {vsManager.ConnectedVersion}");
            Console.WriteLine($"  VS Path: {vsManager.VsInstallPath ?? "Not detected"}");
            
            // Try to get solution info
            var dte = vsManager.GetActiveInstance();
            if (dte?.Solution != null)
            {
                if (dte.Solution.IsOpen)
                {
                    Console.WriteLine($"  Solution: {System.IO.Path.GetFileName(dte.Solution.FullName)}");
                }
                else
                {
                    Console.WriteLine("  Solution: No solution open");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("✓ Connection verified successfully!");
            return 0;
        }
        else
        {
            Console.WriteLine("✗ Failed to connect to Visual Studio");
            Console.WriteLine();
            Console.WriteLine("Troubleshooting:");
            Console.WriteLine("1. Ensure Visual Studio 2026/2022 is running");
            Console.WriteLine("2. Open at least one solution (.sln)");
            Console.WriteLine("3. Check that VS is not running as Administrator");
            Console.WriteLine("4. Try: universal-vsmcp --stdio --vs-version 18.0");
            return 1;
        }
    }

    private static ServerConfig ParseArgs(string[] args)
    {
        var config = new ServerConfig
        {
            TransportMode = "stdio",
            LogFile = Environment.GetEnvironmentVariable("UVM_LOG_FILE"),
            Verify = false
        };
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--vs-version":
                case "-v":
                    if (i + 1 < args.Length)
                        config.VsVersion = args[++i];
                    break;
                case "--log-file":
                case "--log":
                    if (i + 1 < args.Length)
                        config.LogFile = args[++i];
                    break;
                case "--verify":
                    config.Verify = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }
        
        return config;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("UniversalVSMCP - Visual Studio 2026/2022 MCP Server v4.0.0");
        Console.WriteLine();
        Console.WriteLine("Usage: universal-vsmcp [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --stdio              Use stdio transport (default)");
        Console.WriteLine("  --vs-version <ver>   Target VS version (e.g., 18.0 for VS 2026)");
        Console.WriteLine("  --log-file <path>    Log to file (default: console only)");
        Console.WriteLine("  --verify             Verify VS connection and exit");
        Console.WriteLine("  --help               Show this help");
        Console.WriteLine();
        Console.WriteLine("Environment Variables:");
        Console.WriteLine("  UVM_LOG_FILE         Path to log file");
        Console.WriteLine("  VS_AUTO_DETECT       Set to 'true' to auto-detect VS instance");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  universal-vsmcp --stdio                    # Run MCP server");
        Console.WriteLine("  universal-vsmcp --stdio --log-file uv.log  # With logging");
        Console.WriteLine("  universal-vsmcp --verify                   # Verify VS connection");
        Console.WriteLine("  dotnet run -- --stdio --vs-version 18.0    # From source");
    }
}

/// <summary>
/// Server configuration options
/// </summary>
public class ServerConfig
{
    public string TransportMode { get; set; } = "stdio";
    public string? VsVersion { get; set; }
    public string? LogFile { get; set; }
    public bool Verify { get; set; }
}
