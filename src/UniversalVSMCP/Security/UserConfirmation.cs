using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// User Confirmation Manager - implements user elicitation for sensitive operations
/// Ensures users explicitly approve dangerous actions
/// </summary>
public class UserConfirmationManager
{
    private readonly ILogger<UserConfirmationManager> _logger;
    private readonly ConcurrentDictionary<string, PendingConfirmation> _pendingConfirmations;
    private readonly TimeSpan _confirmationTimeout;
    private readonly IConfirmationProvider? _confirmationProvider;

    // Operations requiring user confirmation
    private static readonly HashSet<string> SensitiveOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file",
        "delete_file",
        "replace_in_file",
        "build_solution",
        "rebuild_solution",
        "clean_solution",
        "start_debugging",
        "set_breakpoint",
        "open_solution",
        "create_solution",
        "add_file_to_project",
        "set_startup_project"
    };

    // High-risk operations requiring stronger confirmation
    private static readonly HashSet<string> HighRiskOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete_file",
        "clean_solution",
        "create_solution"
    };

    public UserConfirmationManager(
        ILogger<UserConfirmationManager> logger,
        TimeSpan? confirmationTimeout = null,
        IConfirmationProvider? confirmationProvider = null)
    {
        _logger = logger;
        _pendingConfirmations = new ConcurrentDictionary<string, PendingConfirmation>();
        _confirmationTimeout = confirmationTimeout ?? TimeSpan.FromMinutes(5);
        _confirmationProvider = confirmationProvider;
    }

    /// <summary>
    /// Check if an operation requires user confirmation
    /// </summary>
    public bool RequiresConfirmation(string operationName, string targetPath, out ConfirmationLevel level)
    {
        level = ConfirmationLevel.None;

        if (HighRiskOperations.Contains(operationName))
        {
            level = ConfirmationLevel.Strong;
            _logger.LogWarning("High-risk operation requires strong confirmation: {Operation} on {Path}",
                operationName, targetPath);
            return true;
        }

        if (SensitiveOperations.Contains(operationName))
        {
            level = ConfirmationLevel.Standard;
            _logger.LogInformation("Sensitive operation requires confirmation: {Operation} on {Path}",
                operationName, targetPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Request user confirmation for an operation
    /// </summary>
    public async Task<ConfirmationResult> RequestConfirmationAsync(
        string operationId,
        string operationName,
        string description,
        string targetPath,
        ConfirmationLevel level,
        CancellationToken ct = default)
    {
        var confirmationId = Guid.NewGuid().ToString("N")[..8];
        
        var pending = new PendingConfirmation
        {
            Id = confirmationId,
            OperationId = operationId,
            OperationName = operationName,
            Description = description,
            TargetPath = targetPath,
            Level = level,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_confirmationTimeout),
            CompletionSource = new TaskCompletionSource<ConfirmationResult>()
        };

        _pendingConfirmations[confirmationId] = pending;

        _logger.LogWarning("Confirmation required [{Level}]: {Operation} - {Description}\n" +
            "Confirmation ID: {ConfirmationId}", level, operationName, description, confirmationId);

        // Display confirmation prompt
        DisplayConfirmationPrompt(pending);

        // Use custom provider if available (e.g., VS Code extension UI)
        if (_confirmationProvider != null)
        {
            return await _confirmationProvider.RequestConfirmationAsync(pending, ct);
        }

        // Default: console-based confirmation (for stdio mode)
        // Note: In real implementation, this should be async with proper UI
        return await WaitForConsoleConfirmationAsync(pending, ct);
    }

    /// <summary>
    /// Complete a pending confirmation
    /// </summary>
    public bool CompleteConfirmation(string confirmationId, bool approved, string? reason = null)
    {
        if (!_pendingConfirmations.TryRemove(confirmationId, out var pending))
        {
            _logger.LogWarning("Confirmation {ConfirmationId} not found or already processed", confirmationId);
            return false;
        }

        var result = new ConfirmationResult
        {
            Approved = approved,
            ConfirmationId = confirmationId,
            Reason = reason,
            CompletedAt = DateTime.UtcNow,
            OperationName = pending.OperationName
        };

        pending.CompletionSource.SetResult(result);

        if (approved)
        {
            _logger.LogInformation("Confirmation {ConfirmationId} APPROVED: {Operation}",
                confirmationId, pending.OperationName);
        }
        else
        {
            _logger.LogWarning("Confirmation {ConfirmationId} DENIED: {Operation} - Reason: {Reason}",
                confirmationId, pending.OperationName, reason ?? "User declined");
        }

        return true;
    }

    /// <summary>
    /// Cancel a pending confirmation
    /// </summary>
    public bool CancelConfirmation(string confirmationId, string reason)
    {
        if (!_pendingConfirmations.TryRemove(confirmationId, out var pending))
        {
            return false;
        }

        pending.CompletionSource.SetResult(new ConfirmationResult
        {
            Approved = false,
            ConfirmationId = confirmationId,
            Reason = $"Cancelled: {reason}",
            CompletedAt = DateTime.UtcNow,
            OperationName = pending.OperationName
        });

        return true;
    }

    /// <summary>
    /// Clean up expired confirmations
    /// </summary>
    public void CleanupExpiredConfirmations()
    {
        var now = DateTime.UtcNow;
        var expired = _pendingConfirmations
            .Where(x => x.Value.ExpiresAt < now)
            .Select(x => x.Key)
            .ToList();

        foreach (var id in expired)
        {
            if (_pendingConfirmations.TryRemove(id, out var pending))
            {
                pending.CompletionSource.SetResult(new ConfirmationResult
                {
                    Approved = false,
                    ConfirmationId = id,
                    Reason = "Confirmation expired",
                    CompletedAt = now,
                    OperationName = pending.OperationName
                });

                _logger.LogWarning("Confirmation {ConfirmationId} EXPIRED: {Operation}",
                    id, pending.OperationName);
            }
        }
    }

    /// <summary>
    /// Get all pending confirmations
    /// </summary>
    public IReadOnlyCollection<PendingConfirmationInfo> GetPendingConfirmations()
    {
        return _pendingConfirmations.Values
            .Select(p => new PendingConfirmationInfo
            {
                Id = p.Id,
                OperationName = p.OperationName,
                Description = p.Description,
                TargetPath = p.TargetPath,
                Level = p.Level,
                RequestedAt = p.RequestedAt,
                ExpiresAt = p.ExpiresAt
            })
            .ToList();
    }

    private void DisplayConfirmationPrompt(PendingConfirmation pending)
    {
        var riskColor = pending.Level switch
        {
            ConfirmationLevel.Strong => "🔴",
            ConfirmationLevel.Standard => "🟡",
            _ => "🟢"
        };

        Console.WriteLine($"\n{riskColor} CONFIRMATION REQUIRED [{pending.Level}]\n");
        Console.WriteLine($"Operation: {pending.OperationName}");
        Console.WriteLine($"Target:    {pending.TargetPath}");
        Console.WriteLine($"Details:   {pending.Description}");
        Console.WriteLine($"Expires:   {pending.ExpiresAt:HH:mm:ss} UTC");
        Console.WriteLine($"\nConfirmation ID: {pending.Id}");
        Console.WriteLine($"\nTo approve:  CONFIRM {pending.Id}");
        Console.WriteLine($"To deny:     DENY {pending.Id} [reason]");
        Console.WriteLine();
    }

    private async Task<ConfirmationResult> WaitForConsoleConfirmationAsync(
        PendingConfirmation pending, CancellationToken ct)
    {
        // In stdio mode, the client (Claude/Cursor) should handle the confirmation
        // This is a placeholder that waits for timeout or external completion
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_confirmationTimeout);
            
            return await pending.CompletionSource.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingConfirmations.TryRemove(pending.Id, out _);
            
            return new ConfirmationResult
            {
                Approved = false,
                ConfirmationId = pending.Id,
                Reason = "Confirmation timed out",
                CompletedAt = DateTime.UtcNow,
                OperationName = pending.OperationName
            };
        }
    }
}

/// <summary>
/// Interface for confirmation providers (e.g., VS Code extension UI)
/// </summary>
public interface IConfirmationProvider
{
    Task<ConfirmationResult> RequestConfirmationAsync(PendingConfirmation confirmation, CancellationToken ct);
}

/// <summary>
/// Pending confirmation details
/// </summary>
public class PendingConfirmation
{
    public string Id { get; set; } = "";
    public string OperationId { get; set; } = "";
    public string OperationName { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public ConfirmationLevel Level { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public TaskCompletionSource<ConfirmationResult> CompletionSource { get; set; } = new();
}

/// <summary>
/// Public info about pending confirmation
/// </summary>
public class PendingConfirmationInfo
{
    public string Id { get; set; } = "";
    public string OperationName { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public ConfirmationLevel Level { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Confirmation result
/// </summary>
public class ConfirmationResult
{
    public bool Approved { get; set; }
    public string ConfirmationId { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime CompletedAt { get; set; }
    public string OperationName { get; set; } = "";
}

/// <summary>
/// Confirmation levels
/// </summary>
public enum ConfirmationLevel
{
    None,       // No confirmation needed
    Standard,   // Standard confirmation (e.g., write file)
    Strong      // Strong confirmation (e.g., delete file, clean solution)
}
