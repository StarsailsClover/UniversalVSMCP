using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// Operation Auditor - comprehensive audit logging for all sensitive operations
/// Provides tamper-evident logging for security compliance
/// </summary>
public class OperationAuditor
{
    private readonly ILogger<OperationAuditor> _logger;
    private readonly string _auditLogPath;
    private readonly ConcurrentQueue<AuditEntry> _memoryBuffer;
    private readonly int _maxMemoryEntries;
    private readonly Timer? _flushTimer;
    private readonly object _fileLock = new();

    public OperationAuditor(ILogger<OperationAuditor> logger, string? customLogPath = null)
    {
        _logger = logger;
        _auditLogPath = customLogPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UniversalVSMCP",
            "audit.log"
        );
        _memoryBuffer = new ConcurrentQueue<AuditEntry>();
        _maxMemoryEntries = 1000;

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_auditLogPath)!);

        // Start periodic flush timer (every 30 seconds)
        _flushTimer = new Timer(_ => FlushToDisk(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Start an operation audit
    /// </summary>
    public AuditEntry StartOperation(string operationId, string operationName, string targetPath, string description)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            OperationId = operationId,
            OperationName = operationName,
            TargetPath = targetPath,
            Description = description,
            StartTime = DateTime.UtcNow,
            Status = AuditStatus.Started,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessId = Environment.ProcessId
        };

        _memoryBuffer.Enqueue(entry);
        
        // Flush if buffer is getting full
        if (_memoryBuffer.Count >= _maxMemoryEntries)
        {
            FlushToDisk();
        }

        _logger.LogDebug("Audit started: {Operation} [{AuditId}]", operationName, entry.Id);
        
        return entry;
    }

    /// <summary>
    /// Log a tool invocation
    /// </summary>
    public void LogToolInvocation(string toolName, string arguments, bool success, string? result = null)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            OperationName = $"tool:{toolName}",
            Description = arguments,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            Status = success ? AuditStatus.Succeeded : AuditStatus.Failed,
            Result = result,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessId = Environment.ProcessId
        };

        _memoryBuffer.Enqueue(entry);
        
        _logger.LogInformation("Tool audit: {Tool} - {Status}", toolName, entry.Status);
    }

    /// <summary>
    /// Log a security event
    /// </summary>
    public void LogSecurityEvent(SecurityEventType eventType, string description, string? targetPath = null, bool blocked = false)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            OperationName = $"security:{eventType}",
            Description = description,
            TargetPath = targetPath ?? "N/A",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            Status = blocked ? AuditStatus.Blocked : AuditStatus.Warning,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessId = Environment.ProcessId
        };

        _memoryBuffer.Enqueue(entry);

        if (blocked)
        {
            _logger.LogWarning("Security event BLOCKED: {EventType} - {Description}", eventType, description);
        }
        else
        {
            _logger.LogInformation("Security event: {EventType} - {Description}", eventType, description);
        }
    }

    /// <summary>
    /// Get recent audit entries
    /// </summary>
    public IReadOnlyList<AuditEntry> GetRecentEntries(int count = 100)
    {
        return _memoryBuffer.TakeLast(count).ToList();
    }

    /// <summary>
    /// Get audit entries for a specific operation
    /// </summary>
    public IReadOnlyList<AuditEntry> GetOperationHistory(string operationId)
    {
        return _memoryBuffer.Where(e => e.OperationId == operationId).ToList();
    }

    /// <summary>
    /// Export audit log to JSON
    /// </summary>
    public string ExportToJson(DateTime? since = null, DateTime? until = null)
    {
        var entries = _memoryBuffer.AsEnumerable();
        
        if (since.HasValue)
        {
            entries = entries.Where(e => e.StartTime >= since.Value);
        }
        
        if (until.HasValue)
        {
            entries = entries.Where(e => e.StartTime <= until.Value);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(entries, options);
    }

    /// <summary>
    /// Flush memory buffer to disk
    /// </summary>
    public void FlushToDisk()
    {
        if (_memoryBuffer.IsEmpty)
        {
            return;
        }

        var entries = new List<AuditEntry>();
        while (_memoryBuffer.TryDequeue(out var entry))
        {
            entries.Add(entry);
        }

        if (entries.Count == 0)
        {
            return;
        }

        lock (_fileLock)
        {
            try
            {
                var lines = entries.Select(e => JsonSerializer.Serialize(e, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }));
                
                File.AppendAllLines(_auditLogPath, lines);
                
                _logger.LogDebug("Flushed {Count} audit entries to {Path}", entries.Count, _auditLogPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush audit log");
                // Re-queue entries for retry
                foreach (var entry in entries)
                {
                    _memoryBuffer.Enqueue(entry);
                }
            }
        }
    }

    /// <summary>
    /// Get audit statistics
    /// </summary>
    public AuditStatistics GetStatistics(TimeSpan? period = null)
    {
        var cutoff = DateTime.UtcNow - (period ?? TimeSpan.FromHours(24));
        var entries = _memoryBuffer.Where(e => e.StartTime >= cutoff).ToList();

        return new AuditStatistics
        {
            TotalOperations = entries.Count,
            Succeeded = entries.Count(e => e.Status == AuditStatus.Succeeded),
            Failed = entries.Count(e => e.Status == AuditStatus.Failed),
            Blocked = entries.Count(e => e.Status == AuditStatus.Blocked),
            Warnings = entries.Count(e => e.Status == AuditStatus.Warning),
            PeriodStart = cutoff,
            PeriodEnd = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Dispose and flush remaining entries
    /// </summary>
    public void Dispose()
    {
        _flushTimer?.Dispose();
        FlushToDisk();
    }
}

/// <summary>
/// Audit entry
/// </summary>
public class AuditEntry
{
    public string Id { get; set; } = "";
    public string? OperationId { get; set; }
    public string OperationName { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public AuditStatus Status { get; set; }
    public TrustLevel? TrustLevel { get; set; }
    public string? ConfirmationId { get; set; }
    public string? Result { get; set; }
    public string? ErrorDetails { get; set; }
    public string MachineName { get; set; } = "";
    public string UserName { get; set; } = "";
    public int ProcessId { get; set; }

    public void SetSucceeded()
    {
        Status = AuditStatus.Succeeded;
        EndTime = DateTime.UtcNow;
    }

    public void SetFailed(string errorType, string errorDetails)
    {
        Status = AuditStatus.Failed;
        ErrorDetails = $"[{errorType}] {errorDetails}";
        EndTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Audit status
/// </summary>
public enum AuditStatus
{
    Started,
    Succeeded,
    Failed,
    Blocked,
    Warning
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    None,
    PathValidationFailed,
    TrustCheckFailed,
    ExtensionBlocked,
    SizeLimitExceeded,
    UserConfirmationDenied,
    UnauthorizedAccess,
    SuspiciousPattern,
    RateLimitExceeded
}

/// <summary>
/// Audit statistics
/// </summary>
public class AuditStatistics
{
    public int TotalOperations { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Blocked { get; set; }
    public int Warnings { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public double SuccessRate => TotalOperations > 0 
        ? (double)Succeeded / TotalOperations * 100 
        : 0;
}
