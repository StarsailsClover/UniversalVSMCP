using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// Tool Permission Manager - implements least privilege principle for MCP tools
/// </summary>
public class ToolPermissionManager
{
    private readonly ILogger<ToolPermissionManager> _logger;
    private readonly UserConfirmationManager _confirmationManager;
    private readonly OperationAuditor _auditor;
    private readonly Dictionary<string, ToolPermission> _toolPermissions;
    private readonly Dictionary<string, ClientPermissionProfile> _clientProfiles;

    public ToolPermissionManager(
        ILogger<ToolPermissionManager> logger,
        UserConfirmationManager confirmationManager,
        OperationAuditor auditor)
    {
        _logger = logger;
        _confirmationManager = confirmationManager;
        _auditor = auditor;
        
        _toolPermissions = InitializeDefaultPermissions();
        _clientProfiles = new Dictionary<string, ClientPermissionProfile>();
    }

    /// <summary>
    /// Check if a tool can be executed with the given context
    /// </summary>
    public async Task<PermissionResult> CheckPermissionAsync(
        string toolName,
        string clientId,
        string? targetPath = null,
        Dictionary<string, object>? arguments = null)
    {
        // Get tool permission definition
        if (!_toolPermissions.TryGetValue(toolName, out var permission))
        {
            _logger.LogWarning("Tool {Tool} not found in permission registry", toolName);
            return PermissionResult.Denied($"Tool '{toolName}' is not registered");
        }

        // Get client profile
        var profile = GetClientProfile(clientId);

        // Check if client has permission for this tool category
        if (!profile.AllowedCategories.Contains(permission.Category))
        {
            _logger.LogWarning("Client {Client} denied access to category {Category}", 
                clientId, permission.Category);
            _auditor.LogSecurityEvent(
                SecurityEventType.UnauthorizedAccess,
                $"Client attempted to use unauthorized tool category: {permission.Category}",
                targetPath ?? "N/A",
                blocked: true);
            return PermissionResult.Denied($"Category '{permission.Category}' not allowed for this client");
        }

        // Check operation mode
        if (profile.OperationMode == OperationMode.ReadOnly && permission.RiskLevel != RiskLevel.Low)
        {
            _logger.LogWarning("Client {Client} in read-only mode attempted {Tool}", clientId, toolName);
            return PermissionResult.Denied("Client is in read-only mode");
        }

        // Check if user confirmation is required
        if (permission.RequiresConfirmation || profile.RequireConfirmationForAll)
        {
            var description = BuildOperationDescription(toolName, arguments);
            var level = permission.RiskLevel switch
            {
                RiskLevel.Critical => ConfirmationLevel.Strong,
                RiskLevel.High => ConfirmationLevel.Strong,
                RiskLevel.Medium => ConfirmationLevel.Standard,
                _ => ConfirmationLevel.Standard
            };

            var confirmation = await _confirmationManager.RequestConfirmationAsync(
                Guid.NewGuid().ToString(),
                toolName,
                description,
                targetPath ?? "N/A",
                level);

            if (!confirmation.Approved)
            {
                _auditor.LogSecurityEvent(
                    SecurityEventType.UserConfirmationDenied,
                    $"User denied {toolName} operation",
                    targetPath ?? "N/A",
                    blocked: true);
                return PermissionResult.Denied($"Operation denied by user: {confirmation.Reason}");
            }
        }

        // Log permission grant
        _auditor.LogSecurityEvent(
            SecurityEventType.None,
            $"Permission granted for {toolName}",
            targetPath ?? "N/A",
            blocked: false);

        return PermissionResult.Granted(permission);
    }

    /// <summary>
    /// Register a client with specific permission profile
    /// </summary>
    public void RegisterClient(string clientId, ClientPermissionProfile profile)
    {
        _clientProfiles[clientId] = profile;
        _logger.LogInformation("Registered client {Client} with profile {Mode}", 
            clientId, profile.OperationMode);
    }

    /// <summary>
    /// Get or create default client profile
    /// </summary>
    private ClientPermissionProfile GetClientProfile(string clientId)
    {
        if (_clientProfiles.TryGetValue(clientId, out var profile))
        {
            return profile;
        }

        // Create default profile
        return new ClientPermissionProfile
        {
            ClientId = clientId,
            OperationMode = OperationMode.Standard,
            AllowedCategories = Enum.GetValues<ToolCategory>().ToList(),
            RequireConfirmationForAll = false
        };
    }

