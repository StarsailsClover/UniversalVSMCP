# UniversalVSMCP (UVM)
## .NET Native MCP Server for Visual Studio 2026/2022

**Enable AI Agents to natively control Visual Studio 2026/2022 via the Model Context Protocol (MCP).**

---

## What's New in v1.0.0

- **Native .NET 8.0 Implementation** - No Python or Node.js dependencies
- **28 MCP Tools** - Comprehensive VS automation (Solution, Project, File, Build, Debug)
- **MCP Registry Compatible** - One-click installation in VS 2026
- **Global .NET Tool** - `dotnet tool install -g UniversalVSMCP`
- **Native DTE/COM Integration** - Direct Windows COM interop with Visual Studio

---

## Why .NET Native?

| Feature | Python (Legacy) | .NET Native (Current) |
|---------|----------------|----------------------|
| VS 2026 Integration | Manual config required | One-click from MCP Registry |
| Runtime Dependencies | Python + uvx | .NET 8.0 SDK only |
| DTE Automation | via pywin32 | Native COM interop |
| Performance | Medium | Optimal |
| VS 2026 Discovery | Not supported | Auto-discovery |
| Distribution | PyPI/uvx | NuGet + MCP Registry |

---

## Installation

### Method 1: Global Tool (Recommended)

```bash
dotnet tool install -g UniversalVSMCP
```

After installation, run:

```bash
universal-vsmcp --stdio
```

### Method 2: From Source

```bash
git clone https://github.com/StarsailsClover/UniversalVSMCP.git
cd UniversalVSMCP/src/UniversalVSMCP
dotnet run -- --stdio
```

### Method 3: Via NuGet (Future)

```bash
dotnet tool install -g UniversalVSMCP --version 1.0.0
```

---

## Visual Studio 2026 Configuration (3 Methods)

### Method A: MCP Registry (Recommended)

1. Open **Visual Studio 2026**
2. **Tools** -> **Options** -> **Environment** -> **Extensions**
3. In **MCP Registry** section, click **Add**
4. Enter:
   - **Name**: `UniversalVSMCP Official`
   - **URL**: `https://github.com/StarsailsClover/UniversalVSMCP`
5. VS scans `server.json` and lists available servers
6. Select `universal-vsmcp` and click **Install**
7. **Restart Visual Studio** if prompted

### Method B: Manual MCP Server

1. **Tools** -> **Options** -> **Environment** -> **Extensions**
2. In **MCP Server List**, click **Add**
3. Fill in:
   - **Name**: `universal-vsmcp`
   - **Command**: `dotnet`
   - **Args**: `tool run --global universal-vsmcp -- --stdio`
4. Save and restart VS

### Method C: Direct JSON Edit

Check **"Edit user settings as JSON"** and paste:

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
      },
      "transport": "stdio"
    }
  }
}
```

---

## Available Tools

### Solution Management
| Tool | Description |
|------|-------------|
| `get_solution_projects` | Get all projects in solution |
| `get_solution_path` | Get solution file path |
| `get_solution_name` | Get solution name |
| `open_solution` | Open .sln file |
| `close_solution` | Close current solution |

### Project Management
| Tool | Description |
|------|-------------|
| `get_project_files` | Get files in a project |
| `get_startup_projects` | Get startup project(s) |
| `add_file_to_project` | Add file to project |
| `set_startup_project` | Set startup project |
| `get_project_properties` | Get project properties |

### File Operations
| Tool | Description |
|------|-------------|
| `open_file` | Open file in VS editor |
| `read_file` | Read file content |
| `write_file` | Write file content |
| `replace_in_file` | Replace text in file |
| `get_file_info` | Get file information |

### Build Operations
| Tool | Description |
|------|-------------|
| `build_solution` | Build solution |
| `rebuild_solution` | Rebuild solution |
| `clean_solution` | Clean solution |
| `build_project` | Build specific project |
| `get_build_errors` | Get build errors |
| `get_build_configurations` | Get build configs |

### Debug Control
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

---

## Project Structure

```
UniversalVSMCP/
├── server.json              # MCP Registry config (VS 2026 discovery)
├── REGISTRY.md              # Registry setup guide
├── README.md                # English documentation (default)
├── README.zh.md             # Chinese documentation
├── Link2VS.skill/          # AI Agent skill package
├── pyproject.toml           # Python package config (legacy)
├── requirements.txt         # Python dependencies (legacy)
├── config_templates.py      # VS config templates (legacy)
├── tests/
│   └── test_core.py         # Python tests (legacy)
└── src/UniversalVSMCP/
    ├── UniversalVSMCP.csproj
    ├── Program.cs           # Entry point + MCP Server config
    ├── VsConnectionManager.cs  # VS DTE connection management
    ├── SolutionTools.cs     # Solution operations
    ├── ProjectTools.cs      # Project management
    ├── FileTools.cs         # File operations
    ├── BuildTools.cs        # Build operations
    └── DebugTools.cs        # Debug control
```

---

## AI Agent Integration

### Claude Desktop / Cursor / Cline

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

## Architecture

```
┌─────────────┐     MCP Protocol     ┌──────────────────┐     DTE COM      ┌─────────────┐
│   AI Agent   │◄────────────────────►│  UniversalVSMCP  │◄────────────────►│  VS 2026/22  │
│  (Claude)   │   (JSON-RPC over     │   (.NET 8.0)     │   (Windows COM)  │   Instance   │
└─────────────┘     stdio/SSE)       └──────────────────┘                    └─────────────┘
```

### Tech Stack

- **Runtime**: .NET 8.0
- **MCP SDK**: Microsoft.ModelContextProtocol (0.3.0-preview)
- **VS Automation**: EnvDTE / EnvDTE80
- **Transport**: stdio (local), SSE (remote)
- **Deployment**: Global Tool, NuGet, MCP Registry

---

## Requirements

- .NET 8.0 SDK or later
- Visual Studio 2026 (18.0) or VS 2022 (17.14+)
- Windows 10 or later

---

## Troubleshooting

### MCP Registry doesn't show the server

1. Verify `server.json` is valid JSON at repository root
2. Ensure repository is public
3. Restart Visual Studio after adding registry
4. Use Method B (Manual) if Registry feature is unstable

### Server fails to start

1. Ensure .NET 8.0 SDK is installed: `dotnet --version`
2. Install as global tool: `dotnet tool install -g UniversalVSMCP`
3. Test manually: `universal-vsmcp --stdio`

### DTE connection fails

1. Start Visual Studio before running the server
2. Open at least one solution
3. Check that VS is not running as Administrator (mismatch with user token)

---

## License

MIT © StarsailsClover

---

## Links

- [MCP Documentation](https://modelcontextprotocol.io/)
- [Microsoft MCP .NET Samples](https://github.com/microsoft/mcp-dotnet-samples)
- [Visual Studio 2026](https://learn.microsoft.com/en-us/visualstudio/)
- [GitHub Repository](https://github.com/StarsailsClover/UniversalVSMCP)
- [NuGet Package](https://www.nuget.org/packages/UniversalVSMCP/)
