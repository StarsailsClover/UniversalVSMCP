using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// Workspace Trust Manager - implements Microsoft Workspace Trust best practices
/// Ensures operations only occur in trusted directories
/// </summary>
public class WorkspaceTrustManager
{
    private readonly ILogger<WorkspaceTrustManager> _logger;
    private readonly string _trustConfigPath;
    private readonly HashSet<string> _trustedPaths;
    private readonly HashSet<string> _blockedPaths;
    
    // System directories that should never be accessed
    private static readonly string[] SystemPaths = new[]
    {
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\ProgramData",
        @"C:\Users\Default",
        @"C:\Recovery",
        Path.GetTempPath() // Temp directory
    };

    public WorkspaceTrustManager(ILogger<WorkspaceTrustManager> logger)
    {
        _logger = logger;
        _trustConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UniversalVSMCP",
            "trusted-workspaces.json"
        );
        _trustedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _blockedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        LoadTrustedWorkspaces();
    }

    /// <summary>
    /// Check if a path is within a trusted workspace
    /// </summary>
    public TrustLevel CheckTrustLevel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return TrustLevel.Untrusted;
        }

        var fullPath = Path.GetFullPath(path);

        // Check system paths (always blocked)
        foreach (var sysPath in SystemPaths)
        {
            if (fullPath.StartsWith(sysPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Access denied: System path {Path}", fullPath);
                return TrustLevel.Blocked;
            }
        }

        // Check blocked paths
        if (_blockedPaths.Any(bp => fullPath.StartsWith(bp, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Access denied: Blocked path {Path}", fullPath);
            return TrustLevel.Blocked;
        }

        // Check trusted paths
        foreach (var trustedPath in _trustedPaths)
        {
            if (fullPath.StartsWith(trustedPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Path {Path} is in trusted workspace {TrustedPath}", 
                    fullPath, trustedPath);
                return TrustLevel.Trusted;
            }
        }

        // Parent directory of executable
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(exeDir) && fullPath.StartsWith(exeDir, StringComparison.OrdinalIgnoreCase))
        {
            return TrustLevel.Trusted;
        }

        // User documents folder (restricted trust)
        var userDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (fullPath.StartsWith(userDocs, StringComparison.OrdinalIgnoreCase))
        {
            return TrustLevel.Restricted;
        }

        return TrustLevel.Untrusted;
    }

    /// <summary>
    /// Add a workspace to trusted list
    /// </summary>
    public void TrustWorkspace(string path)
    {
        var fullPath = Path.GetFullPath(path);
        
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace not found: {fullPath}");
        }

        _trustedPaths.Add(fullPath);
        SaveTrustedWorkspaces();
        
        _logger.LogInformation("Workspace added to trusted list: {Path}", fullPath);
    }

    /// <summary>
    /// Remove a workspace from trusted list
    /// </summary>
    public void UntrustWorkspace(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _trustedPaths.Remove(fullPath);
        SaveTrustedWorkspaces();
        
        _logger.LogInformation("Workspace removed from trusted list: {Path}", fullPath);
    }

    /// <summary>
    /// Block a specific path
    /// </summary>
    public void BlockPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _blockedPaths.Add(fullPath);
        SaveTrustedWorkspaces();
        
        _logger.LogWarning("Path blocked: {Path}", fullPath);
    }

    /// <summary>
    /// Verify file operation is allowed
    /// </summary>
    public bool VerifyFileOperation(string path, FileOperation operation)
    {
        var trustLevel = CheckTrustLevel(path);
        
        switch (trustLevel)
        {
            case TrustLevel.Blocked:
                _logger.LogError("File operation {Operation} blocked for {Path}: System/Blocked directory", 
                    operation, path);
                return false;
                
            case TrustLevel.Untrusted:
                _logger.LogWarning("File operation {Operation} in untrusted directory {Path}. " +
                    "User confirmation required.", operation, path);
                return false; // Requires explicit user confirmation
                
            case TrustLevel.Restricted:
                // Allow read operations, block write/delete
                if (operation == FileOperation.Read || operation == FileOperation.List)
                {
                    return true;
                }
                _logger.LogWarning("Write operation {Operation} in restricted directory {Path}. " +
                    "User confirmation required.", operation, path);
                return false;
                
            case TrustLevel.Trusted:
                return true;
                
            default:
                return false;
        }
    }

    /// <summary>
    /// Get all trusted workspaces
    /// </summary>
    public IReadOnlyCollection<string> GetTrustedWorkspaces() => _trustedPaths.ToList();

    private void LoadTrustedWorkspaces()
    {
        try
        {
            if (!File.Exists(_trustConfigPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_trustConfigPath)!);
                return;
            }

            var json = File.ReadAllText(_trustConfigPath);
            var config = JsonSerializer.Deserialize<TrustConfig>(json);
            
            if (config?.TrustedPaths != null)
            {
                foreach (var path in config.TrustedPaths)
                {
                    if (Directory.Exists(path))
                    {
                        _trustedPaths.Add(path);
                    }
                }
            }

            if (config?.BlockedPaths != null)
            {
                _blockedPaths.UnionWith(config.BlockedPaths);
            }

            _logger.LogInformation("Loaded {Count} trusted workspaces", _trustedPaths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load trusted workspaces");
        }
    }

    private void SaveTrustedWorkspaces()
    {
        try
        {
            var config = new TrustConfig
            {
                TrustedPaths = _trustedPaths.ToList(),
                BlockedPaths = _blockedPaths.ToList(),
                LastUpdated = DateTime.UtcNow
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_trustConfigPath)!);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_trustConfigPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save trusted workspaces");
        }
    }

    private class TrustConfig
    {
        public List<string> TrustedPaths { get; set; } = new();
        public List<string> BlockedPaths { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}

/// <summary>
/// Trust levels for workspace directories
/// </summary>
public enum TrustLevel
{
    /// <summary>
    /// Directory is explicitly blocked
    /// </summary>
    Blocked,
    
    /// <summary>
    /// Directory is not trusted, requires confirmation
    /// </summary>
    Untrusted,
    
    /// <summary>
    /// Directory is in user documents, read-only allowed
    /// </summary>
    Restricted,
    
    /// <summary>
    /// Directory is explicitly trusted
    /// </summary>
    Trusted
}

/// <summary>
/// File operation types
/// </summary>
public enum FileOperation
{
    Read,
    Write,
    Delete,
    List,
    Execute
}
