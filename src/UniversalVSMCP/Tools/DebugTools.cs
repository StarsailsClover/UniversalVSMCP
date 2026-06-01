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
/// Debug Tools - Unified debugging operations using IIdeAdapter
/// </summary>
[McpServerToolType]
public class DebugTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<DebugTools> _logger;

    public DebugTools(IdeRouter ideRouter, ILogger<DebugTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Start debugging
    /// </summary>
    [McpServerTool(Name = "start_debugging",
        Title = "Start debugging session")]
    public async Task<DebugOperationResult> StartDebugging(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting debugging");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.StartDebuggingAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "Debugging started" : "Failed to start debugging"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debugging");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Stop debugging
    /// </summary>
    [McpServerTool(Name = "stop_debugging",
        Title = "Stop debugging session")]
    public async Task<DebugOperationResult> StopDebugging(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Stopping debugging");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.StopDebuggingAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "Debugging stopped" : "Failed to stop debugging"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debugging");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Set breakpoint at file and line
    /// </summary>
    [McpServerTool(Name = "set_breakpoint",
        Title = "Set a breakpoint at a specific file and line")]
    public async Task<DebugOperationResult> SetBreakpoint(string filePath, int line, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Setting breakpoint at {File}:{Line}", filePath, line);

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.SetBreakpointAsync(filePath, line);

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? $"Breakpoint set at {filePath}:{line}" : "Failed to set breakpoint"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set breakpoint");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Remove breakpoint
    /// </summary>
    [McpServerTool(Name = "remove_breakpoint",
        Title = "Remove a breakpoint at a specific file and line")]
    public async Task<DebugOperationResult> RemoveBreakpoint(string filePath, int line, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Removing breakpoint at {File}:{Line}", filePath, line);

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.RemoveBreakpointAsync(filePath, line);

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? $"Breakpoint removed at {filePath}:{line}" : "Failed to remove breakpoint"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove breakpoint");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Remove all breakpoints
    /// </summary>
    [McpServerTool(Name = "remove_all_breakpoints",
        Title = "Remove all breakpoints")]
    public async Task<DebugOperationResult> RemoveAllBreakpoints(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Removing all breakpoints");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.RemoveAllBreakpointsAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "All breakpoints removed" : "Failed to remove breakpoints"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all breakpoints");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get all breakpoints
    /// </summary>
    [McpServerTool(Name = "get_breakpoints",
        Title = "Get all breakpoints")]
    public async Task<BreakpointListResult> GetBreakpoints(CancellationToken ct = default)
    {
        try
        {
            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new BreakpointListResult
                {
                    Breakpoints = Array.Empty<BreakpointInfo>()
                };
            }

            var breakpoints = await adapter.GetBreakpointsAsync();

            return new BreakpointListResult
            {
                Breakpoints = breakpoints.Select(b => new BreakpointInfo
                {
                    FilePath = b.FilePath,
                    Line = b.Line,
                    Enabled = b.Enabled,
                    Condition = b.Condition
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get breakpoints");
            return new BreakpointListResult
            {
                Breakpoints = Array.Empty<BreakpointInfo>()
            };
        }
    }

    /// <summary>
    /// Step over
    /// </summary>
    [McpServerTool(Name = "step_over",
        Title = "Step over current line")]
    public async Task<DebugOperationResult> StepOver(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Stepping over");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.StepOverAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "Stepped over" : "Failed to step over"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step over");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Step into
    /// </summary>
    [McpServerTool(Name = "step_into",
        Title = "Step into method")]
    public async Task<DebugOperationResult> StepInto(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Stepping into");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.StepIntoAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "Stepped into" : "Failed to step into"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step into");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Continue execution
    /// </summary>
    [McpServerTool(Name = "continue_execution",
        Title = "Continue execution")]
    public async Task<DebugOperationResult> Continue(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Continuing execution");

            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugOperationResult
                {
                    Success = false,
                    Message = "No IDE with debug support available"
                };
            }

            var success = await adapter.ContinueAsync();

            return new DebugOperationResult
            {
                Success = success,
                Message = success ? "Continuing execution" : "Failed to continue"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue");
            return new DebugOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get current debug location
    /// </summary>
    [McpServerTool(Name = "get_debug_location",
        Title = "Get current debug location")]
    public async Task<DebugLocationResult> GetDebugLocation(CancellationToken ct = default)
    {
        try
        {
            var criteria = new RoutingCriteria { PreferDebugSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new DebugLocationResult
                {
                    HasLocation = false
                };
            }

            var location = await adapter.GetDebugLocationAsync();

            if (location == null)
            {
                return new DebugLocationResult
                {
                    HasLocation = false
                };
            }

            return new DebugLocationResult
            {
                HasLocation = true,
                FilePath = location.FilePath,
                Line = location.Line,
                Column = location.Column,
                Function = location.Function
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get debug location");
            return new DebugLocationResult
            {
                HasLocation = false
            };
        }
    }
}

// Result types
public class DebugOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class BreakpointListResult
{
    public BreakpointInfo[] Breakpoints { get; set; } = Array.Empty<BreakpointInfo>();
}

public class BreakpointInfo
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public bool Enabled { get; set; }
    public string? Condition { get; set; }
}

public class DebugLocationResult
{
    public bool HasLocation { get; set; }
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Function { get; set; } = "";
}
