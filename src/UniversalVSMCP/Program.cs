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
/// UniversalVSMCP - MCP Server for Visual Studio 2026/2022
/// Bridges AI Agents to Visual Studio via DTE/OM
/// 
/// This is a native .NET MCP Server that can be:
/// - Run directly: dotnet run
/// - Installed as tool: dotnet tool install -g UniversalVSMCP
/// - Published to NuGet for uvx/npx discovery
/// - Registered in MCP Registry for VS 2026 one-click install
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("============================================================");
        Console.WriteLine("         UniversalVSMCP (UVM) - VS 2026/2022 MCP Server         ");
        Console.WriteLine("                  AI Agent <-> Visual Studio Bridge               ");
        Console.WriteLine("============================================================");
        
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register VS Connection Manager (Singleton)
                services.AddSingleton<IVsConnectionManager, VsConnectionManager>();
                
                // Register tool sets (Scoped or Transient as appropriate)
                services.AddScoped<SolutionTools>();
                services.AddScoped<ProjectTools>();
                services.AddScoped<FileTools>();
                services.AddScoped<BuildTools>();
                services.AddScoped<DebugTools>();
                
                // Register MCP Server with stdio transport
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
}
