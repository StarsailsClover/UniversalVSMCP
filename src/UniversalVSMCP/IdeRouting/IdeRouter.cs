using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UniversalVSMCP.IdeAbstraction;

namespace UniversalVSMCP.IdeRouting;

/// <summary>
/// IDE Router - manages multiple IDE connections and routes operations
/// </summary>
public class IdeRouter
{
    private readonly ILogger<IdeRouter> _logger;
    private readonly Dictionary<string, IIdeAdapter> _adapters;
    private readonly Dictionary<string, IIdeAdapter> _activeConnections;
    private IIdeAdapter? _defaultAdapter;
    private RoutingStrategy _strategy;

    public IdeRouter(ILogger<IdeRouter> logger, RoutingStrategy strategy = RoutingStrategy.Auto)
    {
        _logger = logger;
        _adapters = new Dictionary<string, IIdeAdapter>(StringComparer.OrdinalIgnoreCase);
        _activeConnections = new Dictionary<string, IIdeAdapter>(StringComparer.OrdinalIgnoreCase);
        _strategy = strategy;
    }

    /// <summary>
    /// Register an IDE adapter
    /// </summary>
    public void RegisterAdapter(string name, IIdeAdapter adapter)
    {
        _adapters[name] = adapter;
        _logger.LogInformation("Registered IDE adapter: {Name} ({IdeName} {IdeVersion})",
            name, adapter.IdeName, adapter.IdeVersion);
    }

    /// <summary>
    /// Set the default adapter
    /// </summary>
    public void SetDefaultAdapter(string name)
    {
        if (_adapters.TryGetValue(name, out var adapter))
        {
            _defaultAdapter = adapter;
            _logger.LogInformation("Set default IDE adapter: {Name}", name);
        }
        else
        {
            _logger.LogWarning("Cannot set default adapter: {Name} not found", name);
        }
    }

