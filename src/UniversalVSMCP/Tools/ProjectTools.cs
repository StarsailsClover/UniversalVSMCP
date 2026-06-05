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
/// Project Tools - Unified project operations using IIdeAdapter
/// </summary>
[McpServerToolType]
public class ProjectTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<ProjectTools> _logger;

    public ProjectTools(IdeRouter ideRouter, ILogger<ProjectTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Get all files in a project
    /// </summary>
    [McpServerTool(Name = "get_project_files",
        Title = "Get all files in a project")]
    public async Task<ProjectFilesResult> GetProjectFiles(string projectName, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting files for project: {Name}", projectName);

            var adapter = await _ideRouter.GetAdapterAsync(new RoutingCriteria());
            if (adapter == null)
            {
                return new ProjectFilesResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var files = await adapter.GetProjectFilesAsync(projectName);

            return new ProjectFilesResult
            {
                Success = true,
                Files = files.Select(f => new FileItem
                {
                    Name = f.Name,
                    Path = f.FullPath,
                    Extension = f.Extension,
                    Size = f.Size
                }).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project files");
            return new ProjectFilesResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Add file to project
    /// </summary>
    [McpServerTool(Name = "add_file_to_project",
        Title = "Add a file to a project")]
    public async Task<OperationResult> AddFileToProject(string projectName, string filePath, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding file {File} to project {Project}", filePath, projectName);

            var adapter = await _ideRouter.GetAdapterAsync(new RoutingCriteria());
            if (adapter == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var success = await adapter.AddFileToProjectAsync(projectName, filePath);

            return new OperationResult
            {
                Success = success,
                Message = success ? $"File added to {projectName}" : "Failed to add file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add file to project");
            return new OperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get project properties
    /// </summary>
    [McpServerTool(Name = "get_project_properties",
        Title = "Get properties of a project")]
    public async Task<ProjectPropertiesResult> GetProjectProperties(string projectName, CancellationToken ct = default)
    {
        try
        {
            var projects = await _ideRouter.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Name == projectName);

            if (project == null)
            {
                return new ProjectPropertiesResult
                {
                    Success = false,
                    Message = $"Project not found: {projectName}"
                };
            }

            return new ProjectPropertiesResult
            {
                Success = true,
                Name = project.Name,
                FullPath = project.FullPath,
                Type = project.Type,
                Language = project.Language,
                IsStartupProject = project.IsStartupProject,
                FileCount = project.Files.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project properties");
            return new ProjectPropertiesResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}

// Result types
public class ProjectFilesResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public FileItem[] Files { get; set; } = Array.Empty<FileItem>();
}

public class FileItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Extension { get; set; } = "";
    public long Size { get; set; }
}

public class ProjectPropertiesResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Type { get; set; } = "";
    public string Language { get; set; } = "";
    public bool IsStartupProject { get; set; }
    public int FileCount { get; set; }
}
