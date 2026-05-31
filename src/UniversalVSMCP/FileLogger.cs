using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP;

/// <summary>
/// Simple file logger provider for UniversalVSMCP
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new object();
    
    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_filePath, _lock);
    }
    
    public void Dispose()
    {
        // Nothing to dispose
    }
}

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lock;
    
    public FileLogger(string filePath, object lockObj)
    {
        _filePath = filePath;
        _lock = lockObj;
    }
    
    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }
    
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}";
        
        if (exception != null)
        {
            logEntry += Environment.NewLine + exception.ToString();
        }
        
        lock (_lock)
        {
            File.AppendAllText(_filePath, logEntry + Environment.NewLine);
        }
    }
}
