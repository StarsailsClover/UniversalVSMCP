using System;
using System.Collections.Generic;
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
/// Solution Tools - Unified implementation using IIdeAdapter
/// Works with Visual Studio, VS Code, and future IDEs
/// </summary>
[McpServerToolType]
public class SolutionTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<SolutionTools> _logger;

    public SolutionTools(IdeRouter ideRouter, ILogger<SolutionTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects in the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_projects", 
        Title = "Get all projects in the currently open solution. Returns project names, types, and file paths.")]
    public async Task<IEnumerable<ProjectInfo>> GetSolutionProjects(CancellationToken ct = default)
    {
        try
        {
            var solution = await _ideRouter.GetSolutionAsync();
            if (solution == null)
            {
                _logger.LogWarning("No solution is currently open");
                return Enumerable.Empty<ProjectInfo>();
            }

            var projects = await _ideRouter.GetProjectsAsync();
            return projects.Select(MapProjectInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get solution projects");
            throw;
        }
    }

    /// <summary>
    /// Get the path of the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_path",
        Title = "Get the full path of the currently open solution file.")]
    public async Task<string> GetSolutionPath(CancellationToken ct = default)
    {
        try
        {
            var solution = await _ideRouter.GetSolutionAsync();
            if (solution == null)
            {
                _logger.LogWarning("No solution is currently open");
                return string.Empty;
            }

            return solution.FullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get solution path");
            throw;
        }
    }

    /// <summary>
    /// Get the name of the currently open solution
    /// </summary>
    [McpServerTool(Name = "get_solution_name",
        Title = "Get the name of the currently open solution.")]
    public async Task<string> GetSolutionName(CancellationToken ct = default)
    {
        try
        {
            var solution = await _ideRouter.GetSolutionAsync();
            if (solution == null)
            {
                _logger.LogWarning("No solution is currently open");
                return string.Empty;
            }

            return solution.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get solution name");
            throw;
        }
    }

    /// <summary>
    /// Get detailed information about the current solution
    /// </summary>
    [McpServerTool(Name = "get_solution_info",
        Title = "Get comprehensive information about the current solution including name, path, and project count.")]
    public async Task<SolutionDetail> GetSolutionInfo(CancellationToken ct = default)
    {
        try
        {
            var solution = await _ideRouter.GetSolutionAsync();
            if (solution == null)
            {
                return new SolutionDetail
                {
                    IsOpen = false,
                    Name = "No solution open"
                };
            }

            var projects = await _ideRouter.GetProjectsAsync();

            return new SolutionDetail
            {
                IsOpen = solution.IsOpen,
                Name = solution.Name,
                Path = solution.FullPath,
                ProjectCount = projects.Count,
                LastModified = solution.LastModified,
                ConnectedIde = GetConnectedIdeInfo()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get solution info");
            throw;
        }
    }

    /// <summary>
    /// Open a solution file
    /// </summary>
    [McpServerTool(Name = "open_solution",
        Title = "Open a solution file (.sln) in the connected IDE.")]
    public async Task<OperationResult> OpenSolution(string solutionPath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(solutionPath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Solution path cannot be empty"
                };
            }

            if (!File.Exists(solutionPath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Solution file not found: {solutionPath}"
                };
            }

            _logger.LogInformation("Opening solution: {Path}", solutionPath);

            var success = await _ideRouter.OpenSolutionAsync(solutionPath);

            return new OperationResult
            {
                Success = success,
                Message = success ? "Solution opened successfully" : "Failed to open solution"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open solution");
            return new OperationResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Close the current solution
    /// </summary>
    [McpServerTool(Name = "close_solution",
        Title = "Close the currently open solution.")]
    public async Task<OperationResult> CloseSolution(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Closing current solution");

            var success = await _ideRouter.CloseSolutionAsync();

            return new OperationResult
            {
                Success = success,
                Message = success ? "Solution closed successfully" : "Failed to close solution"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close solution");
            return new OperationResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Create a new solution/workspace
    /// </summary>
    [McpServerTool(Name = "create_solution",
        Title = "Create a new solution or workspace at the specified path.")]
    public async Task<OperationResult> CreateSolution(string path, string? template = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Path cannot be empty"
                };
            }

            _logger.LogInformation("Creating solution at: {Path} with template: {Template}", path, template ?? "default");

            // Note: CreateSolutionAsync is not fully implemented in all adapters
            // This would require project template support
            var solution = await _ideRouter.CreateSolutionAsync(path, template ?? "default");

            return new OperationResult
            {
                Success = solution != null,
                Message = solution != null 
                    ? $"Solution created at: {solution.FullPath}"
                    : "Failed to create solution (may not be supported by current IDE)",
                Data = solution
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create solution");
            return new OperationResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get startup project
    /// </summary>
    [McpServerTool(Name = "get_startup_projects",
        Title = "Get the current startup project(s).")]
    public async Task<IEnumerable<ProjectInfo>> GetStartupProjects(CancellationToken ct = default)
    {
        try
        {
            var startupProject = await _ideRouter.GetStartupProjectAsync();
            if (startupProject == null)
            {
                return Enumerable.Empty<ProjectInfo>();
            }

            return new[] { MapProjectInfo(startupProject) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get startup project");
            throw;
        }
    }

    /// <summary>
    /// Set startup project
    /// </summary>
    [McpServerTool(Name = "set_startup_project",
        Title = "Set the startup project for debugging.")]
    public async Task<OperationResult> SetStartupProject(string projectName, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Project name cannot be empty"
                };
            }

            _logger.LogInformation("Setting startup project: {Name}", projectName);

            var success = await _ideRouter.SetStartupProjectAsync(projectName);

            return new OperationResult
            {
                Success = success,
                Message = success 
                    ? $"Startup project set to: {projectName}"
                    : $"Failed to set startup project: {projectName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set startup project");
            return new OperationResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get connected IDE information
    /// </summary>
    [McpServerTool(Name = "get_connected_ides",
        Title = "Get information about all connected IDEs.")]
    public ConnectedIdesResult GetConnectedIdes()
    {
        try
        {
            var ides = _ideRouter.GetConnectedIdes();
            
            return new ConnectedIdesResult
            {
                Count = ides.Count,
                Ides = ides.Select(i => new IdeSummary
                {
                    InstanceId = i.InstanceId,
                    Name = i.Name,
                    Version = i.Version,
                    IsConnected = i.IsConnected,
                    Capabilities = i.Capabilities
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected IDEs");
            throw;
        }
    }

    #region Helper Methods

    private ProjectInfo MapProjectInfo(IdeAbstraction.ProjectInfo project)
    {
        return new ProjectInfo
        {
            Name = project.Name,
            FullName = project.FullPath,
            Language = project.Language,
            Type = project.Type,
            IsStartupProject = project.IsStartupProject,
            Files = project.Files.Select(f => f.FullPath).ToList()
        };
    }

    private List<IdeSummary> GetConnectedIdeInfo()
    {
        return _ideRouter.GetConnectedIdes()
            .Select(i => new IdeSummary
            {
                InstanceId = i.InstanceId,
                Name = i.Name,
                Version = i.Version,
                IsConnected = i.IsConnected
            })
            .ToList();
    }

    #endregion
}

// Response types
public class SolutionDetail
{
    public bool IsOpen { get; set; }
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public int ProjectCount { get; set; }
    public DateTime? LastModified { get; set; }
    public List<IdeSummary> ConnectedIde { get; set; } = new();
}

public class ConnectedIdesResult
{
    public int Count { get; set; }
    public List<IdeSummary> Ides { get; set; } = new();
}

public class IdeSummary
{
    public string InstanceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsConnected { get; set; }
    public IdeCapabilities Capabilities { get; set; } = new();
}
