using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalVSMCP.IdeAbstraction;

/// <summary>
/// Unified IDE Adapter Interface - abstracts Visual Studio, VS Code, and future IDEs
/// </summary>
public interface IIdeAdapter
{
    #region IDE Information
    
    /// <summary>
    /// Unique identifier for this IDE instance
    /// </summary>
    string InstanceId { get; }
    
    /// <summary>
    /// IDE name (e.g., "Visual Studio", "VS Code")
    /// </summary>
    string IdeName { get; }
    
    /// <summary>
    /// IDE version (e.g., "17.14", "1.90")
    /// </summary>
    string IdeVersion { get; }
    
    /// <summary>
    /// Adapter capabilities
    /// </summary>
    IdeCapabilities Capabilities { get; }
    
    /// <summary>
    /// Connection status
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// IDE process information
    /// </summary>
    IdeProcessInfo ProcessInfo { get; }
    
    #endregion

    #region Connection Management
    
    /// <summary>
    /// Connect to the IDE
    /// </summary>
    Task<bool> ConnectAsync(ConnectionOptions options);
    
    /// <summary>
    /// Disconnect from the IDE
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Check if IDE is still responsive
    /// </summary>
    Task<bool> HealthCheckAsync();
    
    #endregion

    #region Solution/Project Management
    
    /// <summary>
    /// Get current solution information
    /// </summary>
    Task<SolutionInfo?> GetSolutionAsync();
    
    /// <summary>
    /// Open a solution
    /// </summary>
    Task<bool> OpenSolutionAsync(string solutionPath);
    
    /// <summary>
    /// Close current solution
    /// </summary>
    Task<bool> CloseSolutionAsync();
    
    /// <summary>
    /// Create a new solution
    /// </summary>
    Task<SolutionInfo?> CreateSolutionAsync(string solutionPath, string template);
    
    /// <summary>
    /// Get all projects in current solution
    /// </summary>
    Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync();
    
    /// <summary>
    /// Get startup project
    /// </summary>
    Task<ProjectInfo?> GetStartupProjectAsync();
    
    /// <summary>
    /// Set startup project
    /// </summary>
    Task<bool> SetStartupProjectAsync(string projectName);
    
    /// <summary>
    /// Add file to project
    /// </summary>
    Task<bool> AddFileToProjectAsync(string projectName, string filePath);
    
    /// <summary>
    /// Get project files
    /// </summary>
    Task<IReadOnlyList<FileInfo>> GetProjectFilesAsync(string projectName);
    
    #endregion

    #region File Operations
    
    /// <summary>
    /// Open file in IDE
    /// </summary>
    Task<bool> OpenFileAsync(string filePath, int? line = null, int? column = null);
    
    /// <summary>
    /// Read file content
    /// </summary>
    Task<string?> ReadFileAsync(string filePath);
    
    /// <summary>
    /// Write file content
    /// </summary>
    Task<bool> WriteFileAsync(string filePath, string content);
    
    /// <summary>
    /// Replace text in file
    /// </summary>
    Task<bool> ReplaceInFileAsync(string filePath, string searchText, string replacement);
    
    /// <summary>
    /// Get file information
    /// </summary>
    Task<FileInfo?> GetFileInfoAsync(string filePath);
    
    /// <summary>
    /// Search files in solution
    /// </summary>
    Task<IReadOnlyList<SearchResult>> FindInFilesAsync(string searchText, string? filePattern = null);
    
    #endregion

    #region Build Operations
    
    /// <summary>
    /// Build solution
    /// </summary>
    Task<BuildResult> BuildSolutionAsync(BuildConfiguration? config = null);
    
    /// <summary>
    /// Rebuild solution
    /// </summary>
    Task<BuildResult> RebuildSolutionAsync(BuildConfiguration? config = null);
    
    /// <summary>
    /// Clean solution
    /// </summary>
    Task<bool> CleanSolutionAsync();
    
    /// <summary>
    /// Build specific project
    /// </summary>
    Task<BuildResult> BuildProjectAsync(string projectName, BuildConfiguration? config = null);
    
    /// <summary>
    /// Get available build configurations
    /// </summary>
    Task<IReadOnlyList<string>> GetBuildConfigurationsAsync();
    
    /// <summary>
    /// Set active build configuration
    /// </summary>
    Task<bool> SetBuildConfigurationAsync(string configuration);
    
    /// <summary>
    /// Get build output
    /// </summary>
    Task<string> GetBuildOutputAsync();
    
    #endregion

    #region Debug Operations
    
    /// <summary>
    /// Start debugging
    /// </summary>
    Task<bool> StartDebuggingAsync(DebugTarget? target = null);
    
    /// <summary>
    /// Stop debugging
    /// </summary>
    Task<bool> StopDebuggingAsync();
    
    /// <summary>
    /// Set breakpoint
    /// </summary>
    Task<bool> SetBreakpointAsync(string filePath, int line);
    
    /// <summary>
    /// Remove breakpoint
    /// </summary>
    Task<bool> RemoveBreakpointAsync(string filePath, int line);
    
    /// <summary>
    /// Remove all breakpoints
    /// </summary>
    Task<bool> RemoveAllBreakpointsAsync();
    
