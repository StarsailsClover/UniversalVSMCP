using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UniversalVSMCP.IdeAbstraction;
using UniversalVSMCP.IdeRouting;

namespace UniversalVSMCP.Tools;

/// <summary>
/// Extension Tools - Manage VS Code Extension MCP Server
/// </summary>
[McpServerToolType]
public class ExtensionTools
{
    private readonly IdeRouter _ideRouter;
    private readonly ILogger<ExtensionTools> _logger;
    private readonly ConfigurationManager _config;
    private Process? _extensionProcess;

    public ExtensionTools(
        IdeRouter ideRouter, 
        ILogger<ExtensionTools> logger,
        ConfigurationManager config)
    {
        _ideRouter = ideRouter;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Start VS Code Extension MCP Server
    /// </summary>
    [McpServerTool(Name = "start_vscode_extension_server",
        Title = "Start VS Code Extension MCP Server for AI Agent connection")]
    public async Task<ExtensionServerResult> StartVsCodeExtensionServer(
        string? port = null,
        string? npxConfig = null,
        CancellationToken ct = default)
    {
        try
        {
            var serverPort = port ?? "5001";
            var config = npxConfig ?? "@modelcontextprotocol/server-vscode";
            
            _logger.LogInformation("Starting VS Code Extension MCP Server on port {Port} with config {Config}", 
                serverPort, config);

            // Check if already running
            if (_extensionProcess != null && !_extensionProcess.HasExited)
            {
                return new ExtensionServerResult
                {
                    Success = true,
                    Message = "VS Code Extension Server already running",
                    Port = serverPort,
                    Pid = _extensionProcess.Id,
                    IsRunning = true
                };
            }

            // Build npx command
            var startInfo = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = $"-y {config} --port {serverPort}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            _extensionProcess = new Process { StartInfo = startInfo };
            
            _extensionProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("[Extension Server] {Output}", e.Data);
                }
            };
            
            _extensionProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogError("[Extension Server] {Error}", e.Data);
                }
            };

            var started = _extensionProcess.Start();
            
            if (started)
            {
                _extensionProcess.BeginOutputReadLine();
                _extensionProcess.BeginErrorReadLine();
                
                // Wait a moment for server to start
                await Task.Delay(2000, ct);
                
                if (!_extensionProcess.HasExited)
                {
                    _logger.LogInformation("VS Code Extension MCP Server started on port {Port}", serverPort);
                    
                    // Generate connection URL
                    var connectionUrl = $"http://localhost:{serverPort}/sse";
                    
                    return new ExtensionServerResult
                    {
                        Success = true,
                        Message = $"VS Code Extension MCP Server started successfully",
                        Port = serverPort,
                        Pid = _extensionProcess.Id,
                        IsRunning = true,
                        ConnectionUrl = connectionUrl,
                        NpxConfig = config,
                        Instructions = $"""
                            VS Code Extension MCP Server is running!
                            
                            Connection URL: {connectionUrl}
                            
                            To connect AI Agent:
                            1. Configure your AI Agent (Claude/Cursor) with:
                               {{
                                 "mcpServers": {{
                                   "vscode-extension": {{
                                     "url": "{connectionUrl}",
                                     "transport": "sse"
                                   }}
                                 }}
                               }}
                            
                            2. Or use stdio transport:
                               npx -y {config} --stdio
                            
                            Server PID: {_extensionProcess.Id}
                            Port: {serverPort}
                            """
                    };
                }
            }

            return new ExtensionServerResult
            {
                Success = false,
                Message = "Failed to start VS Code Extension MCP Server",
                Port = serverPort,
                IsRunning = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start VS Code Extension MCP Server");
            return new ExtensionServerResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Stop VS Code Extension MCP Server
    /// </summary>
    [McpServerTool(Name = "stop_vscode_extension_server",
        Title = "Stop VS Code Extension MCP Server")]
    public async Task<ExtensionServerResult> StopVsCodeExtensionServer(CancellationToken ct = default)
    {
        try
        {
            if (_extensionProcess == null || _extensionProcess.HasExited)
            {
                return new ExtensionServerResult
                {
                    Success = true,
                    Message = "VS Code Extension Server is not running",
                    IsRunning = false
                };
            }

            _logger.LogInformation("Stopping VS Code Extension MCP Server (PID: {Pid})", _extensionProcess.Id);

            // Try graceful shutdown first
            _extensionProcess.Kill(true);
            
            // Wait for process to exit
            await Task.Run(() => _extensionProcess.WaitForExit(5000), ct);

            var isRunning = !_extensionProcess.HasExited;
            
            return new ExtensionServerResult
            {
                Success = !isRunning,
                Message = isRunning ? "Failed to stop server" : "VS Code Extension Server stopped",
                Pid = _extensionProcess.Id,
                IsRunning = isRunning
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop VS Code Extension MCP Server");
            return new ExtensionServerResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get Extension Server status
    /// </summary>
    [McpServerTool(Name = "get_extension_server_status",
        Title = "Get VS Code Extension MCP Server status")]
    public ExtensionServerResult GetExtensionServerStatus()
    {
        var isRunning = _extensionProcess != null && !_extensionProcess.HasExited;
        
        return new ExtensionServerResult
        {
            Success = true,
            Message = isRunning ? "Server is running" : "Server is not running",
            Pid = isRunning ? _extensionProcess?.Id : null,
            IsRunning = isRunning
        };
    }

    /// <summary>
    /// Get AI Agent connection configuration
    /// </summary>
    [McpServerTool(Name = "get_ai_agent_config",
        Title = "Get AI Agent connection configuration for VS Code Extension")]
    public AiAgentConfigResult GetAiAgentConfig(string? port = null)
    {
        var serverPort = port ?? "5001";
        var connectionUrl = $"http://localhost:{serverPort}/sse";
        
        return new AiAgentConfigResult
        {
            Success = true,
            Config = new AiAgentConfig
            {
                Claude = new McpServerConfig
                {
                    McpServers = new McpServers
                    {
                        VsCodeExtension = new ServerEntry
                        {
                            Url = connectionUrl,
                            Transport = "sse"
                        }
                    }
                },
                Cursor = new McpServerConfig
                {
                    McpServers = new McpServers
                    {
                        VsCodeExtension = new ServerEntry
                        {
                            Url = connectionUrl,
                            Transport = "sse"
                        }
                    }
                }
            },
            Instructions = $"""
                Add this to your AI Agent MCP configuration:
                
                Claude Desktop (claude_desktop_config.json):
                {{
                  "mcpServers": {{
                    "vscode-extension": {{
                      "url": "{connectionUrl}",
                      "transport": "sse"
                    }}
                  }}
                }}
                
                Cursor (mcp.json):
                {{
                  "servers": {{
                    "vscode-extension": {{
                      "url": "{connectionUrl}",
                      "transport": "sse"
                    }}
                  }}
                }}
                """
        };
    }

    /// <summary>
    /// Install VS Code Extension from marketplace
    /// </summary>
    [McpServerTool(Name = "install_vscode_extension",
        Title = "Install Universal VS MCP VS Code Extension from marketplace")]
    public ExtensionInstallResult InstallVsCodeExtension(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing VS Code Extension from marketplace");

            var extensionId = "StarsailsClover.universal-vsmcp";
            
            // Try to use VS Code CLI
            var startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"--install-extension {extensionId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return new ExtensionInstallResult
                {
                    Success = true,
                    Message = $"Extension {extensionId} installed successfully",
                    ExtensionId = extensionId,
                    MarketplaceUrl = $"https://marketplace.visualstudio.com/items?itemName={extensionId}",
                    OpenVsxUrl = $"https://open-vsx.org/extension/{extensionId.Replace(".", "/")}"
                };
            }
            else
            {
                return new ExtensionInstallResult
                {
                    Success = false,
                    Message = $"Installation failed: {error}",
                    ExtensionId = extensionId,
                    ManualInstallInstructions = $"""
                        Please install manually:
                        1. Open VS Code
                        2. Go to Extensions (Ctrl+Shift+X)
                        3. Search for "Universal VS MCP"
                        4. Click Install
                        
                        Or use command line:
                        code --install-extension {extensionId}
                        """
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install VS Code Extension");
            return new ExtensionInstallResult
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                ManualInstallInstructions = "Please install from VS Code Marketplace manually"
            };
        }
    }
}

// Result types
public class ExtensionServerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Port { get; set; }
    public int? Pid { get; set; }
    public bool IsRunning { get; set; }
    public string? ConnectionUrl { get; set; }
    public string? NpxConfig { get; set; }
    public string? Instructions { get; set; }
}

public class AiAgentConfigResult
{
    public bool Success { get; set; }
    public AiAgentConfig? Config { get; set; }
    public string? Instructions { get; set; }
}

public class AiAgentConfig
{
    public McpServerConfig? Claude { get; set; }
    public McpServerConfig? Cursor { get; set; }
}

public class McpServerConfig
{
    public McpServers? McpServers { get; set; }
}

public class McpServers
{
    public ServerEntry? VsCodeExtension { get; set; }
}

public class ServerEntry
{
    public string? Url { get; set; }
    public string? Transport { get; set; } = "sse";
}

public class ExtensionInstallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? ExtensionId { get; set; }
    public string? MarketplaceUrl { get; set; }
    public string? OpenVsxUrl { get; set; }
    public string? ManualInstallInstructions { get; set; }
}
