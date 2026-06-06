using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UniversalVSMCP.IdeAbstraction;
using UniversalVSMCP.IdeRouting;
using IOFileInfo = System.IO.FileInfo;

namespace UniversalVSMCP.Tools;

/// <summary>
/// File Tools - Unified file operations using IIdeAdapter
/// </summary>
[McpServerToolType]
public class FileTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<FileTools> _logger;

    public FileTools(IdeRouter ideRouter, ILogger<FileTools> logger)
    {
        _ideRouter = ideRouter;
        _logger = logger;
    }

    /// <summary>
    /// Open file in IDE
    /// </summary>
    [McpServerTool(Name = "open_file",
        Title = "Open a file in the IDE")]
    public async Task<FileOperationResult> OpenFile(string filePath, int? line = null, int? column = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening file: {Path}", filePath);

            var criteria = new RoutingCriteria { FilePath = filePath };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var success = await adapter.OpenFileAsync(filePath, line, column);

            return new FileOperationResult
            {
                Success = success,
                Message = success ? $"Opened: {filePath}" : "Failed to open file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file");
            return new FileOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Read file content
    /// </summary>
    [McpServerTool(Name = "read_file",
        Title = "Read file content")]
    public async Task<FileContentResult> ReadFile(string filePath, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Reading file: {Path}", filePath);

            var criteria = new RoutingCriteria { FilePath = filePath };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new FileContentResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var content = await adapter.ReadFileAsync(filePath);

            if (content == null)
            {
                return new FileContentResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }

            return new FileContentResult
            {
                Success = true,
                Content = content,
                LineCount = content.Split('\n').Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file");
            return new FileContentResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Write file content
    /// </summary>
    [McpServerTool(Name = "write_file",
        Title = "Write content to file")]
    public async Task<FileOperationResult> WriteFile(string filePath, string content, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Writing file: {Path}", filePath);

            var criteria = new RoutingCriteria { FilePath = filePath };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var success = await adapter.WriteFileAsync(filePath, content);

            return new FileOperationResult
            {
                Success = success,
                Message = success ? $"Written: {filePath}" : "Failed to write file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file");
            return new FileOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Replace text in file
    /// </summary>
    [McpServerTool(Name = "replace_in_file",
        Title = "Replace text in file")]
    public async Task<FileOperationResult> ReplaceInFile(string filePath, string searchText, string replacement, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Replacing in file: {Path}", filePath);

            var criteria = new RoutingCriteria { FilePath = filePath };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = "No IDE available"
                };
            }

            var success = await adapter.ReplaceInFileAsync(filePath, searchText, replacement);

            return new FileOperationResult
            {
                Success = success,
                Message = success ? $"Replaced in: {filePath}" : "Failed to replace in file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace in file");
            return new FileOperationResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Get file information
    /// </summary>
    [McpServerTool(Name = "get_file_info",
        Title = "Get file information")]
    public async Task<FileInfoResult> GetFileInfo(string filePath, CancellationToken ct = default)
    {
        try
        {
            var criteria = new RoutingCriteria { FilePath = filePath };
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                // Fallback to File API
                if (!File.Exists(filePath))
                {
                    return new FileInfoResult
                    {
                        Exists = false
                    };
                }

                var info = new IOFileInfo(filePath);
                return new FileInfoResult
                {
                    Exists = true,
                    Name = info.Name,
                    FullPath = info.FullName,
                    Extension = info.Extension,
                    Size = info.Length,
                    LastModified = info.LastWriteTime
                };
            }

            var fileInfo = await adapter.GetFileInfoAsync(filePath);

            if (fileInfo == null)
            {
                return new FileInfoResult
                {
                    Exists = false
                };
            }

            return new FileInfoResult
            {
                Exists = true,
                Name = fileInfo.Name,
                FullPath = fileInfo.FullPath,
                Extension = fileInfo.Extension,
                Size = fileInfo.Size,
                LastModified = fileInfo.LastModified,
                IsOpen = fileInfo.IsOpen
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file info");
            return new FileInfoResult
            {
                Exists = false
            };
        }
    }

    /// <summary>
    /// Find text in files
    /// </summary>
    [McpServerTool(Name = "find_in_files",
        Title = "Find text in solution files")]
    public async Task<FindResult> FindInFiles(string searchText, string? filePattern = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Finding: {Text}", searchText);

            var criteria = new RoutingCriteria();
            var adapter = await _ideRouter.GetAdapterAsync(criteria);
            
            if (adapter == null)
            {
                return new FindResult
                {
                    Results = Array.Empty<SearchResult>()
                };
            }

            var results = await adapter.FindInFilesAsync(searchText, filePattern);

            return new FindResult
            {
                Results = results.Select(r => new SearchResult
                {
                    FilePath = r.FilePath,
                    Line = r.Line,
                    Column = r.Column,
                    Text = r.Text,
                    LineText = r.LineText
                }).ToArray(),
                TotalCount = results.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find in files");
            return new FindResult
            {
                Results = Array.Empty<SearchResult>()
            };
        }
    }
}

// Result types
public class FileOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class FileContentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Content { get; set; } = "";
    public int LineCount { get; set; }
}

public class FileInfoResult
{
    public bool Exists { get; set; }
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Extension { get; set; } = "";
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsOpen { get; set; }
}

public class FindResult
{
    public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();
    public int TotalCount { get; set; }
}

public class SearchResult
{
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Text { get; set; } = "";
    public string LineText { get; set; } = "";
}
