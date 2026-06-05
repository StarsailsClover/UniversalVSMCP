using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UniversalVSMCP.IdeAbstraction;

namespace UniversalVSMCP;

/// <summary>
/// Tools for Visual Studio Debug operations
/// Enables AI Agents to control debugging sessions
/// </summary>
[McpServerToolType]
public class DebugTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<DebugTools> _logger;

    public DebugTools(IVsConnectionManager vsManager, ILogger<DebugTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Start debugging (F5)
    /// </summary>
    [McpServerTool(Name = "start_debugging", Title = "Start debugging the startup project (equivalent to pressing F5).")]
    public async Task<OperationResult> StartDebugging(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.Go();
            _logger.LogInformation("Started debugging");
            return OperationResult.Success("Debugging started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debugging");
            return OperationResult.Failure($"Failed to start debugging: {ex.Message}");
        }
    }

    /// <summary>
    /// Start debugging without attaching (Ctrl+F5)
    /// </summary>
    [McpServerTool(Name = "start_without_debugging", Title = "Start the startup project without debugging (equivalent to Ctrl+F5).")]
    public async Task<OperationResult> StartWithoutDebugging(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.Go(false); // false = don't break on exceptions
            _logger.LogInformation("Started without debugging");
            return OperationResult.Success("Execution started without debugging");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start without debugging");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop debugging (Shift+F5)
    /// </summary>
    [McpServerTool(Name = "stop_debugging", Title = "Stop the current debugging session (equivalent to Shift+F5).")]
    public async Task<OperationResult> StopDebugging(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.Stop(false);
            _logger.LogInformation("Stopped debugging");
            return OperationResult.Success("Debugging stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debugging");
            return OperationResult.Failure($"Failed to stop debugging: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle breakpoint at a specific line
    /// </summary>
    [McpServerTool(Name = "toggle_breakpoint", Title = "Navigate to a file and line. User can press F9 to toggle breakpoint manually.")]
    public async Task<OperationResult> ToggleBreakpoint(string filePath, int lineNumber, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            // Open the file first
            var window = dte.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindPrimary);
            
            // Navigate to the line
            var selection = window?.Selection as TextSelection;
            if (selection != null)
            {
                selection.GotoLine(lineNumber, false);
                _logger.LogInformation("Navigated to {File}:{Line} - User can press F9 to toggle breakpoint", filePath, lineNumber);
                return OperationResult.Success($"Navigated to line {lineNumber}. Press F9 to toggle breakpoint.");
            }
            
            return OperationResult.Failure("Could not access text selection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to breakpoint location");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Continue execution (F5)
    /// </summary>
    [McpServerTool(Name = "continue_execution", Title = "Continue execution from a breakpoint (equivalent to F5).")]
    public async Task<OperationResult> ContinueExecution(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.Go();
            _logger.LogInformation("Continued execution");
            return OperationResult.Success("Execution continued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue execution");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Step over (F10)
    /// </summary>
    [McpServerTool(Name = "step_over", Title = "Step over the current line (equivalent to F10).")]
    public async Task<OperationResult> StepOver(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.StepOver();
            _logger.LogInformation("Stepped over");
            return OperationResult.Success("Stepped over");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step over");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Step into (F11)
    /// </summary>
    [McpServerTool(Name = "step_into", Title = "Step into the current function call (equivalent to F11).")]
    public async Task<OperationResult> StepInto(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.StepInto();
            _logger.LogInformation("Stepped into");
            return OperationResult.Success("Stepped into");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step into");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Step out (Shift+F11)
    /// </summary>
    [McpServerTool(Name = "step_out", Title = "Step out of the current function (equivalent to Shift+F11).")]
    public async Task<OperationResult> StepOut(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        try
        {
            dte.Debugger.StepOut();
            _logger.LogInformation("Stepped out");
            return OperationResult.Success("Stepped out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step out");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current debug state
    /// </summary>
    [McpServerTool(Name = "get_debug_state", Title = "Get the current debugging state (running, paused, stopped) and current process/thread info.")]
    public async Task<DebugStateResult?> GetDebugState(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Debugger == null)
        {
            return null;
        }

        try
        {
            return new DebugStateResult
            {
                CurrentMode = dte.Debugger.CurrentMode.ToString(),
                CurrentProcessName = dte.Debugger.CurrentProcess?.Name ?? "None",
                CurrentThreadId = dte.Debugger.CurrentThread?.ID ?? 0,
                CurrentStackFrame = dte.Debugger.CurrentStackFrame?.FunctionName ?? "None"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get debug state");
            return null;
        }
    }
}

#region Data Models

public class DebugStateResult
{
    public string CurrentMode { get; set; } = string.Empty;
    public string CurrentProcessName { get; set; } = string.Empty;
    public int CurrentThreadId { get; set; }
    public string CurrentStackFrame { get; set; } = string.Empty;
}

#endregion



