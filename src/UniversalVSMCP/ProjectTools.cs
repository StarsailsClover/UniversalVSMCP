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
/// Tools for Visual Studio Project-level operations
/// </summary>
[McpServerToolType]
public class ProjectTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<ProjectTools> _logger;

    public ProjectTools(IVsConnectionManager vsManager, ILogger<ProjectTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all files in a specific project
    /// </summary>
    [McpServerTool(Name = "get_project_files", Title = "Get all files and folders in a specific project. Returns project items with their types and paths.")]
    public async Task<IEnumerable<ProjectItemInfo>> GetProjectFiles(string projectName, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return Enumerable.Empty<ProjectItemInfo>();
        }

        var project = FindProjectByName(dte.Solution, projectName);
        if (project == null)
        {
            _logger.LogWarning("Project not found: {ProjectName}", projectName);
            return Enumerable.Empty<ProjectItemInfo>();
        }

        return EnumerateProjectItems(project);
    }

    /// <summary>
    /// Get the startup project(s) of the solution
    /// </summary>
    [McpServerTool(Name = "get_startup_projects", Title = "Get the name(s) of the startup project(s) in the current solution.")]
    public async Task<IEnumerable<string>> GetStartupProjects(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var startupProjects = dte.Solution.SolutionBuild.StartupProjects as object[];
            if (startupProjects != null)
            {
                return startupProjects.Cast<string>().ToList();
            }
        }
        catch
        {
            // Fallback for single startup project
            try
            {
                string? startup = dte.Solution.SolutionBuild.StartupProjects as string;
                if (!string.IsNullOrEmpty(startup))
                {
                    return new[] { startup };
                }
            }
            catch { }
        }
        
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Add an existing file to a project
    /// </summary>
    [McpServerTool(Name = "add_file_to_project", Title = "Add an existing file to a specified project in the solution.")]
    public async Task<OperationResult> AddFileToProject(string projectName, string filePath, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return OperationResult.Failure("No solution is open");
        }

        var project = FindProjectByName(dte.Solution, projectName);
        if (project == null)
        {
            return OperationResult.Failure($"Project '{projectName}' not found");
        }

        try
        {
            project.ProjectItems.AddFromFile(filePath);
            _logger.LogInformation("Added file {File} to project {Project}", filePath, projectName);
            return OperationResult.IsSuccess($"File added to project: {System.IO.Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add file to project");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the startup project
    /// </summary>
    [McpServerTool(Name = "set_startup_project", Title = "Set a specific project as the startup project for the solution.")]
    public async Task<OperationResult> SetStartupProject(string projectName, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return OperationResult.Failure("No solution is open");
        }

        try
        {
            var project = FindProjectByName(dte.Solution, projectName);
            if (project == null)
            {
                return OperationResult.Failure($"Project '{projectName}' not found");
            }

            dte.Solution.SolutionBuild.StartupProjects = project.UniqueName;
            _logger.LogInformation("Set startup project to: {Project}", projectName);
            return OperationResult.IsSuccess($"Startup project set to: {projectName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set startup project");
            return OperationResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get project properties
    /// </summary>
    [McpServerTool(Name = "get_project_properties", Title = "Get key properties of a project (OutputType, TargetFramework, AssemblyName, etc.)")]
    public async Task<Dictionary<string, string>> GetProjectProperties(string projectName, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return new Dictionary<string, string>();
        }

        var project = FindProjectByName(dte.Solution, projectName);
        if (project == null)
        {
            return new Dictionary<string, string>();
        }

        var properties = new Dictionary<string, string>();
        
        try
        {
            foreach (Property prop in project.Properties)
            {
                try
                {
                    properties[prop.Name] = prop.Value?.ToString() ?? string.Empty;
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading project properties");
        }
        
        return properties;
    }

    private Project? FindProjectByName(Solution solution, string projectName)
    {
        foreach (Project project in solution.Projects)
        {
            if (project.Name.Equals(projectName, System.StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
            // Check in solution folders
            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                var found = FindInSolutionFolder(project.ProjectItems, projectName);
                if (found != null) return found;
            }
        }
        return null;
    }

    private Project? FindInSolutionFolder(ProjectItems items, string projectName)
    {
        foreach (ProjectItem item in items)
        {
            if (item.SubProject != null && 
                item.SubProject.Name.Equals(projectName, System.StringComparison.OrdinalIgnoreCase))
            {
                return item.SubProject;
            }
            // Recursive check for nested solution folders
            if (item.SubProject?.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                var found = FindInSolutionFolder(item.SubProject.ProjectItems, projectName);
                if (found != null) return found;
            }
        }
        return null;
    }

    private IEnumerable<ProjectItemInfo> EnumerateProjectItems(Project project)
    {
        foreach (ProjectItem item in project.ProjectItems)
        {
            yield return MapProjectItem(item, 0);
            // Recurse into sub-items
            foreach (var child in EnumerateSubItems(item, 1))
            {
                yield return child;
            }
        }
    }

    private IEnumerable<ProjectItemInfo> EnumerateSubItems(ProjectItem parent, int depth)
    {
        foreach (ProjectItem item in parent.ProjectItems)
        {
            yield return MapProjectItem(item, depth);
            foreach (var child in EnumerateSubItems(item, depth + 1))
            {
                yield return child;
            }
        }
    }

    private ProjectItemInfo MapProjectItem(ProjectItem item, int depth)
    {
        var info = new ProjectItemInfo
        {
            Name = item.Name,
            Depth = depth,
            ItemType = GetItemType(item)
        };

        try
        {
            if (item.FileCount > 0)
            {
                for (short i = 1; i <= item.FileCount; i++)
                {
                    string? filePath = item.FileNames[i];
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        info.FilePaths.Add(filePath);
                    }
                }
            }
        }
        catch
        {
            // Ignore file access errors
        }

        return info;
    }

    private string GetItemType(ProjectItem item)
    {
        try
        {
            return item.Kind switch
            {
                EnvDTE.Constants.vsProjectItemKindPhysicalFile => "File",
                EnvDTE.Constants.vsProjectItemKindPhysicalFolder => "Folder",
                EnvDTE.Constants.vsProjectItemKindSubProject => "SubProject",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }
}

public class ProjectItemInfo
{
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public List<string> FilePaths { get; set; } = new();
}
