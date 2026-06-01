using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UniversalVSMCP.IdeAbstraction;

namespace UniversalVSMCP.IdeAdapters;

/// <summary>
/// VS Code Adapter - implements IIdeAdapter for Visual Studio Code
/// Uses VS Code Extension API via HTTP/WebSocket
/// </summary>
public class VsCodeAdapter : IIdeAdapter
{
    private readonly ILogger<VsCodeAdapter> _logger;
    private readonly HttpClient _httpClient;
    private string _instanceId = Guid.NewGuid().ToString("N")[..8];
    private bool _isConnected;
    private string _apiEndpoint = "http://localhost:5001";

    public string InstanceId => _instanceId;
    public string IdeName => "VS Code";
    public string IdeVersion => "1.90.0+";
    public IdeCapabilities Capabilities { get; } = new()
    {
        SupportsSolutionManagement = true,
        SupportsProjectManagement = true,
        SupportsFileOperations = true,
        SupportsBuild = true,
        SupportsDebug = true,
        SupportsRefactoring = false, // Limited via API
        SupportsIntelliSense = false, // Limited via API
        SupportsSourceControl = true,
        SupportsExtensions = true
    };

    public bool IsConnected => _isConnected;

    public IdeProcessInfo ProcessInfo => new()
    {
        ProcessId = 0, // Not tracked
        ProcessName = "Code",
        StartTime = DateTime.Now
    };

    public event EventHandler<SolutionEventArgs>? SolutionOpened;
    public event EventHandler<SolutionEventArgs>? SolutionClosed;
    public event EventHandler<BuildEventArgs>? BuildCompleted;
    public event EventHandler<DebugEventArgs>? DebugStateChanged;
    public event EventHandler? Disconnected;

