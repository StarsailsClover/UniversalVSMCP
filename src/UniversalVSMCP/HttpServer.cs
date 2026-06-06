using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UniversalVSMCP;

/// <summary>
/// Simple HTTP/SSE MCP Server for Visual Studio 2026 MCP Registry support
/// Provides localhost endpoint for VS 2026 MCP Server Manager
/// 
/// URL: http://localhost:5000/sse
/// </summary>
public class HttpMcpServer : IDisposable
{
    private readonly ILogger<HttpMcpServer> _logger;
    private readonly IVsConnectionManager _vsManager;
    private readonly IEnumerable<McpServerTool> _tools;
    private HttpListener? _listener;
    private readonly ConcurrentDictionary<string, HttpListenerResponse> _sseClients = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public HttpMcpServer(ILogger<HttpMcpServer> logger, IVsConnectionManager vsManager, IEnumerable<McpServerTool> tools)
    {
        _logger = logger;
        _vsManager = vsManager;
        _tools = tools;
    }

    /// <summary>
    /// Start HTTP server on specified port
    /// </summary>
    public async Task StartAsync(int port = 5000, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting HTTP MCP Server on port {Port}...", port);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            
            _logger.LogInformation("HTTP MCP Server listening on http://localhost:{Port}", port);
            _logger.LogInformation("VS 2026 can connect to: http://localhost:{Port}/sse", port);
            
            _listenerTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
                    }
                    catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                    {
                        // Expected when stopping
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accepting HTTP connection");
                    }
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start HTTP server on port {Port}", port);
            throw;
        }
    }

    /// <summary>
    /// Handle incoming HTTP request
    /// </summary>
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        
        try
        {
            var path = request.Url?.AbsolutePath ?? "/";
            _logger.LogDebug("HTTP {Method} {Path}", request.HttpMethod, path);

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            switch (path.ToLower())
            {
                case "/sse":
                    await HandleSseConnection(request, response);
                    break;
                case "/health":
                    await HandleHealthCheck(response);
                    break;
                case "/info":
                    await HandleServerInfo(response);
                    break;
                case "/tools":
                    await HandleToolsList(response);
                    break;
                case "/tools/call":
                    if (request.HttpMethod == "POST")
                        await HandleToolCall(request, response);
                    else
                        await SendError(response, 405, "Method not allowed");
                    break;
                default:
                    await SendError(response, 404, $"Not found: {path}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HTTP request");
            try
            {
                await SendError(response, 500, $"Internal error: {ex.Message}");
            }
            catch { /* Ignore */ }
        }
    }

    /// <summary>
    /// Handle SSE connection for MCP
    /// </summary>
    private async Task HandleSseConnection(HttpListenerRequest request, HttpListenerResponse response)
    {
        var clientId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("New SSE client connected: {ClientId}", clientId);

        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.KeepAlive = true;

        _sseClients[clientId] = response;
        var outputStream = response.OutputStream;

        // Send initial server info
        var serverInfo = new
        {
            name = "universal-vsmcp",
            version = "26.0.2",
            capabilities = new { tools = new { listChanged = true } }
        };
        await SendSseEvent(outputStream, "server-info", JsonSerializer.Serialize(serverInfo, _jsonOptions));

        // Send tool list
        var toolsList = new
        {
            tools = _tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            }).ToList()
        };
        await SendSseEvent(outputStream, "tools-list", JsonSerializer.Serialize(toolsList, _jsonOptions));

        // Keep connection alive
        try
        {
            while (!_cts?.IsCancellationRequested ?? true)
            {
                await SendSseEvent(outputStream, "ping", "{}");
                await Task.Delay(30000); // 30s heartbeat
            }
        }
        catch (Exception)
        {
            // Normal disconnection
        }
        finally
        {
            _sseClients.TryRemove(clientId, out _);
            _logger.LogInformation("SSE client disconnected: {ClientId}", clientId);
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    private async Task HandleHealthCheck(HttpListenerResponse response)
    {
        var health = new
        {
            status = "healthy",
            vsConnected = _vsManager.IsConnected,
            timestamp = DateTime.UtcNow
        };
        
        await SendJson(response, health);
    }

    /// <summary>
    /// Server info endpoint
    /// </summary>
    private async Task HandleServerInfo(HttpListenerResponse response)
    {
        var info = new
        {
            name = "universal-vsmcp",
            version = "26.0.2",
            description = "MCP Server for Visual Studio 2026/2022 automation via DTE/OM",
            transport = "http-sse",
            endpoints = new
            {
                sse = "/sse",
                health = "/health",
                tools = "/tools",
                toolCall = "/tools/call"
            },
            vsConnected = _vsManager.IsConnected,
            vsVersion = _vsManager.ConnectedVersion
        };
        
        await SendJson(response, info);
    }

    /// <summary>
    /// Tools list endpoint
    /// </summary>
    private async Task HandleToolsList(HttpListenerResponse response)
    {
        var tools = new
        {
            tools = _tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema,
                outputSchema = t.OutputSchema
            }).ToList()
        };
        
        await SendJson(response, tools);
    }

    /// <summary>
    /// Tool call endpoint
    /// </summary>
    private async Task HandleToolCall(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync();
        
        try
        {
            var jsonDoc = JsonDocument.Parse(body);
            var toolName = jsonDoc.RootElement.GetProperty("name").GetString() ?? "";
            var arguments = jsonDoc.RootElement.TryGetProperty("arguments", out var args) 
                ? args 
                : JsonDocument.Parse("{}").RootElement;

            _logger.LogInformation("Tool call via HTTP: {ToolName}", toolName);

            // Find tool
            var tool = _tools.FirstOrDefault(t => t.Name == toolName);
            if (tool == null)
            {
                await SendError(response, 404, $"Tool not found: {toolName}");
                return;
            }

            // For now, return a placeholder response
            // Full implementation would invoke the actual tool
            var result = new
            {
                success = true,
                tool = toolName,
                arguments,
                content = new[] { new { type = "text", text = $"Tool '{toolName}' would be executed here. Connect via stdio for full functionality." } }
            };

            await SendJson(response, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tool call");
            await SendError(response, 500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Send JSON response
    /// </summary>
    private async Task SendJson(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    /// <summary>
    /// Send SSE event
    /// </summary>
    private async Task SendSseEvent(Stream stream, string eventName, string data)
    {
        var message = $"event: {eventName}\ndata: {data}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    /// <summary>
    /// Send error response
    /// </summary>
    private async Task SendError(HttpListenerResponse response, int code, string message)
    {
        response.StatusCode = code;
        var error = new { error = message, code };
        await SendJson(response, error);
    }

    /// <summary>
    /// Stop HTTP server
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping HTTP MCP Server...");
        
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        
        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch { /* Ignore */ }
        }
        
        _logger.LogInformation("HTTP MCP Server stopped");
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }
}

/// <summary>
/// Server configuration for transport mode
/// </summary>
public enum TransportMode
{
    Stdio,
    Http,
    Hybrid
}

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfig
{
    public TransportMode TransportMode { get; set; } = TransportMode.Stdio;
    public int HttpPort { get; set; } = 5000;
    public string? LogFile { get; set; }
    public string? VsVersion { get; set; }
    public string? SolutionPath { get; set; }
    public bool Verify { get; set; }
    public bool ShowHelp { get; set; }
}
