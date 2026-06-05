using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using UniversalVSMCP.IdeAbstraction;

namespace UniversalVSMCP.IdeAdapters;

/// <summary>
/// Visual Studio COM/DTE Adapter - implements IIdeAdapter for Visual Studio
/// </summary>
public class VsDteAdapter : IIdeAdapter
{
    private readonly ILogger<VsDteAdapter> _logger;
    private DTE2? _dte;
    private bool _isConnected;
    private string _instanceId = Guid.NewGuid().ToString("N")[..8];

    public string InstanceId => _instanceId;
    public string IdeName => "Visual Studio";
    public string IdeVersion => _dte?.Version ?? "Unknown";
    public IdeCapabilities Capabilities { get; } = new()
    {
        SupportsSolutionManagement = true,
        SupportsProjectManagement = true,
        SupportsFileOperations = true,
        SupportsBuild = true,
        SupportsDebug = true,
        SupportsRefactoring = true,
        SupportsIntelliSense = true,
        SupportsSourceControl = true,
        SupportsExtensions = true
    };

    public bool IsConnected => _isConnected && _dte != null;

    public IdeProcessInfo ProcessInfo => new()
    {
        ProcessId = System.Diagnostics.Process.GetProcessesByName("devenv").FirstOrDefault()?.Id ?? 0 ?? 0,
        ProcessName = "devenv",
        StartTime = DateTime.Now // Approximate
    };

    public event EventHandler<SolutionEventArgs>? SolutionOpened;
    public event EventHandler<SolutionEventArgs>? SolutionClosed;
    public event EventHandler<BuildEventArgs>? BuildCompleted;
    public event EventHandler<DebugEventArgs>? DebugStateChanged;
    public event EventHandler? Disconnected;

