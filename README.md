# UniversalVSMCP (UVM)
## .NET Native MCP Server for Visual Studio 2026/2022

**Enable AI Agents to natively control Visual Studio 2026/2022 via the Model Context Protocol (MCP).**

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
2. **Tools** → **Options** → **Environment** → **Extensions**
3. In **MCP Registry** section, click **Add**
4. Enter:
   - **Name**: `UniversalVSMCP Official`
   - **URL**: `https://github.com/StarsailsClover/UniversalVSMCP`
5. VS scans `server.json` and lists available servers
6. Select `universal-vsmcp` and click **Install**

### Method B: Manual MCP Server

1. **Tools** → **Options** → **Environment** → **Extensions**
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

| Tool | Description | Category |
|------|-------------|----------|
| `get_solution_projects` | Get all projects in solution | Solution |
| `get_solution_path` | Get solution file path | Solution |
| `get_solution_name` | Get solution name | Solution |
| `open_solution` | Open .sln file | Solution |
| `close_solution` | Close current solution | Solution |
| `open_file` | Open file in VS editor | File |
| `read_file` | Read file content | File |
| `write_file` | Write file content | File |
| `build_solution` | Build solution | Build |
| `build_project` | Build specific project | Build |
| `get_build_errors` | Get build errors | Build |
| `get_build_configurations` | Get available build configs | Build |
| `start_debugging` | Start debugging (F5) | Debug |
| `stop_debugging` | Stop debugging (Shift+F5) | Debug |
| `toggle_breakpoint` | Toggle breakpoint | Debug |
| `step_over` | Step over (F10) | Debug |
| `step_into` | Step into (F11) | Debug |
| `step_out` | Step out (Shift+F11) | Debug |
| `get_debug_state` | Get current debug state | Debug |

---

## Project Structure

```
UniversalVSMCP/
├── server.json              # MCP Registry config (VS 2026 discovery)
├── REGISTRY.md              # Registry setup guide
├── README.md                # English documentation
├── README.zh.md             # Chinese documentation
├── Link2VS.skill/          # AI Agent skill package
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

## License

MIT © StarsailsClover

---

## Links

- [MCP Documentation](https://modelcontextprotocol.io/)
- [Microsoft MCP .NET Samples](https://github.com/microsoft/mcp-dotnet-samples)
- [Visual Studio 2026](https://learn.microsoft.com/en-us/visualstudio/)
- [GitHub Repository](https://github.com/StarsailsClover/UniversalVSMCP)