    /// <summary>
    /// Connect to an IDE based on routing criteria
    /// </summary>
    public async Task<ConnectionResult> ConnectAsync(RoutingCriteria criteria)
    {
        var adapter = await SelectAdapterAsync(criteria);
        if (adapter == null)
        {
            return new ConnectionResult
            {
                Success = false,
                ErrorMessage = "No suitable IDE adapter found"
            };
        }

        try
        {
            var options = new ConnectionOptions
            {
                SolutionPath = criteria.SolutionPath,
                AutoDiscover = criteria.AutoDiscover,
                Timeout = criteria.Timeout
            };

            var connected = await adapter.ConnectAsync(options);
            if (connected)
            {
                _activeConnections[adapter.InstanceId] = adapter;
                _logger.LogInformation("Connected to {IdeName} {IdeVersion} (Instance: {InstanceId})",
                    adapter.IdeName, adapter.IdeVersion, adapter.InstanceId);

                return new ConnectionResult
                {
                    Success = true,
                    Adapter = adapter,
                    InstanceId = adapter.InstanceId
                };
            }
            else
            {
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to connect to {adapter.IdeName}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to IDE");
            return new ConnectionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Disconnect from all IDEs
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        foreach (var (instanceId, adapter) in _activeConnections.ToList())
        {
            try
            {
                await adapter.DisconnectAsync();
                _logger.LogInformation("Disconnected from {IdeName} (Instance: {InstanceId})",
                    adapter.IdeName, instanceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from IDE");
            }
        }
        _activeConnections.Clear();
    }

    /// <summary>
    /// Get the current solution information from the best available IDE
    /// </summary>
    public async Task<SolutionInfo?> GetSolutionAsync(RoutingCriteria? criteria = null)
    {
        var adapter = await SelectAdapterAsync(criteria ?? new RoutingCriteria());
        if (adapter == null)
        {
            _logger.LogWarning("No IDE adapter available for GetSolution");
            return null;
        }

        return await adapter.GetSolutionAsync();
    }

    /// <summary>
    /// Execute a build operation on the appropriate IDE
    /// </summary>
    public async Task<BuildResult> BuildSolutionAsync(RoutingCriteria? criteria = null, BuildConfiguration? config = null)
    {
        var adapter = await SelectAdapterAsync(criteria ?? new RoutingCriteria { PreferBuildSupport = true });
        if (adapter == null)
        {
            return new BuildResult
            {
                Success = false,
                Output = "No IDE adapter available for build"
            };
        }

        return await adapter.BuildSolutionAsync(config);
    }

    /// <summary>
    /// Start debugging on the appropriate IDE
    /// </summary>
    public async Task<bool> StartDebuggingAsync(RoutingCriteria? criteria = null, DebugTarget? target = null)
    {
        var adapter = await SelectAdapterAsync(criteria ?? new RoutingCriteria { PreferDebugSupport = true });
        if (adapter == null)
        {
            _logger.LogWarning("No IDE adapter available for debugging");
            return false;
        }

        return await adapter.StartDebuggingAsync(target);
    }

    /// <summary>
    /// Open a file in the appropriate IDE
    /// </summary>
    public async Task<bool> OpenFileAsync(string filePath, RoutingCriteria? criteria = null)
    {
        var adapter = await SelectAdapterAsync(criteria ?? new RoutingCriteria { FilePath = filePath });
        if (adapter == null)
        {
            _logger.LogWarning("No IDE adapter available for opening file");
            return false;
        }

        return await adapter.OpenFileAsync(filePath);
    }

    /// <summary>
    /// Get all connected IDE information
    /// </summary>
    public IReadOnlyList<IdeConnectionInfo> GetConnectedIdes()
    {
        return _activeConnections.Values.Select(adapter => new IdeConnectionInfo
        {
            InstanceId = adapter.InstanceId,
            Name = adapter.IdeName,
            Version = adapter.IdeVersion,
            IsConnected = adapter.IsConnected,
            Capabilities = adapter.Capabilities
        }).ToList();
    }

    /// <summary>
    /// Select the best adapter based on routing criteria
    /// </summary>
    private async Task<IIdeAdapter?> SelectAdapterAsync(RoutingCriteria criteria)
    {
        // 1. If specific instance requested, use it
        if (!string.IsNullOrEmpty(criteria.InstanceId))
        {
            if (_activeConnections.TryGetValue(criteria.InstanceId, out var instance))
            {
                return instance;
            }
            _logger.LogWarning("Requested IDE instance {InstanceId} not found", criteria.InstanceId);
        }

        // 2. If specific IDE type requested
        if (!string.IsNullOrEmpty(criteria.IdeType))
        {
            var typeAdapters = _activeConnections.Values
                .Where(a => a.IdeName.Equals(criteria.IdeType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (typeAdapters.Any())
            {
                return SelectBestFromList(typeAdapters, criteria);
            }

            // Try to find adapter by name in registered adapters
            var adapterName = criteria.IdeType.ToLower() switch
            {
                "vs" or "visualstudio" => "vs2022",
                "vscode" or "vs code" => "vscode",
                _ => criteria.IdeType
            };

            if (_adapters.TryGetValue(adapterName, out var namedAdapter))
            {
                return namedAdapter;
            }
        }

        // 3. If file path provided, route based on file type
        if (!string.IsNullOrEmpty(criteria.FilePath))
        {
            var extension = Path.GetExtension(criteria.FilePath).ToLower();
            var fileAdapters = _activeConnections.Values
                .Where(a => SupportsFileType(a, extension))
                .ToList();

            if (fileAdapters.Any())
            {
                return SelectBestFromList(fileAdapters, criteria);
            }
        }

        // 4. If solution path provided
        if (!string.IsNullOrEmpty(criteria.SolutionPath))
        {
            var extension = Path.GetExtension(criteria.SolutionPath).ToLower();
            
            // .sln files prefer VS
            if (extension == ".sln")
            {
                var vsAdapter = _activeConnections.Values
                    .FirstOrDefault(a => a.IdeName.Contains("Visual Studio"));
                if (vsAdapter != null)
                {
                    return vsAdapter;
                }
            }

            // Workspace folders prefer VS Code
            if (Directory.Exists(criteria.SolutionPath))
            {
                var vscodeAdapter = _activeConnections.Values
                    .FirstOrDefault(a => a.IdeName.Contains("VS Code"));
                if (vscodeAdapter != null)
                {
                    return vscodeAdapter;
                }
            }
        }

        // 5. Based on capability requirements
        if (criteria.PreferBuildSupport)
        {
            var buildAdapter = _activeConnections.Values
                .FirstOrDefault(a => a.Capabilities.SupportsBuild);
            if (buildAdapter != null)
            {
                return buildAdapter;
            }
        }

        if (criteria.PreferDebugSupport)
        {
            var debugAdapter = _activeConnections.Values
                .FirstOrDefault(a => a.Capabilities.SupportsDebug);
            if (debugAdapter != null)
            {
                return debugAdapter;
            }
        }

        // 6. Use default or first active connection
        if (_defaultAdapter != null && _activeConnections.ContainsValue(_defaultAdapter))
        {
            return _defaultAdapter;
        }

        // 7. Return first active connection
        if (_activeConnections.Any())
        {
            return _activeConnections.Values.First();
        }

        // 8. No active connections - return default adapter for potential connection
        return _defaultAdapter ?? _adapters.Values.FirstOrDefault();
    }

    /// <summary>
    /// Select the best adapter from a list based on criteria
    /// </summary>
    private IIdeAdapter SelectBestFromList(List<IIdeAdapter> adapters, RoutingCriteria criteria)
    {
        // Score each adapter
        var scoredAdapters = adapters.Select(adapter => new
        {
            Adapter = adapter,
            Score = ScoreAdapter(adapter, criteria)
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        return scoredAdapters.First().Adapter;
    }

    /// <summary>
    /// Score an adapter based on how well it matches criteria
    /// </summary>
    private int ScoreAdapter(IIdeAdapter adapter, RoutingCriteria criteria)
    {
        int score = 0;

        // Prefer connected adapters
        if (adapter.IsConnected)
        {
            score += 10;
        }

        // Match capabilities
        if (criteria.PreferBuildSupport && adapter.Capabilities.SupportsBuild)
        {
            score += 5;
        }

        if (criteria.PreferDebugSupport && adapter.Capabilities.SupportsDebug)
        {
            score += 5;
        }

        // Prefer newer versions
        if (Version.TryParse(adapter.IdeVersion, out var version))
        {
            score += version.Major;
        }

        // Health check bonus
        if (adapter.IsConnected)
        {
            // Would be async, so we skip for now
            // score += await adapter.HealthCheckAsync() ? 3 : 0;
        }

        return score;
    }

    /// <summary>
    /// Check if adapter supports a file type
    /// </summary>
    private bool SupportsFileType(IIdeAdapter adapter, string extension)
    {
        // Common mappings
        var vsExtensions = new[] { ".cs", ".vb", ".cpp", ".h", ".vcxproj", ".sln" };
        var vscodeExtensions = new[] { ".js", ".ts", ".json", ".md", ".py", ".html", ".css" };

        if (adapter.IdeName.Contains("Visual Studio"))
        {
            return vsExtensions.Contains(extension);
        }

        if (adapter.IdeName.Contains("VS Code"))
        {
            return true; // VS Code supports almost everything
        }

        return true; // Default to yes
    }

    /// <summary>
    /// Auto-discover and register IDE adapters
    /// </summary>
    public async Task AutoDiscoverAdaptersAsync()
    {
        _logger.LogInformation("Auto-discovering IDE adapters...");

        // This would scan for running VS and VS Code instances
        // For now, we rely on explicit registration

        await Task.CompletedTask;
    }
}

/// <summary>
/// Routing criteria for IDE selection
/// </summary>
public class RoutingCriteria
{
    /// <summary>
    /// Specific IDE instance ID
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// IDE type preference ("vs", "vscode", etc.)
    /// </summary>
    public string? IdeType { get; set; }

    /// <summary>
    /// File to be operated on
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Solution/project path
    /// </summary>
    public string? SolutionPath { get; set; }

    /// <summary>
    /// Prefer IDE with build support
    /// </summary>
    public bool PreferBuildSupport { get; set; }

    /// <summary>
    /// Prefer IDE with debug support
    /// </summary>
    public bool PreferDebugSupport { get; set; }

    /// <summary>
    /// Auto-discover running IDE instances
    /// </summary>
    public bool AutoDiscover { get; set; } = true;

    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Routing strategies
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// Automatically select best IDE
    /// </summary>
    Auto,

    /// <summary>
    /// Prefer Visual Studio
    /// </summary>
    PreferVs,

    /// <summary>
    /// Prefer VS Code
    /// </summary>
    PreferVsCode,

    /// <summary>
    /// Round-robin between available IDEs
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Load balance between IDEs
    /// </summary>
    LoadBalance
}

/// <summary>
/// Connection result
/// </summary>
public class ConnectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IIdeAdapter? Adapter { get; set; }
    public string? InstanceId { get; set; }
}

/// <summary>
/// IDE connection information
/// </summary>
public class IdeConnectionInfo
{
    public string InstanceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsConnected { get; set; }
    public IdeCapabilities Capabilities { get; set; } = new();
}