    public VsDteAdapter(ILogger<VsDteAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(ConnectionOptions options)
    {
        try
        {
            _logger.LogInformation("Connecting to Visual Studio...");

            // Try to get running VS instance
            if (options.ProcessId.HasValue)
            {
                _dte = await GetVsInstanceByProcessId(options.ProcessId.Value);
            }
            else if (!string.IsNullOrEmpty(options.SolutionPath))
            {
                _dte = await GetVsInstanceBySolution(options.SolutionPath);
            }
            else if (options.AutoDiscover)
            {
                _dte = await DiscoverVsInstance();
            }

            if (_dte == null)
            {
                _logger.LogError("Could not find Visual Studio instance");
                return false;
            }

            _isConnected = true;
            _logger.LogInformation("Connected to Visual Studio {Version}", _dte.Version);

            // Subscribe to events
            SubscribeToEvents();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Visual Studio");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_dte != null)
            {
                UnsubscribeFromEvents();
                Marshal.ReleaseComObject(_dte);
                _dte = null;
            }
            _isConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Disconnected from Visual Studio");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from Visual Studio");
        }

        await Task.CompletedTask;
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            if (_dte == null) return false;
            // Try to access a property to verify connection
            _ = _dte.Version;
            return true;
        }
        catch
        {
            _isConnected = false;
            return false;
        }
    }

    #region Solution/Project Management

    public async Task<SolutionInfo?> GetSolutionAsync()
    {
        await Task.CompletedTask;
        if (_dte?.Solution == null)
        {
            return null;
        }

        var solution = _dte.Solution;
        return new SolutionInfo
        {
            Name = Path.GetFileName(solution.FullName),
            FullPath = solution.FullName,
            IsOpen = solution.IsOpen,
            ProjectCount = solution.Projects.Count,
            LastModified = File.Exists(solution.FullName) 
                ? File.GetLastWriteTime(solution.FullName) 
                : null
        };
    }

    public async Task<bool> OpenSolutionAsync(string solutionPath)
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Solution.Open(solutionPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open solution {Path}", solutionPath);
            return false;
        }
    }

    public async Task<bool> CloseSolutionAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Solution.Close();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close solution");
            return false;
        }
    }

    public async Task<SolutionInfo?> CreateSolutionAsync(string solutionPath, string template)
    {
        // VS doesn't have direct solution creation via DTE
        // This would require creating from template
        _logger.LogWarning("CreateSolution not implemented for VS DTE adapter");
        return await GetSolutionAsync();
    }

    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync()
    {
        await Task.CompletedTask;
        var projects = new List<ProjectInfo>();

        if (_dte?.Solution?.Projects == null)
        {
            return projects;
        }

        foreach (Project project in _dte.Solution.Projects)
        {
            try
            {
                projects.Add(new ProjectInfo
                {
                    Name = project.Name,
                    FullPath = project.FullName,
                    Type = project.Kind,
                    Language = GetProjectLanguage(project),
                    IsStartupProject = IsStartupProject(project),
                    Files = GetProjectFiles(project).Select(f => f.FullPath).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get info for project {Name}", project.Name);
            }
        }

        return projects;
    }

    public async Task<ProjectInfo?> GetStartupProjectAsync()
    {
        await Task.CompletedTask;
        if (_dte?.Solution?.SolutionBuild?.StartupProjects == null)
        {
            return null;
        }

        var startupProjects = _dte.Solution.SolutionBuild.StartupProjects as Array;
        if (startupProjects?.Length > 0)
        {
            var startupPath = startupProjects.GetValue(0)?.ToString();
            if (!string.IsNullOrEmpty(startupPath))
            {
                var project = FindProjectByPath(startupPath);
                if (project != null)
                {
                    return new ProjectInfo
                    {
                        Name = project.Name,
                        FullPath = project.FullName,
                        Type = project.Kind,
                        IsStartupProject = true
                    };
                }
            }
        }
        return null;
    }

    public async Task<bool> SetStartupProjectAsync(string projectName)
    {
        try
        {
            await Task.CompletedTask;
            var project = FindProjectByName(projectName);
            if (project != null)
            {
                _dte.Solution.SolutionBuild.StartupProjects = project.FullName;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set startup project {Name}", projectName);
            return false;
        }
    }

    public async Task<bool> AddFileToProjectAsync(string projectName, string filePath)
    {
        try
        {
            await Task.CompletedTask;
            var project = FindProjectByName(projectName);
            if (project != null)
            {
                project.ProjectItems.AddFromFile(filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add file {File} to project {Project}", filePath, projectName);
            return false;
        }
    }

    public async Task<IReadOnlyList<UVMFileInfo>> GetProjectFilesAsync(string projectName)
    {
        await Task.CompletedTask;
        var project = FindProjectByName(projectName);
        if (project == null)
        {
            return Array.Empty<UVMFileInfo>();
        }
        return GetProjectFiles(project).Select(f => f.FullPath).ToList();
    }

    #endregion

    #region File Operations

    public async Task<bool> OpenFileAsync(string filePath, int? line = null, int? column = null)
    {
        try
        {
            await Task.CompletedTask;
            var window = _dte?.ItemOperations.OpenFile(filePath);
            if (line.HasValue && window != null)
            {
                // Navigate to line
                var selection = (TextSelection?)_dte?.ActiveDocument?.Selection;
                selection?.GotoLine(line.Value, false);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file {Path}", filePath);
            return false;
        }
    }

    public async Task<string?> ReadFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file {Path}", filePath);
            return null;
        }
    }

    public async Task<bool> WriteFileAsync(string filePath, string content)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file {Path}", filePath);
            return false;
        }
    }

    public async Task<bool> ReplaceInFileAsync(string filePath, string searchText, string replacement)
    {
        try
        {
            var content = await ReadFileAsync(filePath);
            if (content == null) return false;
            
            var newContent = content.Replace(searchText, replacement);
            return await WriteFileAsync(filePath, newContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace in file {Path}", filePath);
            return false;
        }
    }

    public async Task<UVMFileInfo?> GetUVMFileInfoAsync(string filePath)
    {
        await Task.CompletedTask;
        if (!File.Exists(filePath))
        {
            return null;
        }

        var UVMFileInfo = new System.IO.UVMFileInfo(filePath);
        return new UVMFileInfo
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            Extension = fileInfo.Extension,
            Size = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime,
            IsOpen = _dte?.Documents?.Cast<Document>().Any(d => d.FullName == filePath) ?? false
        };
    }

    public async Task<IReadOnlyList<SearchResult>> FindInFilesAsync(string searchText, string? filePattern = null)
    {
        var results = new List<SearchResult>();
        
        try
        {
            // Use VS Find in Files
            if (_dte?.Find != null)
            {
                _dte.Find.Action = vsFindAction.vsFindActionFindAll;
                _dte.Find.FindWhat = searchText;
                _dte.Find.FilesOfType = filePattern ?? "*.*";
                _dte.Find.ResultsLocation = vsFindResultsLocation.vsFindResults1;
                _dte.Find.Execute();
                
                // Note: Getting actual results from Find window is complex
                // This is a simplified implementation
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find in files");
        }

        return results;
    }

    #endregion

    #region Build Operations

    public async Task<BuildResult> BuildSolutionAsync(BuildConfiguration? config = null)
    {
        return await ExecuteBuildAsync(vsBuildAction.vsBuildActionBuild, config);
    }

    public async Task<BuildResult> RebuildSolutionAsync(BuildConfiguration? config = null)
    {
        return await ExecuteBuildAsync(vsBuildAction.vsBuildActionRebuildAll, config);
    }

    public async Task<bool> CleanSolutionAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Solution.SolutionBuild.Cleanup();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean solution");
            return false;
        }
    }

    public async Task<BuildResult> BuildProjectAsync(string projectName, BuildConfiguration? config = null)
    {
        var project = FindProjectByName(projectName);
        if (project == null)
        {
            return new BuildResult { Success = false, Output = $"Project {projectName} not found" };
        }

        try
        {
            await Task.CompletedTask;
            // VS DTE doesn't directly support single project build
            // This would require more complex implementation
            return new BuildResult { Success = true, Output = "Project build not fully implemented" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build project {Name}", projectName);
            return new BuildResult { Success = false, Output = ex.Message };
        }
    }

    public async Task<IReadOnlyList<string>> GetBuildConfigurationsAsync()
    {
        await Task.CompletedTask;
        var configs = new List<string>();
        
        try
        {
            var solutionConfigs = _dte?.Solution.SolutionBuild.SolutionConfigurations;
            if (solutionConfigs != null)
            {
                foreach (SolutionConfiguration config in solutionConfigs)
                {
                    configs.Add(config.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get build configurations");
        }
        
        return configs.Count > 0 ? configs : new[] { "Debug", "Release" };
    }

    public async Task<bool> SetBuildConfigurationAsync(string configuration)
    {
        try
        {
            await Task.CompletedTask;
            var solutionConfigs = _dte?.Solution.SolutionBuild.SolutionConfigurations;
            if (solutionConfigs != null)
            {
                foreach (SolutionConfiguration config in solutionConfigs)
                {
                    if (config.Name == configuration)
                    {
                        config.Activate();
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set build configuration {Config}", configuration);
            return false;
        }
    }

    public async Task<string> GetBuildOutputAsync()
    {
        await Task.CompletedTask;
        // Getting actual build output requires accessing output window
        return "Build output not fully implemented";
    }

    #endregion

    #region Debug Operations

    public async Task<bool> StartDebuggingAsync(DebugTarget? target = null)
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.Go();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debugging");
            return false;
        }
    }

    public async Task<bool> StopDebuggingAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.Stop();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debugging");
            return false;
        }
    }

    public async Task<bool> SetBreakpointAsync(string filePath, int line)
    {
        try
        {
            await Task.CompletedTask;
            // Open file first
            await OpenFileAsync(filePath);
            // Set breakpoint
            _dte?.Debugger.Breakpoints.Add(filePath, line);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set breakpoint at {File}:{Line}", filePath, line);
            return false;
        }
    }

    public async Task<bool> RemoveBreakpointAsync(string filePath, int line)
    {
        try
        {
            await Task.CompletedTask;
            var breakpoints = _dte?.Debugger.Breakpoints;
            if (breakpoints != null)
            {
                foreach (Breakpoint bp in breakpoints)
                {
                    if (bp.File == filePath && bp.FileLine == line)
                    {
                        bp.Delete();
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove breakpoint");
            return false;
        }
    }

    public async Task<bool> RemoveAllBreakpointsAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.Breakpoints.DeleteAll();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all breakpoints");
            return false;
        }
    }

    public async Task<IReadOnlyList<BreakpointInfo>> GetBreakpointsAsync()
    {
        await Task.CompletedTask;
        var breakpoints = new List<BreakpointInfo>();
        
        try
        {
            var dteBreakpoints = _dte?.Debugger.Breakpoints;
            if (dteBreakpoints != null)
            {
                foreach (Breakpoint bp in dteBreakpoints)
                {
                    breakpoints.Add(new BreakpointInfo
                    {
                        FilePath = bp.File,
                        Line = bp.FileLine,
                        Enabled = bp.Enabled,
                        Condition = bp.Condition
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get breakpoints");
        }
        
        return breakpoints;
    }

    public async Task<bool> StepOverAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.StepOver();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step over");
            return false;
        }
    }

    public async Task<bool> StepIntoAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.StepInto();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step into");
            return false;
        }
    }

    public async Task<bool> StepOutAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.StepOut();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step out");
            return false;
        }
    }

    public async Task<bool> ContinueAsync()
    {
        try
        {
            await Task.CompletedTask;
            _dte?.Debugger.Go();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue");
            return false;
        }
    }

    public async Task<DebugLocation?> GetDebugLocationAsync()
    {
        await Task.CompletedTask;
        try
        {
            var currentFrame = _dte?.Debugger.CurrentStackFrame;
            if (currentFrame != null)
            {
                return new DebugLocation
                {
                    FilePath = currentFrame.FileName,
                    Line = currentFrame.Line,
                    Column = currentFrame.Column,
                    Function = currentFrame.FunctionName
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get debug location");
            return null;
        }
    }

    #endregion

    #region IDE Commands

    public async Task<bool> ExecuteCommandAsync(string commandName, object? args = null)
    {
        try
        {
            await Task.CompletedTask;
            _dte?.ExecuteCommand(commandName, args?.ToString() ?? "");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command {Command}", commandName);
            return false;
        }
    }

    public async Task<IReadOnlyList<CommandInfo>> GetCommandsAsync()
    {
        await Task.CompletedTask;
        var commands = new List<CommandInfo>();
        
        try
        {
            var dteCommands = _dte?.Commands;
            if (dteCommands != null)
            {
                foreach (Command cmd in dteCommands)
                {
                    try
                    {
                        commands.Add(new CommandInfo
                        {
                            Name = cmd.Name,
                            DisplayName = cmd.LocalizedName,
                            Shortcut = cmd.Bindings?.Length > 0 ? cmd.Bindings[0].ToString() : null
                        });
                    }
                    catch { /* Skip commands that can't be accessed */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get commands");
        }
        
        return commands;
    }

    public async Task<IdeStatus> GetStatusAsync()
    {
        await Task.CompletedTask;
        return new IdeStatus
        {
            IsBusy = _dte?.Solution?.SolutionBuild?.BuildState == vsBuildState.vsBuildStateInProgress,
            CurrentOperation = _dte?.Solution?.SolutionBuild?.BuildState.ToString(),
            OpenDocumentCount = _dte?.Documents?.Count ?? 0,
            ActiveDocument = _dte?.ActiveDocument?.FullName
        };
    }

    #endregion

    #region Helper Methods

    private async Task<DTE2?> GetVsInstanceByProcessId(int processId)
    {
        // Implementation would use ROT (Running Object Table) enumeration
        _logger.LogWarning("GetVsInstanceByProcessId not fully implemented");
        return await DiscoverVsInstance();
    }

    private async Task<DTE2?> GetVsInstanceBySolution(string solutionPath)
    {
        // Implementation would enumerate VS instances and check solution
        _logger.LogWarning("GetVsInstanceBySolution not fully implemented");
        return await DiscoverVsInstance();
    }

    private async Task<DTE2?> DiscoverVsInstance()
    {
        await Task.CompletedTask;
        try
        {
            // Try to get latest VS instance via ROT
            var rotEntries = System.Runtime.InteropServices.ComTypes.IRunningObjectTable;
            
            // For now, use GetActiveObject
            var dte = (DTE?)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.18.0");
            if (dte == null)
            {
                dte = (DTE?)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.17.0");
            }
            return dte as DTE2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover VS instance");
            return null;
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to build events
        if (_dte?.Events?.BuildEvents != null)
        {
            _dte.Events.BuildEvents.OnBuildDone += OnBuildDone;
        }

        // Subscribe to debug events
        if (_dte?.Events?.DebuggerEvents != null)
        {
            _dte.Events.DebuggerEvents.OnEnterBreakMode += OnEnterBreakMode;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_dte?.Events?.BuildEvents != null)
        {
            _dte.Events.BuildEvents.OnBuildDone -= OnBuildDone;
        }
        if (_dte?.Events?.DebuggerEvents != null)
        {
            _dte.Events.DebuggerEvents.OnEnterBreakMode -= OnEnterBreakMode;
        }
    }

    private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
    {
        var result = new BuildResult
        {
            Success = _dte?.Solution?.SolutionBuild?.LastBuildInfo == 0,
            ErrorCount = _dte?.Solution?.SolutionBuild?.LastBuildInfo ?? 0
        };
        BuildCompleted?.Invoke(this, new BuildEventArgs { Result = result });
    }

    private void OnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction action)
    {
        DebugStateChanged?.Invoke(this, new DebugEventArgs { State = "Break" });
    }

    private async Task<BuildResult> ExecuteBuildAsync(vsBuildAction action, BuildConfiguration? config)
    {
        try
        {
            await Task.CompletedTask;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            if (config != null)
            {
                await SetBuildConfigurationAsync(config.Configuration);
            }
            
            _dte?.Solution.SolutionBuild.Build(true); // true = wait for completion
            sw.Stop();
            
            return new BuildResult
            {
                Success = _dte?.Solution?.SolutionBuild?.LastBuildInfo == 0,
                ErrorCount = _dte?.Solution?.SolutionBuild?.LastBuildInfo ?? 0,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed");
            return new BuildResult { Success = false, Output = ex.Message };
        }
    }

    private Project? FindProjectByName(string name)
    {
        if (_dte?.Solution?.Projects == null) return null;
        
        foreach (Project project in _dte.Solution.Projects)
        {
            if (project.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
        }
        return null;
    }

    private Project? FindProjectByPath(string path)
    {
        if (_dte?.Solution?.Projects == null) return null;
        
        foreach (Project project in _dte.Solution.Projects)
        {
            if (project.FullName.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
        }
        return null;
    }

    private bool IsStartupProject(Project project)
    {
        var startupProjects = _dte?.Solution?.SolutionBuild?.StartupProjects as Array;
        if (startupProjects == null) return false;
        
        foreach (var path in startupProjects)
        {
            if (path?.ToString()?.Equals(project.FullName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }
        return false;
    }

    private string GetProjectLanguage(Project project)
    {
        // Try to determine language from project type
        return project.Kind switch
        {
            "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" => "C#",
            "{F184B08B-C81C-45F6-A57F-5ABD9991F28F}" => "VB.NET",
            "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" => "C++",
            _ => "Unknown"
        };
    }

    private IEnumerable<UVMFileInfo> GetProjectFiles(Project project)
    {
        var files = new List<UVMFileInfo>();
        
        try
        {
            foreach (ProjectItem item in project.ProjectItems)
            {
                try
                {
                    if (item.FileNames[0] != null)
                    {
                        var path = item.FileNames[0];
                        if (File.Exists(path))
                        {
                            var info = new System.IO.UVMFileInfo(path);
                            files.Add(new UVMFileInfo
                            {
                                Name = info.Name,
                                FullPath = info.FullName,
                                Extension = info.Extension,
                                Size = info.Length,
                                LastModified = info.LastWriteTime
                            });
                        }
                    }
                }
                catch { /* Skip items that can't be accessed */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get project files for {Project}", project.Name);
        }
        
        return files;
    }

    #endregion
}


