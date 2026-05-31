# UniversalVSMCP (UVM)
## .NET Native MCP Server for Visual Studio 2026/2022

**Enable AI Agents to natively control Visual Studio 2026/2022 via the Model Context Protocol (MCP).**

---

## Installation (Single Method)

### NuGet Package (Recommended)

```bash
dotnet tool install -g UniversalVSMCP
```

Run:

```bash
universal-vsmcp --stdio
```

Verify installation:

```bash
universal-vsmcp --help
```

**Package:** https://www.nuget.org/packages/UniversalVSMCP/

---

## Quick Start

### 1. Start Visual Studio 2026
Open any solution or create a new one.

### 2. Run the MCP Server
```bash
universal-vsmcp --stdio
```

### 3. Configure in VS 2026
Go to **Tools → Options → Environment → Extensions**:
- Add MCP Server: `universal-vsmcp`
- Command: `dotnet tool run --global universal-vsmcp -- --stdio`

---

## Available Tools (30 Total)

### Solution Management (6)
| Tool | Description |
|------|-------------|
| `get_solution_projects` | Get all projects in solution |
| `get_solution_path` | Get solution file path |
| `get_solution_name` | Get solution name |
| `open_solution` | Open .sln file |
| `close_solution` | Close current solution |
| `create_solution` | Create new solution (workspace) |

### Project Management (5)
| Tool | Description |
|------|-------------|
| `get_project_files` | Get files in a project |
| `get_startup_projects` | Get startup project(s) |
| `add_file_to_project` | Add file to project |
| `set_startup_project` | Set startup project |
| `get_project_properties` | Get project properties |

### File Operations (5)
| Tool | Description |
|------|-------------|
| `open_file` | Open file in VS editor |
| `read_file` | Read file content |
| `write_file` | Write file content |
| `replace_in_file` | Replace text in file |
| `get_file_info` | Get file information |

### Build Operations (6)
| Tool | Description |
|------|-------------|
| `build_solution` | Build solution |
| `rebuild_solution` | Rebuild solution |
| `clean_solution` | Clean solution |
| `build_project` | Build specific project |
| `get_build_errors` | Get build errors |
| `get_build_configurations` | Get build configs |

### Debug Control (8)
| Tool | Description |
|------|-------------|
| `start_debugging` | Start debugging (F5) |
| `stop_debugging` | Stop debugging (Shift+F5) |
| `toggle_breakpoint` | Navigate to line for breakpoint |
| `continue_execution` | Continue from breakpoint (F5) |
| `step_over` | Step over (F10) |
| `step_into` | Step into (F11) |
| `step_out` | Step out (Shift+F11) |
| `get_debug_state` | Get debug state |

### Diagnostics (3)
| Tool | Description |
|------|-------------|
| `health_check` | Verify server and VS connection |
| `get_server_info` | Get server information |
| `get_diagnostic_logs` | Get recent diagnostic logs |

---

## Command Line Options

```bash
universal-vsmcp --stdio                    # Default: stdio transport
universal-vsmcp --stdio --log-file uv.log  # With file logging
universal-vsmcp --verify                   # Verify VS connection
universal-vsmcp --help                     # Show help
```

---

## Configuration

### VS 2026 Configuration

**Method A: Direct JSON (Recommended)**
1. Go to **Tools → Options → Environment → Extensions**
2. Check **"Edit user settings as JSON"**
3. Paste:

```json
{
  "mcpServers": {
    "universal-vsmcp": {
      "command": "dotnet",
      "args": [
        "tool",
        "run",
        "--global",
        "universal-vsmcp",
        "--",
        "--stdio"
      ],
      "env": {
        "VS_AUTO_DETECT": "true"
      }
    }
  }
}
```

**Method B: GUI**
1. **Tools → Options → Environment → Extensions**
2. In **MCP Server List**, click **Add**
3. Fill in:
   - **Name**: `universal-vsmcp`
   - **Command**: `dotnet`
   - **Args**: `tool run --global universal-vsmcp -- --stdio`

### Claude Desktop / Cursor

```json
{
  "mcpServers": {
    "vsmcp": {
      "command": "dotnet",
      "args": ["tool", "run", "--global", "universal-vsmcp", "--", "--stdio"],
      "env": {
        "VS_AUTO_DETECT": "true"
      }
    }
  }
}
```

---

## Localhost Support

For local development or SSE transport:

```json
{
  "mcpServers": {
    "vsmcp-local": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}",
        "--",
        "--stdio"
      ],
      "transport": "stdio",
      "url": "http://localhost:6277/sse"
    }
  }
}
```

---

## Troubleshooting

### Verify Connection

```bash
# Check if server is installed
dotnet tool list -g

# Run health check tool from MCP client
# Or run with logging:
universal-vsmcp --stdio --log-file uv.log
```

### Common Issues

| Issue | Solution |
|-------|----------|
| "No VS instance found" | Start Visual Studio first |
| "Server not responding" | Check `UVM_LOG_FILE` for errors |
| "Tool not found" | Verify server is running |
| "Connection failed" | Ensure VS is not running as Admin |

---

## Requirements

- .NET 8.0 SDK or later
- Visual Studio 2026 (18.0) or VS 2022 (17.14+)
- Windows 10 or later

---

## License

MIT © StarsailsClover

---

## Links

- [NuGet Package](https://www.nuget.org/packages/UniversalVSMCP/)
- [GitHub Repository](https://github.com/StarsailsClover/UniversalVSMCP)
- [MCP Documentation](https://modelcontextprotocol.io/)
- [VS 2026 MCP Guide](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
