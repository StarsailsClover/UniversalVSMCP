using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace UniversalVSMCP;

/// <summary>
/// UniversalVSMCP (UVM) - MCP Server for Visual Studio 2026/2022
/// Bridges AI Agents to Visual Studio via DTE/OM
/// 
/// Features:
/// - Native .NET 8.0 implementation for optimal performance
/// - Direct DTE/COM integration with Visual Studio
/// - 28 MCP tools for comprehensive VS automation
/// - MCP Registry compatible for VS 2026 one-click installation
/// - Global .NET tool packaging
/// 
/// Usage:
///   dotnet run -- --stdio          # Run with stdio transport
///   dotnet run -- --sse --port 6277  # Run with SSE transport
///   universal-vsmcp --stdio        # After global tool install
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine("         UniversalVSMCP (UVM) v1.0.0 - VS 2026/2022 MCP Server         ");
        Console.WriteLine("                  AI Agent <-> Visual Studio Bridge               ");
        Console.WriteLine("=================================================================");
        
        var config = ParseArgs(args);
        Console.WriteLine($"Transport:  {config.TransportMode}");
        Console.WriteLine($"VS Target:  {config.VsVersion ?? "Auto-detect latest instance"}");
        Console.WriteLine($"Port:       {config.Port}");
        Console.WriteLine();

        try
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register VS Connection Manager (Singleton - single VS connection)
                    services.AddSingleton<IVsConnectionManager, VsConnectionManager>();
                    
                    // Register tool sets (Scoped - new instance per request)
                    services.AddScoped<SolutionTools>();
                    services.AddScoped<ProjectTools>();
                    services.AddScoped<FileTools>();
                    services.AddScoped<BuildTools>();
                    services.AddScoped<DebugTools>();
                    
                    // Register MCP Server
                    services.AddMcpServer(options =>
                    {
                        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
                        {
                            Name = "universal-vsmcp",
                            Version = "1.0.0"
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
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static ServerConfig ParseArgs(string[] args)
    {
        var config = new ServerConfig
        {
            TransportMode = "stdio",
            Port = 6277
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
                case "--port":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                        config.Port = port;
                    break;
                case "--sse":
                    config.TransportMode = "sse";
                    break;
                case "--stdio":
                    config.TransportMode = "stdio";
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
        Console.WriteLine("UniversalVSMCP - Visual Studio 2026/2022 MCP Server");
        Console.WriteLine();
        Console.WriteLine("Usage: universal-vsmcp [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --stdio              Use stdio transport (default)");
        Console.WriteLine("  --sse                Use SSE transport");
        Console.WriteLine("  --port <number>      Port for SSE mode (default: 6277)");
        Console.WriteLine("  --vs-version <ver>   Target VS version (e.g., 17.0 for VS 2022)");
        Console.WriteLine("  --help               Show this help");
    }
}

/// <summary>
/// Server configuration options
/// </summary>
public class ServerConfig
{
    public string TransportMode { get; set; } = "stdio";
    public string? VsVersion { get; set; }
    public int Port { get; set; } = 6277;
}
