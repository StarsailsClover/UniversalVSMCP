using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UniversalVSMCP;

/// <summary>
/// Tools for file operations within Visual Studio
/// Provides file reading, writing, and editing capabilities
/// </summary>
[McpServerToolType]
public class FileTools
{
    private readonly IVsConnectionManager _vsManager;
    private readonly ILogger<FileTools> _logger;

    public FileTools(IVsConnectionManager vsManager, ILogger<FileTools> logger)
    {
        _vsManager = vsManager;
        _logger = logger;
    }

    /// <summary>
    /// Open a file in Visual Studio editor
    /// </summary>
    [McpServerTool(Name = "open_file", Title = "Open a file in Visual Studio editor. Optionally specify a line number to navigate to.")]
    public async Task<OperationResult> OpenFile(string filePath, int? lineNumber = null, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var dte = _vsManager.GetActiveInstance();
        if (dte == null)
        {
            return OperationResult.Failure("Visual Studio is not connected");
        }

        if (!File.Exists(filePath))
        {
            return OperationResult.Failure($"File not found: {filePath}");
        }

        try
        {
            Window window = dte.ItemOperations.OpenFile(filePath, Constants.vsViewKindPrimary);
            
            if (lineNumber.HasValue && window.Selection is TextSelection selection)
            {
                selection.GotoLine(lineNumber.Value, false);
                _logger.LogInformation("Opened file {File} at line {Line}", filePath, lineNumber);
            }
            else
            {
                _logger.LogInformation("Opened file: {File}", filePath);
            }
            
            return OperationResult.Success($"Opened: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file: {File}", filePath);
            return OperationResult.Failure($"Failed to open file: {ex.Message}");
        }
    }

    /// <summary>
    /// Read the content of a text file
    /// </summary>
    [McpServerTool(Name = "read_file", Title = "Read the content of a text file. Returns up to maxLines lines (default 500).")]
    public async Task<FileReadResult> ReadFile(string filePath, int maxLines = 500, int startLine = 1, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        if (!File.Exists(filePath))
        {
            return new FileReadResult { IsSuccess = false, Error = $"File not found: {filePath}" };
        }

        try
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            var totalLines = lines.Length;
            
            // Apply line range
            var startIndex = Math.Max(0, startLine - 1);
            var endIndex = Math.Min(totalLines, startIndex + maxLines);
            var selectedLines = new List<string>();
            
            for (int i = startIndex; i < endIndex; i++)
            {
                selectedLines.Add(lines[i]);
            }
            
            return new FileReadResult
            {
                IsSuccess = true,
                FilePath = filePath,
                Content = string.Join("\n", selectedLines),
                TotalLines = totalLines,
                ReadLines = selectedLines.Count,
                StartLine = startLine,
                EndLine = endIndex
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file: {File}", filePath);
            return new FileReadResult { IsSuccess = false, Error = $"Failed to read file: {ex.Message}" };
        }
    }

    /// <summary>
    /// Write content to a file (with backup)
    /// </summary>
    [McpServerTool(Name = "write_file", Title = "Write content to a file. Creates backup of existing file if it exists.")]
    public async Task<OperationResult> WriteFile(string filePath, string content, bool createBackup = true, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        try
        {
            // Create backup if file exists
            if (createBackup && File.Exists(filePath))
            {
                var backupPath = filePath + ".bak";
                File.Copy(filePath, backupPath, true);
                _logger.LogInformation("Created backup: {Backup}", backupPath);
            }

            // Write content
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            
            _logger.LogInformation("Wrote {Chars} characters to {File}", content.Length, filePath);
            return OperationResult.Success($"File written: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file: {File}", filePath);
            return OperationResult.Failure($"Failed to write file: {ex.Message}");
        }
    }

    /// <summary>
    /// Replace text in a file
    /// </summary>
    [McpServerTool(Name = "replace_in_file", Title = "Replace occurrences of oldText with newText in a file. Returns number of replacements made.")]
    public async Task<ReplaceResult> ReplaceInFile(string filePath, string oldText, string newText, bool createBackup = true, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        if (!File.Exists(filePath))
        {
            return new ReplaceResult { IsSuccess = false, Error = $"File not found: {filePath}" };
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var occurrences = System.Text.RegularExpressions.Regex.Escape(oldText);
            var matches = System.Text.RegularExpressions.Regex.Matches(content, occurrences);
            var originalCount = matches.Count;

            if (originalCount == 0)
            {
                return new ReplaceResult 
                { 
                    IsSuccess = true, 
                    Replacements = 0,
                    Message = $"No occurrences of '{oldText}' found" 
                };
            }

            // Create backup
            if (createBackup)
            {
                var backupPath = filePath + ".bak";
                await File.WriteAllTextAsync(backupPath, content, Encoding.UTF8);
            }

            // Replace
            var newContent = content.Replace(oldText, newText);
            await File.WriteAllTextAsync(filePath, newContent, Encoding.UTF8);
            
            _logger.LogInformation("Replaced {Count} occurrences in {File}", originalCount, filePath);
            return new ReplaceResult
            {
                IsSuccess = true,
                Replacements = originalCount,
                Message = $"Replaced {originalCount} occurrence(s)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replace text in file: {File}", filePath);
            return new ReplaceResult { IsSuccess = false, Error = $"Failed: {ex.Message}" };
        }
    }

    /// <summary>
    /// Get file information
    /// </summary>
    [McpServerTool(Name = "get_file_info", Title = "Get detailed information about a file (size, creation date, attributes, etc.)")]
    public async Task<FileInfoResult?> GetFileInfo(string filePath, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var info = new FileInfo(filePath);
            return new FileInfoResult
            {
                FileName = info.Name,
                FullPath = info.FullName,
                Size = info.Length,
                SizeFormatted = FormatBytes(info.Length),
                CreatedAt = info.CreationTimeUtc,
                ModifiedAt = info.LastWriteTimeUtc,
                Extension = info.Extension,
                IsReadOnly = info.IsReadOnly,
                Attributes = info.Attributes.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file info: {File}", filePath);
            return null;
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

#region Data Models

public class FileReadResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int ReadLines { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}

public class ReplaceResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public int Replacements { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FileInfoResult
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Extension { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
    public string Attributes { get; set; } = string.Empty;
}

#endregion
