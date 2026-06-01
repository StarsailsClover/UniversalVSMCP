using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace UniversalVSMCP.Security;

/// <summary>
/// HTTP Authentication Middleware - provides API key and token-based authentication
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly AuthenticationConfig _config;
    private readonly ConcurrentDictionary<string, TokenInfo> _activeTokens;
    private readonly ConcurrentDictionary<string, int> _failedAttempts;

    public AuthenticationMiddleware(
        RequestDelegate next,
        ILogger<AuthenticationMiddleware> logger,
        AuthenticationConfig config)
    {
        _next = next;
        _logger = logger;
        _config = config;
        _activeTokens = new ConcurrentDictionary<string, TokenInfo>();
        _failedAttempts = new ConcurrentDictionary<string, int>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health check endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check IP whitelist
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!IsIpAllowed(clientIp))
        {
            _logger.LogWarning("Authentication failed: IP {Ip} not in whitelist", clientIp);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden: IP not allowed");
            return;
        }

        // Check authentication
        if (_config.RequireAuthentication)
        {
            var isAuthenticated = await AuthenticateRequest(context);
            if (!isAuthenticated)
            {
                RecordFailedAttempt(clientIp);
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", "Bearer, X-Api-Key");
                await context.Response.WriteAsync("Unauthorized: Invalid or missing credentials");
                return;
            }
        }

        await _next(context);
    }

    private async Task<bool> AuthenticateRequest(HttpContext context)
    {
        // Try Bearer token
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var auth = authHeader.ToString();
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth.Substring(7);
                return ValidateToken(token);
            }
        }

        // Try API Key
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            return ValidateApiKey(apiKeyHeader.ToString());
        }

        // Try query parameter (for SSE connections)
        if (context.Request.Query.TryGetValue("token", out var queryToken))
        {
            return ValidateToken(queryToken.ToString());
        }

        return false;
    }

    private bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token) || !_activeTokens.TryGetValue(token, out var info))
        {
            return false;
        }

        if (info.ExpiresAt < DateTime.UtcNow)
        {
            _activeTokens.TryRemove(token, out _);
            return false;
        }

        return true;
    }

    private bool ValidateApiKey(string apiKey)
    {
        // Hash the provided key and compare
        var hashedKey = HashKey(apiKey);
        return _config.AllowedApiKeys.Contains(hashedKey);
    }

    private bool IsIpAllowed(string ip)
    {
        if (!_config.EnableIpWhitelist)
        {
            return true;
        }

        return _config.AllowedIps.Contains(ip) || 
               ip == "127.0.0.1" || 
               ip == "::1" ||
               ip == "localhost";
    }

    private void RecordFailedAttempt(string ip)
    {
        _failedAttempts.AddOrUpdate(ip, 1, (_, count) => count + 1);
        
        if (_failedAttempts[ip] >= _config.MaxFailedAttempts)
        {
            _logger.LogWarning("IP {Ip} blocked due to {Count} failed attempts", ip, _failedAttempts[ip]);
            // Could add IP blocking logic here
        }
    }

    private static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hash);
    }

    public string GenerateToken(string clientId, TimeSpan? duration = null)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var info = new TokenInfo
        {
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + (duration ?? TimeSpan.FromHours(1))
        };

        _activeTokens[token] = info;
        return token;
    }

    public void RevokeToken(string token)
    {
        _activeTokens.TryRemove(token, out _);
    }
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthenticationConfig
{
    public bool RequireAuthentication { get; set; } = true;
    public bool EnableIpWhitelist { get; set; } = false;
    public List<string> AllowedIps { get; set; } = new();
    public List<string> AllowedApiKeys { get; set; } = new(); // Store hashed keys
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Token information
/// </summary>
public class TokenInfo
{
    public string ClientId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