    public VsCodeAdapter(ILogger<VsCodeAdapter> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> ConnectAsync(ConnectionOptions options)
    {
        try
        {
            _logger.LogInformation("Connecting to VS Code...");

            // VS Code adapter requires the VS Code extension to be running
            // The extension exposes an HTTP API on localhost:5001
            if (!string.IsNullOrEmpty(options.SolutionPath))
            {
                // Try to open workspace
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiEndpoint}/api/workspace/open",
                    new { path = options.SolutionPath });
                
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Connected to VS Code");
                    return true;
                }
            }
            else
            {
                // Just check if extension is available
                var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/status");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Connected to VS Code");
                    return true;
                }
            }

            _logger.LogError("VS Code extension not responding. Make sure universal-vsmcp extension is installed and running.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to VS Code");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _isConnected = false;
            _httpClient.Dispose();
            Disconnected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Disconnected from VS Code");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from VS Code");
        }
        
        await Task.CompletedTask;
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/status");
            return response.IsSuccessStatusCode;
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
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/workspace");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var workspace = JsonSerializer.Deserialize<VsCodeWorkspace>(content);
                
                if (workspace != null)
                {
                    return new SolutionInfo
                    {
                        Name = workspace.Name,
                        FullPath = workspace.Path,
                        IsOpen = !string.IsNullOrEmpty(workspace.Path),
                        ProjectCount = workspace.Folders?.Count ?? 0
                    };
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VS Code workspace");
            return null;
        }
    }

    public async Task<bool> OpenSolutionAsync(string solutionPath)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/workspace/open",
                new { path = solutionPath });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open workspace in VS Code");
            return false;
        }
    }

    public async Task<bool> CloseSolutionAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/workspace/close", 
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close workspace in VS Code");
            return false;
        }
    }

    public async Task<SolutionInfo?> CreateSolutionAsync(string solutionPath, string template)
    {
        // VS Code doesn't have direct solution creation
        // Would need to use 'dotnet new' or scaffolding
        _logger.LogWarning("CreateSolution not fully implemented for VS Code adapter");
        return await GetSolutionAsync();
    }

    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync()
    {
        var projects = new List<ProjectInfo>();
        
        try
        {
            // Scan workspace for .csproj files
            var workspace = await GetSolutionAsync();
            if (workspace?.FullPath != null)
            {
                var projectFiles = Directory.GetFiles(workspace.FullPath, "*.csproj", SearchOption.AllDirectories);
                foreach (var projectFile in projectFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);
                    projects.Add(new ProjectInfo
                    {
                        Name = projectName,
                        FullPath = projectFile,
                        Type = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", // C# project
                        Language = "C#",
                        IsStartupProject = false // Would need launch.json parsing
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get projects from VS Code workspace");
        }
        
        return projects;
    }

    public async Task<ProjectInfo?> GetStartupProjectAsync()
    {
        try
        {
            // Check launch.json for startup project
            var workspace = await GetSolutionAsync();
            if (workspace?.FullPath != null)
            {
                var launchJsonPath = Path.Combine(workspace.FullPath, ".vscode", "launch.json");
                if (File.Exists(launchJsonPath))
                {
                    var launchConfig = await File.ReadAllTextAsync(launchJsonPath);
                    // Parse launch.json to find startup program
                    // This is simplified - actual implementation would parse JSON
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get startup project from VS Code");
            return null;
        }
    }

    public async Task<bool> SetStartupProjectAsync(string projectName)
    {
        // Would need to modify launch.json
        _logger.LogWarning("SetStartupProject not fully implemented for VS Code");
        return await Task.FromResult(false);
    }

    public async Task<bool> AddFileToProjectAsync(string projectName, string filePath)
    {
        // VS Code doesn't have a strict project system like VS
        // Files are automatically part of the workspace
        return await Task.FromResult(true);
    }

    public async Task<IReadOnlyList<FileInfo>> GetProjectFilesAsync(string projectName)
    {
        var files = new List<FileInfo>();
        
        try
        {
            var project = (await GetProjectsAsync()).FirstOrDefault(p => p.Name == projectName);
            if (project != null)
            {
                var projectDir = Path.GetDirectoryName(project.FullPath);
                if (projectDir != null)
                {
                    var allFiles = Directory.GetFiles(projectDir, "*.*", SearchOption.AllDirectories);
                    files.AddRange(allFiles.Select(f => new FileInfo
                    {
                        Name = Path.GetFileName(f),
                        FullPath = f,
                        Extension = Path.GetExtension(f),
                        Size = new System.IO.FileInfo(f).Length,
                        LastModified = File.GetLastWriteTime(f)
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project files from VS Code");
        }
        
        return files;
    }

    #endregion

    #region File Operations

    public async Task<bool> OpenFileAsync(string filePath, int? line = null, int? column = null)
    {
        try
        {
            var request = new
            {
                path = filePath,
                line = line,
                column = column
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/document/open",
                request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in VS Code");
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
            // Use VS Code API if available, otherwise use File API
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/document/save",
                new { path = filePath, content = content });
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            
            // Fallback to direct file write
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
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/document/replace",
                new
                {
                    path = filePath,
                    search = searchText,
                    replace = replacement
                });
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            
            // Fallback
            var content = await ReadFileAsync(filePath);
            if (content != null)
            {
                var newContent = content.Replace(searchText, replacement);
                return await WriteFileAsync(filePath, newContent);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace in file {Path}", filePath);
            return false;
        }
    }

    public async Task<FileInfo?> GetFileInfoAsync(string filePath)
    {
        await Task.CompletedTask;
        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        return new FileInfo
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            Extension = fileInfo.Extension,
            Size = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime,
            IsOpen = false // Would need to query VS Code API
        };
    }

    public async Task<IReadOnlyList<SearchResult>> FindInFilesAsync(string searchText, string? filePattern = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/workspace/search",
                new
                {
                    query = searchText,
                    pattern = filePattern ?? "*.*"
                });
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<VsCodeSearchResult>>(content);
                return results?.Select(r => new SearchResult
                {
                    FilePath = r.Path,
                    Line = r.Range.Start.Line + 1, // VS Code uses 0-based
                    Column = r.Range.Start.Character + 1,
                    Text = r.Preview.Text,
                    LineText = r.Preview.Text
                }).ToList() ?? new List<SearchResult>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search in VS Code");
        }
        
        return new List<SearchResult>();
    }

    #endregion

    #region Build Operations

    public async Task<BuildResult> BuildSolutionAsync(BuildConfiguration? config = null)
    {
        return await ExecuteBuildAsync("build", config);
    }

    public async Task<BuildResult> RebuildSolutionAsync(BuildConfiguration? config = null)
    {
        return await ExecuteBuildAsync("rebuild", config);
    }

    public async Task<bool> CleanSolutionAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/tasks/execute",
                new StringContent("{\"task\":\"clean\"}"));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean in VS Code");
            return false;
        }
    }

    public async Task<BuildResult> BuildProjectAsync(string projectName, BuildConfiguration? config = null)
    {
        // VS Code builds at solution/workspace level
        return await BuildSolutionAsync(config);
    }

    public async Task<IReadOnlyList<string>> GetBuildConfigurationsAsync()
    {
        // VS Code uses tasks.json, not predefined configurations
        return new[] { "Debug", "Release" };
    }

    public async Task<bool> SetBuildConfigurationAsync(string configuration)
    {
        // Would need to modify launch.json or settings
        _logger.LogWarning("SetBuildConfiguration not fully implemented for VS Code");
        return await Task.FromResult(true);
    }

    public async Task<string> GetBuildOutputAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/tasks/output");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get build output from VS Code");
        }
        return "";
    }

    #endregion

    #region Debug Operations

    public async Task<bool> StartDebuggingAsync(DebugTarget? target = null)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/start",
                target != null 
                    ? JsonContent.Create(target)
                    : null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debugging in VS Code");
            return false;
        }
    }

    public async Task<bool> StopDebuggingAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/stop",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debugging in VS Code");
            return false;
        }
    }

    public async Task<bool> SetBreakpointAsync(string filePath, int line)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/debug/breakpoint",
                new { path = filePath, line = line - 1 }); // VS Code uses 0-based
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set breakpoint in VS Code");
            return false;
        }
    }

    public async Task<bool> RemoveBreakpointAsync(string filePath, int line)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/debug/breakpoint/remove",
                new { path = filePath, line = line - 1 });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove breakpoint in VS Code");
            return false;
        }
    }

    public async Task<bool> RemoveAllBreakpointsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/breakpoints/clear",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all breakpoints in VS Code");
            return false;
        }
    }

    public async Task<IReadOnlyList<BreakpointInfo>> GetBreakpointsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/debug/breakpoints");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var breakpoints = JsonSerializer.Deserialize<List<VsCodeBreakpoint>>(content);
                return breakpoints?.Select(b => new BreakpointInfo
                {
                    FilePath = b.Location.Path,
                    Line = b.Location.Line + 1,
                    Enabled = b.Enabled,
                    Condition = b.Condition
                }).ToList() ?? new List<BreakpointInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get breakpoints from VS Code");
        }
        return new List<BreakpointInfo>();
    }

    public async Task<bool> StepOverAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/stepOver",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step over in VS Code");
            return false;
        }
    }

    public async Task<bool> StepIntoAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/stepInto",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step into in VS Code");
            return false;
        }
    }

    public async Task<bool> StepOutAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/stepOut",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to step out in VS Code");
            return false;
        }
    }

    public async Task<bool> ContinueAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_apiEndpoint}/api/debug/continue",
                null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue in VS Code");
            return false;
        }
    }

    public async Task<DebugLocation?> GetDebugLocationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/debug/location");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var location = JsonSerializer.Deserialize<VsCodeDebugLocation>(content);
                if (location != null)
                {
                    return new DebugLocation
                    {
                        FilePath = location.Path,
                        Line = location.Line + 1,
                        Column = location.Column + 1,
                        Function = location.Function
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get debug location from VS Code");
        }
        return null;
    }

    #endregion

    #region IDE Commands

    public async Task<bool> ExecuteCommandAsync(string commandName, object? args = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/command",
                new { command = commandName, args = args });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in VS Code");
            return false;
        }
    }

    public async Task<IReadOnlyList<CommandInfo>> GetCommandsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/commands");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var commands = JsonSerializer.Deserialize<List<VsCodeCommand>>(content);
                return commands?.Select(c => new CommandInfo
                {
                    Name = c.Command,
                    DisplayName = c.Title,
                    Shortcut = c.Keybinding
                }).ToList() ?? new List<CommandInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get commands from VS Code");
        }
        return new List<CommandInfo>();
    }

    public async Task<IdeStatus> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiEndpoint}/api/status");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<VsCodeStatus>(content);
                if (status != null)
                {
                    return new IdeStatus
                    {
                        IsBusy = status.Busy,
                        CurrentOperation = status.Operation,
                        OpenDocumentCount = status.OpenDocuments?.Count ?? 0,
                        ActiveDocument = status.ActiveDocument
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status from VS Code");
        }
        
        return new IdeStatus();
    }

    #endregion

    #region Helper Methods

    private async Task<BuildResult> ExecuteBuildAsync(string task, BuildConfiguration? config)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiEndpoint}/api/tasks/execute",
                new
                {
                    task = task,
                    configuration = config?.Configuration ?? "Debug"
                });
            
            sw.Stop();
            
            var output = await GetBuildOutputAsync();
            var success = response.IsSuccessStatusCode && !output.Contains("error");
            
            return new BuildResult
            {
                Success = success,
                Output = output,
                Duration = sw.Elapsed,
                ErrorCount = success ? 0 : 1 // Simplified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed in VS Code");
            return new BuildResult { Success = false, Output = ex.Message };
        }
    }

    #endregion

    #region VS Code API Types

    private class VsCodeWorkspace
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public List<string>? Folders { get; set; }
    }

    private class VsCodeSearchResult
    {
        public string Path { get; set; } = "";
        public VsCodeRange Range { get; set; } = new();
        public VsCodePreview Preview { get; set; } = new();
    }

    private class VsCodeRange
    {
        public VsCodePosition Start { get; set; } = new();
        public VsCodePosition End { get; set; } = new();
    }

    private class VsCodePosition
    {
        public int Line { get; set; }
        public int Character { get; set; }
    }

    private class VsCodePreview
    {
        public string Text { get; set; } = "";
    }

    private class VsCodeBreakpoint
    {
        public VsCodeBreakpointLocation Location { get; set; } = new();
        public bool Enabled { get; set; }
        public string? Condition { get; set; }
    }

    private class VsCodeBreakpointLocation
    {
        public string Path { get; set; } = "";
        public int Line { get; set; }
    }

    private class VsCodeDebugLocation
    {
        public string Path { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public string Function { get; set; } = "";
    }

    private class VsCodeCommand
    {
        public string Command { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Keybinding { get; set; }
    }

    private class VsCodeStatus
    {
        public bool Busy { get; set; }
        public string? Operation { get; set; }
        public List<string>? OpenDocuments { get; set; }
        public string? ActiveDocument { get; set; }
    }

    #endregion
}
