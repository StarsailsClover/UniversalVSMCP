using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP.Security;

/// <summary>
/// AI Prompt Injection Detector - detects and blocks malicious prompt patterns
/// </summary>
public class PromptInjectionDetector
{
    private readonly ILogger<PromptInjectionDetector> _logger;
    private readonly OperationAuditor _auditor;
    
    // Suspicious patterns that may indicate prompt injection attempts
    private static readonly List<InjectionPattern> SuspiciousPatterns = new()
    {
        // System instruction override attempts
        new InjectionPattern(
            @"ignore\s+(?:previous|all|the)\s+(?:instructions?|commands?|prompts?)",
            "Attempt to override system instructions",
            Severity.Critical),
        
        new InjectionPattern(
            @"(system|assistant|user)\s*:\s*.*?(?:you\s+are|act\s+as|pretend\s+to\s+be)",
            "Role impersonation attempt",
            Severity.High),
        
        // Code execution attempts
        new InjectionPattern(
            @"execute\s+(?:system|shell|cmd|powershell)\s+command",
            "System command execution attempt",
            Severity.Critical),
        
        new InjectionPattern(
            @"(eval|exec|system|Process\.Start)\s*\(",
            "Code evaluation attempt",
            Severity.Critical),
        
        // Data exfiltration attempts
        new InjectionPattern(
            @"(send|upload|post|email)\s+(?:the|this)?\s*(?:content|data|file|code)",
            "Data exfiltration attempt",
            Severity.High),
        
        new InjectionPattern(
            @"(password|secret|key|token|credential|connection\s*string)",
            "Credential access attempt",
            Severity.Critical),
        
        // Path traversal attempts
        new InjectionPattern(
            @"(\.\.|\/etc\/|\/var\/|C:\\Windows|\\\.\.\\)",
            "Path traversal attempt",
            Severity.High),
        
        // Tool manipulation attempts
        new InjectionPattern(
            @"(write|modify|delete|create)\s+.*?(?:malicious|virus|trojan|backdoor)",
            "Malicious code creation attempt",
            Severity.Critical),
        
        new InjectionPattern(
            @"(bypass|disable|turn\s+off|ignore)\s+(?:security|protection|confirmation|trust)",
            "Security bypass attempt",
            Severity.Critical),
        
        // Social engineering patterns
        new InjectionPattern(
            @"(administrator|developer|maintainer|owner)\s+(?:said|asked|told|requested)",
            "Authority impersonation",
            Severity.High),
        
        new InjectionPattern(
            @"(urgent|emergency|critical|immediately)\s+(?:need|must|should)",
            "Urgency manipulation",
            Severity.Medium),
        
        // Encoding/obfuscation attempts
        new InjectionPattern(
            @"(base64|hex|encode|decode|rot13)\s*[:\(]",
            "Obfuscation attempt",
            Severity.Medium),
        
        // Multi-step attack indicators
        new InjectionPattern(
            @"first\s+.*?(?:then|next|after\s+that)\s+.*?(?:finally|lastly)",
            "Multi-step operation sequence",
            Severity.Medium)
    };

    public PromptInjectionDetector(
        ILogger<PromptInjectionDetector> logger,
        OperationAuditor auditor)
    {
        _logger = logger;
        _auditor = auditor;
    }

    /// <summary>
    /// Scan tool arguments for injection attempts
    /// </summary>
    public InjectionScanResult ScanArguments(string toolName, Dictionary<string, object> arguments)
    {
        var findings = new List<InjectionFinding>();
        
        foreach (var kvp in arguments)
        {
            if (kvp.Value is string value)
            {
                var result = ScanText(value);
                if (result.HasFinding)
                {
                    findings.AddRange(result.Findings.Select(f => new InjectionFinding
                    {
                        Pattern = f.Pattern,
                        Severity = f.Severity,
                        Description = f.Description,
                        Location = $"Argument '{kvp.Key}'",
                        MatchedText = f.MatchedText
                    }));
                }
            }
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                // Recursively scan nested dictionaries
                var nestedResult = ScanNested(toolName, kvp.Key, nestedDict);
                findings.AddRange(nestedResult.Findings);
            }
        }

        if (findings.Any())
        {
            _logger.LogWarning("Injection patterns detected in {Tool}: {Count} findings", 
                toolName, findings.Count);
            
            _auditor.LogSecurityEvent(
                SecurityEventType.SuspiciousPattern,
                $"Prompt injection patterns detected in tool {toolName}",
                blocked: findings.Any(f => f.Severity == Severity.Critical));
        }

