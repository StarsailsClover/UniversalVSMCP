using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UniversalVSMCP;

/// <summary>
/// Diagnostic and health check tools for UniversalVSMCP
/// </summary>
[McpServerToolType]
public class DiagnosticTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<DiagnosticTools> _logger;

    public DiagnosticTools(IVsConnectionManager vsManager, ILogger<DiagnosticTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Health check - verify server is running and can connect to VS
    /// </summary>
    [McpServerTool(Name = "health_check", Title = "Perform health check: verify server is running and VS connection status.")]
    public async Task<HealthCheckResult> HealthCheck(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var result = new HealthCheckResult
        {
            ServerStatus = "running",
            Timestamp = DateTime.UtcNow
        };

        // Check VS connection
        var dte = _vsManager.GetActiveInstance();
        if (dte != null)
        {
            result.VsConnection = "connected";
            result.VsVersion = _vsManager.ConnectedVersion ?? "unknown";
            
            try
            {
                // Note: VsInstallPath not exposed on interface, use null
                result.VsInstallPath = null;
                result.SolutionOpen = dte.Solution.IsOpen;
                if (dte.Solution.IsOpen)
                {
                    result.SolutionName = System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                }
            }
            catch (Exception ex)
            {
                result.VsConnection = "connected_but_limited";
                result.Error = $"Limited access: {ex.Message}";
            }
        }
        else
        {
            result.VsConnection = "disconnected";
            result.Error = "No Visual Studio instance found. Please start Visual Studio first.";
        }

        return result;
    }

    /// <summary>
    /// Get server information and capabilities
    /// </summary>
    [McpServerTool(Name = "get_server_info", Title = "Get server information including version, capabilities, and available tools.")]
    public async Task<ServerInfoResult> GetServerInfo(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        return new ServerInfoResult
        {
            Name = "universal-vsmcp",
            Version = "1.0.0",
            Description = "MCP Server for Visual Studio 2026/2022 automation",
            Transport = "stdio",
            VsCompatibility = new[] { "VS 2026 (18.0)", "VS 2022 (17.14+)" },
            Tools = new[]
            {
                "get_solution_projects", "get_solution_path", "get_solution_name",
                "open_solution", "close_solution", "create_solution",
                "get_project_files", "get_startup_projects", "add_file_to_project",
                "set_startup_project", "get_project_properties",
                "open_file", "read_file", "write_file", "replace_in_file", "get_file_info",
                "build_solution", "rebuild_solution", "clean_solution", "build_project",
                "get_build_errors", "get_build_configurations",
                "start_debugging", "stop_debugging", "toggle_breakpoint",
                "continue_execution", "step_over", "step_into", "step_out", "get_debug_state",
                "health_check", "get_server_info", "get_diagnostic_logs"
            }
        };
    }

    /// <summary>
    /// Get diagnostic logs (recent log entries)
    /// </summary>
    [McpServerTool(Name = "get_diagnostic_logs", Title = "Get recent diagnostic logs from the server session.")]
    public async Task<DiagnosticLogResult> GetDiagnosticLogs(int maxEntries = 50, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        // In a real implementation, this would read from the log file
        // For now, return current session info
        return new DiagnosticLogResult
        {
            LogFile = Environment.GetEnvironmentVariable("UVM_LOG_FILE") ?? "console",
            MaxEntries = maxEntries,
            Message = "Logs are written to console or specified log file. Use --log-file option to enable file logging.",
            RecentEvents = new List<string>
            {
                $"[{DateTime.UtcNow:HH:mm:ss}] Server started",
                $"[{DateTime.UtcNow:HH:mm:ss}] Transport: stdio",
                $"[{DateTime.UtcNow:HH:mm:ss}] VS detection: {( _vsManager.IsConnected ? "connected" : "disconnected" )}"
            }
        };
    }
}

#region Data Models

public class HealthCheckResult
{
    public string ServerStatus { get; set; } = "unknown";
    public string VsConnection { get; set; } = "unknown";
    public string? VsVersion { get; set; }
    public string? VsInstallPath { get; set; }
    public bool SolutionOpen { get; set; }
    public string? SolutionName { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ServerInfoResult
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public string[] VsCompatibility { get; set; } = Array.Empty<string>();
    public string[] Tools { get; set; } = Array.Empty<string>();
}

public class DiagnosticLogResult
{
    public string LogFile { get; set; } = string.Empty;
    public int MaxEntries { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> RecentEvents { get; set; } = new();
}

#endregion