    /// <summary>
    /// Initialize default tool permissions
    /// </summary>
    private Dictionary<string, ToolPermission> InitializeDefaultPermissions()
    {
        return new Dictionary<string, ToolPermission>(StringComparer.OrdinalIgnoreCase)
        {
            // Solution tools - Medium risk
            ["get_solution_projects"] = new ToolPermission 
            { 
                Name = "get_solution_projects", 
                Category = ToolCategory.Solution,
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false
            },
            ["open_solution"] = new ToolPermission 
            { 
                Name = "open_solution", 
                Category = ToolCategory.Solution,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = true
            },
            ["close_solution"] = new ToolPermission 
            { 
                Name = "close_solution", 
                Category = ToolCategory.Solution,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = true
            },
            ["create_solution"] = new ToolPermission 
            { 
                Name = "create_solution", 
                Category = ToolCategory.Solution,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },

            // Project tools - Medium risk
            ["get_project_files"] = new ToolPermission 
            { 
                Name = "get_project_files", 
                Category = ToolCategory.Project,
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false
            },
            ["add_file_to_project"] = new ToolPermission 
            { 
                Name = "add_file_to_project", 
                Category = ToolCategory.Project,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = true
            },
            ["set_startup_project"] = new ToolPermission 
            { 
                Name = "set_startup_project", 
                Category = ToolCategory.Project,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = true
            },

            // File tools - High risk for write
            ["read_file"] = new ToolPermission 
            { 
                Name = "read_file", 
                Category = ToolCategory.File,
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false
            },
            ["write_file"] = new ToolPermission 
            { 
                Name = "write_file", 
                Category = ToolCategory.File,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },
            ["replace_in_file"] = new ToolPermission 
            { 
                Name = "replace_in_file", 
                Category = ToolCategory.File,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },
            ["delete_file"] = new ToolPermission 
            { 
                Name = "delete_file", 
                Category = ToolCategory.File,
                RiskLevel = RiskLevel.Critical,
                RequiresConfirmation = true
            },

            // Build tools - High risk
            ["build_solution"] = new ToolPermission 
            { 
                Name = "build_solution", 
                Category = ToolCategory.Build,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = false
            },
            ["rebuild_solution"] = new ToolPermission 
            { 
                Name = "rebuild_solution", 
                Category = ToolCategory.Build,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },
            ["clean_solution"] = new ToolPermission 
            { 
                Name = "clean_solution", 
                Category = ToolCategory.Build,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },

            // Debug tools - High risk
            ["start_debugging"] = new ToolPermission 
            { 
                Name = "start_debugging", 
                Category = ToolCategory.Debug,
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true
            },
            ["set_breakpoint"] = new ToolPermission 
            { 
                Name = "set_breakpoint", 
                Category = ToolCategory.Debug,
                RiskLevel = RiskLevel.Medium,
                RequiresConfirmation = false
            },

            // Diagnostic tools - Low risk
            ["health_check"] = new ToolPermission 
            { 
                Name = "health_check", 
                Category = ToolCategory.Diagnostic,
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false
            },
            ["get_server_info"] = new ToolPermission 
            { 
                Name = "get_server_info", 
                Category = ToolCategory.Diagnostic,
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false
            }
        };
    }

    private string BuildOperationDescription(string toolName, Dictionary<string, object>? arguments)
    {
        var args = arguments != null 
            ? JsonSerializer.Serialize(arguments, new JsonSerializerOptions { WriteIndented = false })
            : "no arguments";
        return $"{toolName} with {args}";
    }
}

/// <summary>
/// Tool permission definition
/// </summary>
public class ToolPermission
{
    public string Name { get; set; } = "";
    public ToolCategory Category { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresConfirmation { get; set; }
}

/// <summary>
/// Permission check result
/// </summary>
public class PermissionResult
{
    public bool IsGranted { get; set; }
    public string? DenyReason { get; set; }
    public ToolPermission? Permission { get; set; }

    public static PermissionResult Granted(ToolPermission permission) => new()
    {
        IsGranted = true,
        Permission = permission
    };

    public static PermissionResult Denied(string reason) => new()
    {
        IsGranted = false,
        DenyReason = reason
    };
}

/// <summary>
/// Client permission profile
/// </summary>
public class ClientPermissionProfile
{
    public string ClientId { get; set; } = "";
    public OperationMode OperationMode { get; set; } = OperationMode.Standard;
    public List<ToolCategory> AllowedCategories { get; set; } = new();
    public bool RequireConfirmationForAll { get; set; }
}

/// <summary>
/// Tool categories
/// </summary>
public enum ToolCategory
{
    Solution,
    Project,
    File,
    Build,
    Debug,
    Diagnostic
}

/// <summary>
/// Risk levels
/// </summary>
public enum RiskLevel
{
    Low,      // Read operations
    Medium,   // Non-destructive modifications
    High,     // Destructive operations
    Critical  // Irreversible operations
}

/// <summary>
/// Operation modes
/// </summary>
public enum OperationMode
{
    ReadOnly,   // Only read operations
    Standard,   // Normal operations with confirmation
    Elevated    // All operations allowed
}
