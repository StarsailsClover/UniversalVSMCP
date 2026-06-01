using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP;

/// <summary>
/// Configuration Manager - loads and manages server configuration
/// </summary>
public class ConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private ServerConfiguration _config;
    private readonly string _configPath;
    private readonly Timer? _reloadTimer;

    public ConfigurationManager(ILogger<ConfigurationManager> logger, string? configPath = null)
    {
        _logger = logger;
        _configPath = configPath ?? GetDefaultConfigPath();
        _config = new ServerConfiguration();
        
        LoadConfiguration();
        
        // Setup auto-reload every 30 seconds
        _reloadTimer = new Timer(_ => ReloadConfiguration(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Get current configuration
    /// </summary>
    public ServerConfiguration Config => _config;

    /// <summary>
    /// Get configuration value
    /// </summary>
    public T Get<T>(string key, T defaultValue)
    {
        try
        {
            var property = typeof(ServerConfiguration).GetProperty(key);
            if (property != null)
            {
                var value = property.GetValue(_config);
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get config value for {Key}", key);
        }
        
        return defaultValue;
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Configuration file not found at {Path}, creating default", _configPath);
                CreateDefaultConfiguration();
                return;
            }

            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<ServerConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config != null)
            {
                _config = config;
                _logger.LogInformation("Configuration loaded from {Path}", _configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", _configPath);
        }
    }

    /// <summary>
    /// Reload configuration from file
    /// </summary>
    public void ReloadConfiguration()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var lastWrite = File.GetLastWriteTime(_configPath);
                if (lastWrite > _config.LastLoaded)
                {
                    LoadConfiguration();
                    _config.LastLoaded = DateTime.UtcNow;
                    _logger.LogInformation("Configuration reloaded from {Path}", _configPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
        }
    }

    /// <summary>
    /// Save configuration to file
    /// </summary>
    public void SaveConfiguration()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(_configPath, json);
            _config.LastLoaded = DateTime.UtcNow;
            _logger.LogInformation("Configuration saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configPath);
        }
    }

    /// <summary>
    /// Create default configuration file
    /// </summary>
    private void CreateDefaultConfiguration()
    {
        _config = new ServerConfiguration
        {
            Server = new ServerSettings
            {
                Name = "Universal VS MCP",
                Version = "26.0.3",
                LogLevel = "Info",
                LogFile = null
            },
            Transport = new TransportSettings
            {
                Mode = "stdio",
                HttpPort = 5000,
                EnableCors = true
            },
            Security = new SecuritySettings
            {
                RequireAuthentication = false,
                EnableWorkspaceTrust = true,
                EnableUserConfirmation = true,
                EnableAuditLogging = true
            },
            IdeAdapters = new IdeAdapterSettings
            {
                DefaultAdapter = "auto", // auto, vs2022, vscode
                AutoDiscover = true,
                ConnectionTimeoutSeconds = 30
            },
            Routing = new RoutingSettings
            {
                Strategy = "auto", // auto, prefer-vs, prefer-vscode, round-robin
                EnableLoadBalancing = false,
                HealthCheckIntervalSeconds = 60
            }
        };

        SaveConfiguration();
    }

    /// <summary>
    /// Get default configuration path
    /// </summary>
    private static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "UniversalVSMCP", "config.json");
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _reloadTimer?.Dispose();
    }
}

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfiguration
{
    public ServerSettings Server { get; set; } = new();
    public TransportSettings Transport { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public IdeAdapterSettings IdeAdapters { get; set; } = new();
    public RoutingSettings Routing { get; set; } = new();
    public DateTime LastLoaded { get; set; } = DateTime.MinValue;
}

/// <summary>
/// Server settings
/// </summary>
public class ServerSettings
{
    public string Name { get; set; } = "Universal VS MCP";
    public string Version { get; set; } = "26.0.3";
    public string LogLevel { get; set; } = "Info";
    public string? LogFile { get; set; }
}

/// <summary>
/// Transport settings
/// </summary>
public class TransportSettings
{
    public string Mode { get; set; } = "stdio"; // stdio, http, hybrid
    public int HttpPort { get; set; } = 5000;
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Security settings
/// </summary>
public class SecuritySettings
{
    public bool RequireAuthentication { get; set; } = false;
    public string[] AllowedApiKeys { get; set; } = Array.Empty<string>();
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
    public bool EnableWorkspaceTrust { get; set; } = true;
    public bool EnableUserConfirmation { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public string AuditLogPath { get; set; } = "";
    public int MaxFailedAttempts { get; set; } = 5;
}

/// <summary>
/// IDE adapter settings
/// </summary>
public class IdeAdapterSettings
{
    public string DefaultAdapter { get; set; } = "auto"; // auto, vs2022, vscode
    public bool AutoDiscover { get; set; } = true;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public string VsPath { get; set; } = "";
    public string VsCodePath { get; set; } = "";
}

/// <summary>
/// Routing settings
/// </summary>
public class RoutingSettings
{
    public string Strategy { get; set; } = "auto"; // auto, prefer-vs, prefer-vscode, round-robin
    public bool EnableLoadBalancing { get; set; } = false;
    public int HealthCheckIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// Configuration example
/// </summary>
public static class ConfigurationExample
{
    public const string Example = @"
{
  ""server"": {
    ""name"": ""Universal VS MCP"",
    ""version"": ""26.0.3"",
    ""logLevel"": ""Info"",
    ""logFile"": ""C:\\Logs\\uvm.log""
  },
  ""transport"": {
    ""mode"": ""hybrid"",
    ""httpPort"": 5000,
    ""enableCors"": true
  },
  ""security"": {
    ""requireAuthentication"": false,
    ""enableWorkspaceTrust"": true,
    ""enableUserConfirmation"": true,
    ""enableAuditLogging"": true,
    ""auditLogPath"": ""C:\\Logs\\audit.log""
  },
  ""ideAdapters"": {
    ""defaultAdapter"": ""auto"",
    ""autoDiscover"": true,
    ""connectionTimeoutSeconds"": 30
  },
  ""routing"": {
    ""strategy"": ""auto"",
    ""enableLoadBalancing"": false,
    ""healthCheckIntervalSeconds"": 60
  }
}
";
}
