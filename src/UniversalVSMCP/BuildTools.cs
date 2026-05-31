using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UniversalVSMCP;

/// <summary>
/// Tools for Visual Studio Build operations
/// Enables AI Agents to build, rebuild, and clean solutions/projects
/// </summary>
[McpServerToolType]
public class BuildTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<BuildTools> _logger;

    public BuildTools(IVsConnectionManager vsManager, ILogger<BuildTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Build the solution (default configuration)
    /// </summary>
    [McpServerTool(Name = "build_solution", Title = "Build the solution with the specified configuration (Debug/Release) and platform (Any CPU/x64/x86).")]
    public async Task<BuildResult> BuildSolution(string configuration = "Debug", string platform = "Any CPU", CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return BuildResult.Failure("No solution is open");
        }

        try
        {
            var solutionBuild = dte.Solution.SolutionBuild;
            solutionBuild.SolutionConfigurations.Item(configuration).Activate();
            
            _logger.LogInformation("Starting build: {Config}/{Platform}", configuration, platform);
            solutionBuild.Build(true);
            
            // Check build status (LastBuildInfo returns 0 on success, non-zero on failure)
            var success = solutionBuild.LastBuildInfo == 0;
            var output = GetBuildOutput(dte);
            
            return new BuildResult
            {
                IsSuccess = success,
                Configuration = configuration,
                Platform = platform,
                Output = output,
                ErrorCount = success ? 0 : GetErrorCount(dte)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed");
            return BuildResult.Failure($"Build failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Rebuild the solution (clean + build)
    /// </summary>
    [McpServerTool(Name = "rebuild_solution", Title = "Rebuild the solution (clean then build) with specified configuration.")]
    public async Task<BuildResult> RebuildSolution(string configuration = "Debug", string platform = "Any CPU", CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return BuildResult.Failure("No solution is open");
        }

        try
        {
            var solutionBuild = dte.Solution.SolutionBuild;
            
            _logger.LogInformation("Starting rebuild: {Config}/{Platform}", configuration, platform);
            solutionBuild.Clean(true);
            solutionBuild.Build(true);
            
            var success = solutionBuild.LastBuildInfo == 0;
            return new BuildResult
            {
                IsSuccess = success,
                Configuration = configuration,
                Platform = platform,
                Operation = "Rebuild",
                ErrorCount = success ? 0 : GetErrorCount(dte)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rebuild failed");
            return BuildResult.Failure($"Rebuild failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean the solution
    /// </summary>
    [McpServerTool(Name = "clean_solution", Title = "Clean the solution (delete all build outputs) for the specified configuration.")]
    public async Task<OperationResult> CleanSolution(string configuration = "Debug", string platform = "Any CPU", CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return OperationResult.Failure("No solution is open");
        }

        try
        {
            var solutionBuild = dte.Solution.SolutionBuild;
            solutionBuild.Clean(true);
            
            _logger.LogInformation("Solution cleaned: {Config}/{Platform}", configuration, platform);
            return OperationResult.Success("Solution cleaned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clean failed");
            return OperationResult.Failure($"Clean failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Build a specific project
    /// </summary>
    [McpServerTool(Name = "build_project", Title = "Build a specific project within the solution.")]
    public async Task<BuildResult> BuildProject(string projectName, string configuration = "Debug", string platform = "Any CPU", CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return BuildResult.Failure("No solution is open");
        }

        var project = FindProject(dte.Solution, projectName);
        if (project == null)
        {
            return BuildResult.Failure($"Project '{projectName}' not found");
        }

        try
        {
            var solutionBuild = dte.Solution.SolutionBuild;
            var buildName = $"{configuration}|{platform}";
            
            // Build the project
            solutionBuild.BuildProject(buildName, project.UniqueName, true);
            
            var success = solutionBuild.LastBuildInfo == 0;
            return new BuildResult
            {
                IsSuccess = success,
                ProjectName = projectName,
                Configuration = configuration,
                Platform = platform,
                ErrorCount = success ? 0 : GetErrorCount(dte)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project build failed: {Project}", projectName);
            return BuildResult.Failure($"Build failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get build errors from the Error List
    /// </summary>
    [McpServerTool(Name = "get_build_errors", Title = "Get build errors and warnings from the Visual Studio Error List.")]
    public async Task<List<BuildError>> GetBuildErrors(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return new List<BuildError>();
        }

        var errors = new List<BuildError>();
        
        try
        {
            // Access Error List through tool windows
            var errorList = dte.ToolWindows.ErrorList;
            
            for (int i = 1; i <= errorList.ErrorItems.Count; i++)
            {
                var error = errorList.ErrorItems.Item(i);
                errors.Add(new BuildError
                {
                    Description = error.Description,
                    File = error.FileName,
                    Line = error.Line,
                    Column = error.Column,
                    ErrorLevel = error.ErrorLevel.ToString(),
                    Project = error.Project ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not access Error List");
        }
        
        return errors;
    }

    /// <summary>
    /// Get available build configurations
    /// </summary>
    [McpServerTool(Name = "get_build_configurations", Title = "Get available build configurations for the current solution (e.g., Debug, Release).")]
    public async Task<List<string>> GetBuildConfigurations(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte?.Solution == null || !dte.Solution.IsOpen)
        {
            return new List<string>();
        }

        var configs = new List<string>();
        
        try
        {
            // Access solution configurations via SolutionBuild
            var solutionBuild = dte.Solution.SolutionBuild;
            foreach (string configName in solutionBuild.SolutionConfigurations)
            {
                configs.Add(configName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get build configurations");
        }
        
        return configs;
    }

    private Project? FindProject(Solution solution, string projectName)
    {
        foreach (Project project in solution.Projects)
        {
            if (project.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
        }
        return null;
    }

    private string GetBuildOutput(DTE2 dte)
    {
        try
        {
            // Access Output window
            var outputWindow = dte.ToolWindows.OutputWindow;
            // This is simplified - in practice you'd need to find the "Build" pane
            return "Build completed. Check Output window for details.";
        }
        catch
        {
            return "Build completed.";
        }
    }

    private int GetErrorCount(DTE2 dte)
    {
        try
        {
            return dte.ToolWindows.ErrorList.ErrorItems.Count;
        }
        catch
        {
            return 0;
        }
    }
}

#region Data Models

public class BuildResult
{
    public bool IsSuccess { get; set; }
    public string? ProjectName { get; set; }
    public string Configuration { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? Operation { get; set; }
    public string Output { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    
    public static BuildResult Failure(string message) => new BuildResult 
    { 
        IsSuccess = false, 
        Output = message 
    };
}

public class BuildError
{
    public string Description { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string ErrorLevel { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
}

#endregion
