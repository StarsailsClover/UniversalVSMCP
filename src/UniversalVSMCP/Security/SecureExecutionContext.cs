using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// Secure Execution Context - provides sandbox isolation for file/code operations
/// Implements defense-in-depth for AI-driven operations
/// </summary>
public class SecureExecutionContext
{
    private readonly ILogger<SecureExecutionContext> _logger;
    private readonly WorkspaceTrustManager _trustManager;
    private readonly UserConfirmationManager _confirmationManager;
    private readonly OperationAuditor _auditor;

    // Maximum file sizes for read/write operations
    private const long MaxReadFileSize = 10 * 1024 * 1024; // 10 MB
    private const long MaxWriteFileSize = 5 * 1024 * 1024;  // 5 MB

    // Blocked file extensions
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".sys", ".bat", ".cmd", ".ps1",
        ".sh", ".com", ".msi", ".msp", ".scr"
    };

    public SecureExecutionContext(
        ILogger<SecureExecutionContext> logger,
        WorkspaceTrustManager trustManager,
        UserConfirmationManager confirmationManager,
        OperationAuditor auditor)
    {
        _logger = logger;
        _trustManager = trustManager;
        _confirmationManager = confirmationManager;
        _auditor = auditor;
    }

    /// <summary>
    /// Execute a file operation with full security checks
    /// </summary>
    public async Task<SecureOperationResult<T>> ExecuteFileOperationAsync<T>(
        string operationId,
        string operationName,
        string filePath,
        string description,
        Func<string, Task<T>> operation)
    {
        var auditEntry = _auditor.StartOperation(operationId, operationName, filePath, description);

        try
        {
            // 1. Validate path
            if (!ValidatePath(filePath, out var validationError))
            {
                auditEntry.SetFailed("PathValidation", validationError);
                return SecureOperationResult<T>.Failure(validationError, SecurityErrorType.InvalidPath);
            }

            // 2. Check trust level
            var trustLevel = _trustManager.CheckTrustLevel(filePath);
            auditEntry.TrustLevel = trustLevel;

            if (trustLevel == TrustLevel.Blocked)
            {
                var error = $"Access denied: {filePath} is in a blocked directory";
                auditEntry.SetFailed("TrustCheck", error);
                _logger.LogError(error);
                return SecureOperationResult<T>.Failure(error, SecurityErrorType.AccessDenied);
            }

            // 3. Determine if confirmation is needed
            var fileOp = GetFileOperationType(operationName);
            
            if (trustLevel != TrustLevel.Trusted && !_trustManager.VerifyFileOperation(filePath, fileOp))
            {
                // 4. Request user confirmation for untrusted/restricted paths
                var confirmationLevel = trustLevel == TrustLevel.Restricted 
                    ? ConfirmationLevel.Standard 
                    : ConfirmationLevel.Strong;

                var confirmation = await _confirmationManager.RequestConfirmationAsync(
                    operationId,
                    operationName,
                    description,
                    filePath,
                    confirmationLevel);

                if (!confirmation.Approved)
                {
                    var error = $"Operation denied by user: {confirmation.Reason}";
                    auditEntry.SetFailed("UserDenied", error);
                    return SecureOperationResult<T>.Failure(error, SecurityErrorType.UserDenied);
                }

                auditEntry.ConfirmationId = confirmation.ConfirmationId;
            }

            // 5. Check file extension
            if (!ValidateFileExtension(filePath))
            {
                var error = $"File type not allowed: {Path.GetExtension(filePath)}";
                auditEntry.SetFailed("ExtensionCheck", error);
                return SecureOperationResult<T>.Failure(error, SecurityErrorType.InvalidFileType);
            }

            // 6. Check file size for read operations
            if (fileOp == FileOperation.Read && File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MaxReadFileSize)
                {
                    var error = $"File too large: {fileInfo.Length} bytes (max: {MaxReadFileSize})";
                    auditEntry.SetFailed("SizeCheck", error);
                    return SecureOperationResult<T>.Failure(error, SecurityErrorType.FileTooLarge);
                }
            }

            // 7. Execute operation
            var result = await operation(filePath);
            
            auditEntry.SetSucceeded();
            return SecureOperationResult<T>.Success(result);
        }
        catch (SecurityException ex)
        {
            var error = $"Security exception: {ex.Message}";
            auditEntry.SetFailed("SecurityException", error);
            _logger.LogError(ex, "Security exception in file operation");
            return SecureOperationResult<T>.Failure(error, SecurityErrorType.SecurityException);
        }
        catch (Exception ex)
        {
            var error = $"Operation failed: {ex.Message}";
            auditEntry.SetFailed("Exception", error);
            _logger.LogError(ex, "Exception in file operation");
            return SecureOperationResult<T>.Failure(error, SecurityErrorType.OperationFailed);
        }
    }

    /// <summary>
    /// Execute a build operation with security checks
    /// </summary>
    public async Task<SecureOperationResult<T>> ExecuteBuildOperationAsync<T>(
        string operationId,
        string operationName,
        string solutionPath,
        string description,
        Func<string, Task<T>> operation)
    {
        // Build operations are high-risk - always require confirmation
        var confirmation = await _confirmationManager.RequestConfirmationAsync(
            operationId,
            operationName,
            description,
            solutionPath,
            ConfirmationLevel.Standard);

        if (!confirmation.Approved)
        {
            return SecureOperationResult<T>.Failure(
                $"Build operation denied: {confirmation.Reason}", 
                SecurityErrorType.UserDenied);
        }

        return await ExecuteFileOperationAsync(operationId, operationName, solutionPath, description, operation);
    }

    /// <summary>
    /// Validate file path
    /// </summary>
    private bool ValidatePath(string path, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Path is empty";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            
            // Check for path traversal
            if (fullPath.Contains("..") || fullPath.Contains("~"))
            {
                error = "Path contains invalid characters";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Invalid path: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Validate file extension
    /// </summary>
    private bool ValidateFileExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return !BlockedExtensions.Contains(extension);
    }

    /// <summary>
    /// Get file operation type from operation name
    /// </summary>
    private FileOperation GetFileOperationType(string operationName)
    {
        return operationName.ToLower() switch
        {
            var s when s.Contains("read") => FileOperation.Read,
            var s when s.Contains("write") => FileOperation.Write,
            var s when s.Contains("delete") => FileOperation.Delete,
            var s when s.Contains("list") => FileOperation.List,
            _ => FileOperation.Read
        };
    }
}

/// <summary>
/// Secure operation result
/// </summary>
public class SecureOperationResult<T>
{
    public bool Success { get; set; }
    public T? Value { get; set; }
    public string? Error { get; set; }
    public SecurityErrorType ErrorType { get; set; }

    public static SecureOperationResult<T> Ok(T value) => new()
    {
        Success = true,
        Value = value
    };

    public static SecureOperationResult<T> Failure(string error, SecurityErrorType type) => new()
    {
        Success = false,
        Error = error,
        ErrorType = type
    };
}

/// <summary>
/// Security error types
/// </summary>
public enum SecurityErrorType
{
    None,
    InvalidPath,
    AccessDenied,
    InvalidFileType,
    FileTooLarge,
    UserDenied,
    SecurityException,
    OperationFailed
}
