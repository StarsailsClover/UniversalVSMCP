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
/// Build Tools - Unified build operations using IIdeAdapter
/// </summary>
[McpServerToolType]
public class BuildTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<BuildTools> _logger;

    public BuildTools(IdeRouter ideRouter, ILogger<BuildTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Build the current solution
    /// </summary>
    [McpServerTool(Name = "build_solution",
        Title = "Build the current solution")]
    public async Task<BuildOperationResult> BuildSolution(
        string? configuration = null,
        string? platform = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Building solution");

            var buildConfig = new BuildConfiguration
            {
                Configuration = configuration ?? "Debug",
                Platform = platform ?? "Any CPU"
            };

            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new BuildOperationResult
                {
                    Success = false,
                    Message = "No IDE with build support available"
                };
            }

            var result = await adapter.BuildSolutionAsync(buildConfig);

            return new BuildOperationResult
            {
                Success = result.Success,
                Message = result.Success ? "Build succeeded" : "Build failed",
                Output = result.Output,
                ErrorCount = result.ErrorCount,
                WarningCount = result.WarningCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed");
            return new BuildOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Rebuild the current solution
    /// </summary>
    [McpServerTool(Name = "rebuild_solution",
        Title = "Rebuild the current solution")]
    public async Task<BuildOperationResult> RebuildSolution(
        string? configuration = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Rebuilding solution");

            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new BuildOperationResult
                {
                    Success = false,
                    Message = "No IDE with build support available"
                };
            }

            var result = await adapter.RebuildSolutionAsync(new BuildConfiguration { Configuration = configuration ?? "Debug" });

            return new BuildOperationResult
            {
                Success = result.Success,
                Message = result.Success ? "Rebuild succeeded" : "Rebuild failed",
                Output = result.Output,
                ErrorCount = result.ErrorCount,
                WarningCount = result.WarningCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rebuild failed");
            return new BuildOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Clean the current solution
    /// </summary>
    [McpServerTool(Name = "clean_solution",
        Title = "Clean the current solution")]
    public async Task<OperationResult> CleanSolution(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Cleaning solution");

            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "No IDE with build support available"
                };
            }

            var success = await adapter.CleanSolutionAsync();

            return new OperationResult
            {
                Success = success,
                Message = success ? "Clean succeeded" : "Clean failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clean failed");
            return new OperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get available build configurations
    /// </summary>
    [McpServerTool(Name = "get_build_configurations",
        Title = "Get available build configurations")]
    public async Task<ConfigurationListResult> GetBuildConfigurations(CancellationToken ct = default)
    {
        try
        {
            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new ConfigurationListResult
                {
                    Configurations = new[] { "Debug", "Release" },
                    Current = "Debug"
                };
            }

            var configs = await adapter.GetBuildConfigurationsAsync();
            
            return new ConfigurationListResult
            {
                Configurations = configs.ToArray(),
                Current = configs.FirstOrDefault() ?? "Debug"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get build configurations");
            return new ConfigurationListResult
            {
                Configurations = new[] { "Debug", "Release" },
                Current = "Debug"
            };
        }
    }

    /// <summary>
    /// Set active build configuration
    /// </summary>
    [McpServerTool(Name = "set_build_configuration",
        Title = "Set the active build configuration")]
    public async Task<OperationResult> SetBuildConfiguration(string configuration, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Setting build configuration: {Config}", configuration);

            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "No IDE with build support available"
                };
            }

            var success = await adapter.SetBuildConfigurationAsync(configuration);

            return new OperationResult
            {
                Success = success,
                Message = success ? $"Configuration set to: {configuration}" : "Failed to set configuration"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set build configuration");
            return new OperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Build a specific project
    /// </summary>
    [McpServerTool(Name = "build_project",
        Title = "Build a specific project")]
    public async Task<BuildOperationResult> BuildProject(
        string projectName,
        string? configuration = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Building project: {Project}", projectName);

            var criteria = new RoutingCriteria { PreferBuildSupport = true };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new BuildOperationResult
                {
                    Success = false,
                    Message = "No IDE with build support available"
                };
            }

            var result = await adapter.BuildProjectAsync(projectName, new BuildConfiguration { Configuration = configuration ?? "Debug" });

            return new BuildOperationResult
            {
                Success = result.Success,
                Message = result.Success ? $"Project {projectName} built successfully" : $"Failed to build {projectName}",
                Output = result.Output,
                ErrorCount = result.ErrorCount,
                WarningCount = result.WarningCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build project");
            return new BuildOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}

// Result types
public class BuildOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Output { get; set; } = "";
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}

public class ConfigurationListResult
{
    public string[] Configurations { get; set; } = Array.Empty<string>();
    public string Current { get; set; } = "";
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
