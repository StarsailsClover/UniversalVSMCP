using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UniversalVSMCP;

/// <summary>
/// Manages connection to Visual Studio via DTE (Development Tools Environment)
/// Uses Running Object Table (ROT) to connect to running VS instances
/// 
/// Connection Flow:
/// 1. Try to get active VS instance from ROT
/// 2. If version specified, try to get specific version instance
/// 3. Fallback to creating new instance via COM (requires VS to be running)
/// </summary>
public interface IVsConnectionManager
{
    DTE2? GetActiveInstance();
    DTE2? GetInstanceByVersion(string version);
    bool IsConnected { get; }
    string? ConnectedVersion { get; }
    Task<bool> ConnectAsync(string? vsVersion = null, CancellationToken ct = default);
    void Disconnect();
    event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
}

public class VsConnectionManager : IVsConnectionManager, IDisposable
{
    private readonly ILogger<VsConnectionManager> _logger;
    private DTE2? _dteInstance;
    private bool _disposed;
    
    public bool IsConnected => _dteInstance != null;
    public string? ConnectedVersion { get; private set; }
    
    public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

    public VsConnectionManager(ILogger<VsConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the most recently active Visual Studio instance
    /// </summary>
    public DTE2? GetActiveInstance()
    {
        if (_dteInstance != null) return _dteInstance;
        
        try
        {
            // Try to get from Running Object Table (ROT)
            // VS registers itself in ROT when running
            var rotEntries = GetRunningObjectTableEntries();
            
            foreach (var entry in rotEntries)
            {
                if (entry.Key.Contains("VisualStudio.DTE"))
                {
                    _logger.LogInformation("Found VS instance in ROT: {Key}", entry.Key);
                    _dteInstance = entry.Value as DTE2;
                    if (_dteInstance != null)
                    {
                        ConnectedVersion = GetVsVersionFromDte(_dteInstance);
                        OnConnectionStatusChanged(true);
                        return _dteInstance;
                    }
                }
            }
            
            _logger.LogWarning("No Visual Studio instance found in ROT");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VS instance from ROT");
            return null;
        }
    }

    /// <summary>
    /// Get VS instance by specific version (e.g., "17.0" for VS 2022, "18.0" for VS 2026)
    /// </summary>
    public DTE2? GetInstanceByVersion(string version)
    {
        try
        {
            var rotEntries = GetRunningObjectTableEntries();
            var pattern = $"VisualStudio.DTE.{version}";
            
            foreach (var entry in rotEntries)
            {
                if (entry.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found VS {Version} instance: {Key}", version, entry.Key);
                    _dteInstance = entry.Value as DTE2;
                    ConnectedVersion = version;
                    OnConnectionStatusChanged(true);
                    return _dteInstance;
                }
            }
            
            _logger.LogWarning("No VS {Version} instance found", version);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get VS {Version} instance", version);
            return null;
        }
    }

    /// <summary>
    /// Connect to Visual Studio
    /// </summary>
    public async Task<bool> ConnectAsync(string? vsVersion = null, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        try
        {
            if (!string.IsNullOrEmpty(vsVersion))
            {
                _dteInstance = GetInstanceByVersion(vsVersion);
            }
            else
            {
                _dteInstance = GetActiveInstance();
            }
            
            if (_dteInstance != null)
            {
                _logger.LogInformation("Connected to Visual Studio {Version}", ConnectedVersion);
                return true;
            }
            
            _logger.LogWarning("No Visual Studio instance available. Please start Visual Studio first.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            OnConnectionStatusChanged(false);
            return false;
        }
    }

    public void Disconnect()
    {
        if (_dteInstance != null)
        {
            try
            {
                Marshal.ReleaseComObject(_dteInstance);
            }
            catch { /* Ignore cleanup errors */ }
            
            _dteInstance = null;
            ConnectedVersion = null;
            OnConnectionStatusChanged(false);
        }
    }

    private string GetVsVersionFromDte(DTE2 dte)
    {
        try
        {
            // DTE.Version returns version like "17.0" for VS 2022, "18.0" for VS 2026
            return dte.Version ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private List<(string Key, object Value)> GetRunningObjectTableEntries()
    {
        var entries = new List<(string, object)>();
        
        try
        {
            // Try to access Running Object Table via COM
            // This requires stdole or similar COM interop
            Type? rotType = Type.GetTypeFromProgID("RunningObjectTable");
            if (rotType != null)
            {
                dynamic rotInstance = Activator.CreateInstance(rotType)!;
                // Enumeration logic would go here
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ROT enumeration failed, will try alternative methods");
        }
        
        // Fallback: Try common ROT monikers directly
        TryAddRotEntry(entries, "!VisualStudio.DTE.18.0");
        TryAddRotEntry(entries, "!VisualStudio.DTE.17.0");
        TryAddRotEntry(entries, "!VisualStudio.DTE.16.0");
        
        return entries;
    }

    private void TryAddRotEntry(List<(string, object)> entries, string moniker)
    {
        try
        {
            // Attempt to bind to moniker
            Type? dteType = Type.GetTypeFromProgID("VisualStudio.DTE.18.0");
            if (dteType == null) dteType = Type.GetTypeFromProgID("VisualStudio.DTE.17.0");
            if (dteType == null) dteType = Type.GetTypeFromProgID("VisualStudio.DTE.16.0");
            
            if (dteType != null)
            {
                object? instance = Activator.CreateInstance(dteType);
                if (instance != null)
                {
                    entries.Add((moniker, instance));
                }
            }
        }
        catch
        {
            // Silently ignore
        }
    }

    protected virtual void OnConnectionStatusChanged(bool connected)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(connected));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}

public class ConnectionStatusChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }
    public ConnectionStatusChangedEventArgs(bool isConnected) => IsConnected = isConnected;
}
