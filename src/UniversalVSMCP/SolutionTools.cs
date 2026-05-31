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
/// Tools for Visual Studio Solution operations
/// Provides comprehensive solution management capabilities for AI Agents
/// </summary>
[McpServerToolType]
public class SolutionTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<SolutionTools> _logger;

    public SolutionTools(IVsConnectionManager vsManager, ILogger<SolutionTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects in the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_projects", Title = "Get all projects in the currently open Visual Studio solution. Returns project names, types, and file paths.")]
    public async Task<IEnumerable<ProjectInfo>> GetSolutionProjects(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            _logger.LogWarning("No solution is currently open");
            return Enumerable.Empty<ProjectInfo>();
        }

        var projects = new List<ProjectInfo>();
        
        foreach (Project project in dte.Solution.Projects)
        {
            projects.Add(MapProject(project));
            // Recursively get sub-projects (solution folders)
            AddSubProjects(project, projects);
        }
        
        _logger.LogInformation("Found {Count} projects in solution", projects.Count);
        return projects;
    }

    /// <summary>
    /// Get the full path of the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_path", Title = "Get the full file path of the currently open Visual Studio solution (.sln file).")]
    public async Task<string?> GetSolutionPath(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return null;
        }
        
        return dte.Solution.FullName;
    }

    /// <summary>
    /// Get the name of the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_name", Title = "Get the name of the currently open Visual Studio solution (without path).")]
    public async Task<string?> GetSolutionName(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return null;
        }
        
        return System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
    }

    /// <summary>
    /// Open a solution file in Visual Studio
    /// </summary>
    [McpServerTool(Name = "open_solution", Title = "Open a solution file (.sln) in Visual Studio.")]
    public async Task<OperationResult> OpenSolution(string solutionPath, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }
        
        try
        {
            dte.Solution.Open(solutionPath);
            _logger.LogInformation("Opened solution: {Path}", solutionPath);
            return OperationResult.Success($"Solution opened: {System.IO.Path.GetFileName(solutionPath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open solution: {Path}", solutionPath);
            return OperationResult.Failure($"Failed to open solution: {ex.Message}");
        }
    }

    /// <summary>
    /// Close the current solution
    /// </summary>
    [McpServerTool(Name = "close_solution", Title = "Close the currently open Visual Studio solution.")]
    public async Task<OperationResult> CloseSolution(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return OperationResult.Failure("No solution is currently open");
        }
        
        try
        {
            dte.Solution.Close(false); // false = don't save changes
            _logger.LogInformation("Solution closed");
            return OperationResult.Success("Solution closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close solution");
            return OperationResult.Failure($"Failed to close solution: {ex.Message}");
        }
    }

    private void AddSubProjects(Project parentProject, List<ProjectInfo> projects)
    {
        try
        {
            // Solution folders contain sub-projects
            if (parentProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (ProjectItem item in parentProject.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        projects.Add(MapProject(item.SubProject));
                        AddSubProjects(item.SubProject, projects);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors in sub-project enumeration
        }
    }

    private ProjectInfo MapProject(Project project)
    {
        return new ProjectInfo
        {
            Name = project.Name,
            Kind = project.Kind,
            KindName = GetProjectKindName(project.Kind),
            FullPath = project.FullName,
            UniqueName = project.UniqueName,
            IsDirty = project.IsDirty,
            BuildState = GetBuildState(project)
        };
    }

    private string GetProjectKindName(string kind)
    {
        return kind switch
        {
            "{66A32650-B612-4B97-9D84-8D06E99F3C88}" => "Solution Folder",
            "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" => "C# Project",
            "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" => "VB.NET Project",
            "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" => "C++ Project",
            "{E24C65DC-7377-472B-9ABA-BC803B73C61A}" => "Web Project",
            _ => $"Unknown ({kind})"
        };
    }

    private string GetBuildState(Project project)
    {
        try
        {
            // Access build state through ConfigurationManager
            var config = project.ConfigurationManager?.ActiveConfiguration;
            return config?.ConfigurationName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

#region Data Models

public class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string KindName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public bool IsDirty { get; set; }
    public string BuildState { get; set; } = string.Empty;
}

public class OperationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    
    public static OperationResult Success(string message) => new OperationResult 
    { 
        IsSuccess = true, 
        Message = message 
    };
    
    public static OperationResult Failure(string message) => new OperationResult 
    { 
        IsSuccess = false, 
        Message = message 
    };
}

#endregion