        return new InjectionScanResult
        {
            HasFinding = findings.Any(),
            Findings = findings,
            ShouldBlock = findings.Any(f => f.Severity == Severity.Critical)
        };
    }

    /// <summary>
    /// Scan text content for injection patterns
    /// </summary>
    public InjectionScanResult ScanText(string text)
    {
        var findings = new List<InjectionFinding>();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            return new InjectionScanResult { HasFinding = false };
        }

        foreach (var pattern in SuspiciousPatterns)
        {
            var matches = pattern.Regex.Matches(text);
            foreach (Match match in matches)
            {
                findings.Add(new InjectionFinding
                {
                    Pattern = pattern.Name,
                    Severity = pattern.Severity,
                    Description = pattern.Description,
                    Location = "Text content",
                    MatchedText = match.Value
                });

                _logger.LogDebug("Detected pattern '{Pattern}' in text: {Match}",
                    pattern.Name, match.Value.Substring(0, Math.Min(match.Value.Length, 50)));
            }
        }

        return new InjectionScanResult
        {
            HasFinding = findings.Any(),
            Findings = findings,
            ShouldBlock = findings.Any(f => f.Severity == Severity.Critical)
        };
    }

    /// <summary>
    /// Validate and sanitize file content before writing
    /// </summary>
    public FileValidationResult ValidateFileContent(string content, string filePath)
    {
        var scanResult = ScanText(content);
        
        if (scanResult.ShouldBlock)
        {
            _logger.LogError("Critical injection patterns detected in file content for {Path}", filePath);
            _auditor.LogSecurityEvent(
                SecurityEventType.SuspiciousPattern,
                $"Blocked file write due to injection patterns: {filePath}",
                filePath,
                blocked: true);
            
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = "File contains suspicious patterns that may indicate prompt injection or malicious code",
                Findings = scanResult.Findings
            };
        }

        // Additional file-specific checks
        var extension = Path.GetExtension(filePath).ToLower();
        
        // Check for dangerous file types
        if (DangerousExtensions.Contains(extension))
        {
            _logger.LogWarning("Attempt to write dangerous file type: {Extension}", extension);
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Writing '{extension}' files is not allowed for security reasons"
            };
        }

        return new FileValidationResult { IsValid = true };
    }

    private InjectionScanResult ScanNested(string toolName, string parentKey, Dictionary<string, object> dict)
    {
        var findings = new List<InjectionFinding>();
        
        foreach (var kvp in dict)
        {
            if (kvp.Value is string value)
            {
                var result = ScanText(value);
                if (result.HasFinding)
                {
                    findings.AddRange(result.Findings.Select(f => new InjectionFinding
                    {
                        Pattern = f.Pattern,
                        Severity = f.Severity,
                        Description = f.Description,
                        Location = $"Nested '{parentKey}.{kvp.Key}'",
                        MatchedText = f.MatchedText
                    }));
                }
            }
        }

        return new InjectionScanResult
        {
            HasFinding = findings.Any(),
            Findings = findings
        };
    }

    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".sys", ".bat", ".cmd", ".ps1",
        ".sh", ".com", ".msi", ".msp", ".scr",
        ".hta", ".js", ".vbs", ".wsf", ".jar"
    };
}

/// <summary>
/// Injection detection pattern
/// </summary>
public class InjectionPattern
{
    public string Name { get; }
    public Regex Regex { get; }
    public string Description { get; }
    public Severity Severity { get; }

    public InjectionPattern(string pattern, string description, Severity severity)
    {
        Name = pattern.Substring(0, Math.Min(pattern.Length, 30));
        Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Description = description;
        Severity = severity;
    }
}

/// <summary>
/// Injection finding
/// </summary>
public class InjectionFinding
{
    public string Pattern { get; set; } = "";
    public Severity Severity { get; set; }
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string MatchedText { get; set; } = "";
}

/// <summary>
/// Injection scan result
/// </summary>
public class InjectionScanResult
{
    public bool HasFinding { get; set; }
    public bool ShouldBlock { get; set; }
    public List<InjectionFinding> Findings { get; set; } = new();
}

/// <summary>
/// File validation result
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<InjectionFinding>? Findings { get; set; }
}

/// <summary>
/// Severity levels
/// </summary>
public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}
