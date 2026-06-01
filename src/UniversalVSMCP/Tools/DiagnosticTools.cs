using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UniversalVSMCP.IdeAbstraction;
using UniversalVSMCP.IdeRouting;

namespace UniversalVSMCP.Tools;

/// <summary>
/// Diagnostic Tools - System health and status checking
/// </summary>
[McpServerToolType]
public class DiagnosticTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<DiagnosticTools> _logger;

    public DiagnosticTools(IdeRouter ideRouter, ILogger<DiagnosticTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Health check - verify all systems
    /// </summary>
    [McpServerTool(Name = "health_check",
        Title = "Check server and IDE health")]
    public async Task<HealthCheckResult> HealthCheck(CancellationToken ct = default)
    {
        try
        {
            var ides = _ideRouter.GetConnectedIdes();
            var ideStatuses = new System.Collections.Generic.List<IdeHealthStatus>();

            foreach (var ide in ides)
            {
                var isHealthy = false;
                try
                {
                    // Note: This would require getting the adapter instance
                    // For now, we check connection status only
                    isHealthy = ide.IsConnected;
                }
                catch { }

                ideStatuses.Add(new IdeHealthStatus
                {
                    InstanceId = ide.InstanceId,
                    Name = ide.Name,
                    Version = ide.Version,
                    IsConnected = ide.IsConnected,
                    IsHealthy = isHealthy
                });
            }

            return new HealthCheckResult
            {
                ServerStatus = "healthy",
                ConnectedIdeCount = ides.Count,
                IdeStatuses = ideStatuses.ToArray(),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new HealthCheckResult
            {
                ServerStatus = "error",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get server information
    /// </summary>
    [McpServerTool(Name = "get_server_info",
        Title = "Get server information and capabilities")]
    public ServerInfoResult GetServerInfo()
    {
        var ides = _ideRouter.GetConnectedIdes();

        return new ServerInfoResult
        {
            Name = "Universal VS MCP",
            Version = "26.0.3",
            Description = "Unified MCP Server for Visual Studio and VS Code",
            ConnectedIdes = ides.Select(i => new ConnectedIdeInfo
            {
                Name = i.Name,
                Version = i.Version,
                Capabilities = i.Capabilities
            }).ToArray(),
            SupportedOperations = new[]
            {
                "Solution Management",
                "Project Management",
                "File Operations",
                "Build Operations",
                "Debug Operations",
                "IDE Commands"
            }
        };
    }

    /// <summary>
    /// Get connected IDE status
    /// </summary>
    [McpServerTool(Name = "get_ide_status",
        Title = "Get detailed status of connected IDEs")]
    public async Task<IdeStatusResult> GetIdeStatus(CancellationToken ct = default)
    {
        try
        {
            var ides = _ideRouter.GetConnectedIdes();
            var statuses = new System.Collections.Generic.List<IdeDetailedStatus>();

            foreach (var ide in ides)
            {
                try
                {
                    // This would require adapter instance to get real-time status
                    statuses.Add(new IdeDetailedStatus
                    {
                        InstanceId = ide.InstanceId,
                        Name = ide.Name,
                        Version = ide.Version,
                        IsConnected = ide.IsConnected,
                        Capabilities = ide.Capabilities,
                        HasSolution = false, // Would need actual check
                        HasActiveDocument = false // Would need actual check
                    });
                }
                catch { }
            }

            return new IdeStatusResult
            {
                Count = statuses.Count,
                Ides = statuses.ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get IDE status");
            return new IdeStatusResult
            {
                Count = 0,
                Ides = Array.Empty<IdeDetailedStatus>()
            };
        }
    }

    /// <summary>
    /// Verify IDE connection
    /// </summary>
    [McpServerTool(Name = "verify_ide_connection",
        Title = "Verify connection to specific IDE")]
    public async Task<VerificationResult> VerifyIdeConnection(string? instanceId = null, CancellationToken ct = default)
    {
        try
        {
            var ides = _ideRouter.GetConnectedIdes();
            
            if (!string.IsNullOrEmpty(instanceId))
            {
                var ide = ides.FirstOrDefault(i => i.InstanceId == instanceId);
                if (ide == null)
                {
                    return new VerificationResult
                    {
                        Success = false,
                        Message = $"IDE with instance ID {instanceId} not found"
                    };
                }

                return new VerificationResult
                {
                    Success = ide.IsConnected,
                    Message = ide.IsConnected ? "IDE is connected and responding" : "IDE is not responding"
                };
            }
            else
            {
                // Check all connected IDEs
                var connectedCount = ides.Count(i => i.IsConnected);
                return new VerificationResult
                {
                    Success = connectedCount > 0,
                    Message = $"{connectedCount} IDE(s) connected out of {ides.Count} registered"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification failed");
            return new VerificationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get MCP tool list
    /// </summary>
    [McpServerTool(Name = "get_tools_list",
        Title = "Get list of available MCP tools")]
    public ToolsListResult GetToolsList()
    {
        return new ToolsListResult
        {
            Tools = new[]
            {
                new ToolInfo { Name = "get_solution_projects", Category = "Solution", Description = "Get all projects in solution" },
                new ToolInfo { Name = "get_solution_path", Category = "Solution", Description = "Get solution path" },
                new ToolInfo { Name = "get_solution_name", Category = "Solution", Description = "Get solution name" },
                new ToolInfo { Name = "get_solution_info", Category = "Solution", Description = "Get detailed solution info" },
                new ToolInfo { Name = "open_solution", Category = "Solution", Description = "Open a solution" },
                new ToolInfo { Name = "close_solution", Category = "Solution", Description = "Close current solution" },
                new ToolInfo { Name = "create_solution", Category = "Solution", Description = "Create new solution" },
                new ToolInfo { Name = "get_project_files", Category = "Project", Description = "Get files in project" },
                new ToolInfo { Name = "get_startup_projects", Category = "Project", Description = "Get startup project" },
                new ToolInfo { Name = "set_startup_project", Category = "Project", Description = "Set startup project" },
                new ToolInfo { Name = "open_file", Category = "File", Description = "Open file in IDE" },
                new ToolInfo { Name = "read_file", Category = "File", Description = "Read file content" },
                new ToolInfo { Name = "write_file", Category = "File", Description = "Write file content" },
                new ToolInfo { Name = "replace_in_file", Category = "File", Description = "Replace text in file" },
                new ToolInfo { Name = "get_file_info", Category = "File", Description = "Get file information" },
                new ToolInfo { Name = "find_in_files", Category = "File", Description = "Find text in files" },
                new ToolInfo { Name = "build_solution", Category = "Build", Description = "Build solution" },
                new ToolInfo { Name = "rebuild_solution", Category = "Build", Description = "Rebuild solution" },
                new ToolInfo { Name = "clean_solution", Category = "Build", Description = "Clean solution" },
                new ToolInfo { Name = "build_project", Category = "Build", Description = "Build specific project" },
                new ToolInfo { Name = "get_build_configurations", Category = "Build", Description = "Get build configurations" },
                new ToolInfo { Name = "set_build_configuration", Category = "Build", Description = "Set build configuration" },
                new ToolInfo { Name = "start_debugging", Category = "Debug", Description = "Start debugging" },
                new ToolInfo { Name = "stop_debugging", Category = "Debug", Description = "Stop debugging" },
                new ToolInfo { Name = "set_breakpoint", Category = "Debug", Description = "Set breakpoint" },
                new ToolInfo { Name = "remove_breakpoint", Category = "Debug", Description = "Remove breakpoint" },
                new ToolInfo { Name = "remove_all_breakpoints", Category = "Debug", Description = "Remove all breakpoints" },
                new ToolInfo { Name = "get_breakpoints", Category = "Debug", Description = "Get all breakpoints" },
                new ToolInfo { Name = "step_over", Category = "Debug", Description = "Step over" },
                new ToolInfo { Name = "step_into", Category = "Debug", Description = "Step into" },
                new ToolInfo { Name = "continue_execution", Category = "Debug", Description = "Continue execution" },
                new ToolInfo { Name = "get_debug_location", Category = "Debug", Description = "Get debug location" },
                new ToolInfo { Name = "health_check", Category = "Diagnostic", Description = "Check system health" },
                new ToolInfo { Name = "get_server_info", Category = "Diagnostic", Description = "Get server info" },
                new ToolInfo { Name = "get_ide_status", Category = "Diagnostic", Description = "Get IDE status" },
                new ToolInfo { Name = "get_tools_list", Category = "Diagnostic", Description = "Get tools list" }
            }
        };
    }
}

// Result types
public class HealthCheckResult
{
    public string ServerStatus { get; set; } = "";
    public int ConnectedIdeCount { get; set; }
    public IdeHealthStatus[] IdeStatuses { get; set; } = Array.Empty<IdeHealthStatus>();
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}

public class IdeHealthStatus
{
    public string InstanceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsConnected { get; set; }
    public bool IsHealthy { get; set; }
}

public class ServerInfoResult
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public ConnectedIdeInfo[] ConnectedIdes { get; set; } = Array.Empty<ConnectedIdeInfo>();
    public string[] SupportedOperations { get; set; } = Array.Empty<string>();
}

public class ConnectedIdeInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public IdeCapabilities Capabilities { get; set; } = new();
}

public class IdeStatusResult
{
    public int Count { get; set; }
    public IdeDetailedStatus[] Ides { get; set; } = Array.Empty<IdeDetailedStatus>();
}

public class IdeDetailedStatus
{
    public string InstanceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsConnected { get; set; }
    public IdeCapabilities Capabilities { get; set; } = new();
    public bool HasSolution { get; set; }
    public bool HasActiveDocument { get; set; }
}

public class VerificationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class ToolsListResult
{
    public ToolInfo[] Tools { get; set; } = Array.Empty<ToolInfo>();
}

public class ToolInfo
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
}
