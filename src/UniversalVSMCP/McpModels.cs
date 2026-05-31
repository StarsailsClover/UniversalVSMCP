using System;
using System.Collections.Generic;
using System.Text.Json;

namespace UniversalVSMCP;

/// <summary>
/// Represents an MCP Server Tool definition
/// Used for HTTP/SSE transport to expose tool metadata
/// </summary>
public class McpServerTool
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object InputSchema { get; set; } = new();
    public object OutputSchema { get; set; } = new();
}

/// <summary>
/// Tool call request for HTTP transport
/// </summary>
public class ToolCallRequest
{
    public string Name { get; set; } = "";
    public Dictionary<string, JsonElement> Arguments { get; set; } = new();
}

/// <summary>
/// Tool call response for HTTP transport
/// </summary>
public class ToolCallResponse
{
    public bool Success { get; set; }
    public string Tool { get; set; } = "";
    public object[] Content { get; set; } = Array.Empty<object>();
}

/// <summary>
/// Server info for HTTP transport
/// </summary>
public class ServerInfoResponse
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string Transport { get; set; } = "";
    public Dictionary<string, string> Endpoints { get; set; } = new();
    public bool VsConnected { get; set; }
    public string? VsVersion { get; set; }
}