    /// <summary>
    /// Get all breakpoints
    /// </summary>
    Task<IReadOnlyList<BreakpointInfo>> GetBreakpointsAsync();
    
    /// <summary>
    /// Step over
    /// </summary>
    Task<bool> StepOverAsync();
    
    /// <summary>
    /// Step into
    /// </summary>
    Task<bool> StepIntoAsync();
    
    /// <summary>
    /// Step out
    /// </summary>
    Task<bool> StepOutAsync();
    
    /// <summary>
    /// Continue execution
    /// </summary>
    Task<bool> ContinueAsync();
    
    /// <summary>
    /// Get current debug location
    /// </summary>
    Task<DebugLocation?> GetDebugLocationAsync();
    
    #endregion

    #region IDE Commands
    
    /// <summary>
    /// Execute IDE command
    /// </summary>
    Task<bool> ExecuteCommandAsync(string commandName, object? args = null);
    
    /// <summary>
    /// Get available commands
    /// </summary>
    Task<IReadOnlyList<CommandInfo>> GetCommandsAsync();
    
    /// <summary>
    /// Get IDE status
    /// </summary>
    Task<IdeStatus> GetStatusAsync();
    
    #endregion

    #region Events
    
    /// <summary>
    /// Raised when solution is opened
    /// </summary>
    event EventHandler<SolutionEventArgs>? SolutionOpened;
    
    /// <summary>
    /// Raised when solution is closed
    /// </summary>
    event EventHandler<SolutionEventArgs>? SolutionClosed;
    
    /// <summary>
    /// Raised when build completes
    /// </summary>
    event EventHandler<BuildEventArgs>? BuildCompleted;
    
    /// <summary>
    /// Raised when debugging state changes
    /// </summary>
    event EventHandler<DebugEventArgs>? DebugStateChanged;
    
    /// <summary>
    /// Raised when IDE disconnects
    /// </summary>
    event EventHandler? Disconnected;
    
    #endregion
}

/// <summary>
/// IDE capabilities
/// </summary>
public class IdeCapabilities
{
    public bool SupportsSolutionManagement { get; set; } = true;
    public bool SupportsProjectManagement { get; set; } = true;
    public bool SupportsFileOperations { get; set; } = true;
    public bool SupportsBuild { get; set; } = true;
    public bool SupportsDebug { get; set; } = true;
    public bool SupportsRefactoring { get; set; } = false;
    public bool SupportsIntelliSense { get; set; } = false;
    public bool SupportsSourceControl { get; set; } = false;
    public bool SupportsExtensions { get; set; } = false;
}

/// <summary>
/// Connection options
/// </summary>
public class ConnectionOptions
{
    public string? SolutionPath { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AutoDiscover { get; set; } = true;
    public int? ProcessId { get; set; }
}

/// <summary>
/// IDE process information
/// </summary>
public class IdeProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public long WorkingSet { get; set; }
}

/// <summary>
/// Solution information
/// </summary>
public class SolutionInfo
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsOpen { get; set; }
    public int ProjectCount { get; set; }
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Project information
/// </summary>
public class ProjectInfo
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Type { get; set; } = "";
    public string Language { get; set; } = "";
    public bool IsStartupProject { get; set; }
    public IReadOnlyList<string> Files { get; set; } = Array.Empty<string>();
}

/// <summary>
/// File information
/// </summary>
public class FileInfo
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Extension { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsOpen { get; set; }
}

/// <summary>
/// Build configuration
/// </summary>
public class BuildConfiguration
{
    public string Configuration { get; set; } = "Debug";
    public string Platform { get; set; } = "Any CPU";
}

/// <summary>
/// Build result
/// </summary>
public class BuildResult
{
    public bool Success { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public string Output { get; set; } = "";
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Debug target
/// </summary>
public class DebugTarget
{
    public string? ProjectName { get; set; }
    public string? ExecutablePath { get; set; }
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// Breakpoint information
/// </summary>
public class BreakpointInfo
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public bool Enabled { get; set; }
    public string? Condition { get; set; }
}

/// <summary>
/// Debug location
/// </summary>
public class DebugLocation
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Function { get; set; } = "";
}

/// <summary>
/// Search result
/// </summary>
public class SearchResult
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Text { get; set; } = "";
    public string LineText { get; set; } = "";
}

/// <summary>
/// Command information
/// </summary>
public class CommandInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Shortcut { get; set; }
}

/// <summary>
/// IDE status
/// </summary>
public class IdeStatus
{
    public bool IsBusy { get; set; }
    public string? CurrentOperation { get; set; }
    public int OpenDocumentCount { get; set; }
    public string? ActiveDocument { get; set; }
}

/// <summary>
/// Solution event arguments
/// </summary>
public class SolutionEventArgs : EventArgs
{
    public SolutionInfo Solution { get; set; } = new();
}

/// <summary>
/// Build event arguments
/// </summary>
public class BuildEventArgs : EventArgs
{
    public BuildResult Result { get; set; } = new();
}

/// <summary>
/// Debug event arguments
/// </summary>
public class DebugEventArgs : EventArgs
{
    public string State { get; set; } = "";
    public DebugLocation? Location { get; set; }
}
