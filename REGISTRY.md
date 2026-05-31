# UniversalVSMCP - MCP Registry Registration Guide

This document explains how to register UniversalVSMCP with the GitHub MCP Registry and make it discoverable in Visual Studio 2026.

---

## 📋 Prerequisites for Registry

- GitHub repository: `StarsailsClover/UniversalVSMCP`
- `server.json` file at repository root ✅ (already created)
- GitHub release with tagged version
- NuGet package (optional but recommended)

---

## 🔗 Step 1: Ensure server.json is Correct

The `server.json` file is already created at the root of this repository with:

```json
{
  "name": "universal-vsmcp",
  "version": "1.0.0",
  "mcpServers": { ... },
  "tools": [ ... ],
  "requirements": { ... }
}
```

**Key fields for VS 2026 discovery:**
- `name`: Server identifier
- `version`: Semantic version
- `mcpServers`: Configuration templates
- `tools`: Tool definitions for AI agents
- `requirements`: Platform requirements

---

## 📦 Step 2: Publish to NuGet

### 2.1 Create NuGet Package

```bash
cd src/UniversalVSMCP
dotnet pack -c Release -o ./nupkg
```

This creates: `UniversalVSMCP.1.0.0.nupkg`

### 2.2 Publish to NuGet.org

```bash
dotnet nuget push ./nupkg/UniversalVSMCP.1.0.0.nupkg \
  --api-key YOUR_NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### 2.3 Verify Package

Visit: https://www.nuget.org/packages/UniversalVSMCP/

---

## 🏷️ Step 3: Create GitHub Release

### 3.1 Tag the Release

```bash
git tag -a v1.0.0 -m "Release v1.0.0: Initial MCP Server for VS 2026/2022"
git push origin v1.0.0
```

### 3.2 Create GitHub Release

Go to: https://github.com/StarsailsClover/UniversalVSMCP/releases/new

- **Tag**: `v1.0.0`
- **Title**: `UniversalVSMCP v1.0.0 - VS 2026/2022 MCP Server`
- **Description**:

```markdown
## 🚀 UniversalVSMCP v1.0.0

MCP Server for Visual Studio 2026/2022 automation.

### Installation

**Global Tool:**
```bash
dotnet tool install -g UniversalVSMCP
```

**Or via NuGet:**
```bash
dotnet tool install -g UniversalVSMCP --version 1.0.0
```

### VS 2026 Configuration

1. Open VS 2026 → Tools → Options → Environment → Extensions
2. In MCP Server List, click Add
3. Enter:
   - **Name**: `universal-vsmcp`
   - **Command**: `dotnet`
   - **Args**: `tool run --global universal-vsmcp -- --stdio`
4. Save and restart VS

### Available Tools

- `get_solution_projects` - List all projects in solution
- `get_solution_path` - Get solution file path
- `open_file` - Open file in VS editor
- `read_file` - Read file content
- `write_file` - Write file content
- `build_solution` - Build solution
- `build_project` - Build specific project
- `start_debugging` - Start debugging (F5)
- `stop_debugging` - Stop debugging
- `toggle_breakpoint` - Set breakpoints

### Requirements

- .NET 8.0+
- Visual Studio 2026 (18.0) or VS 2022 (17.14+)
- Windows 10+

---

**Full Documentation**: https://github.com/StarsailsClover/UniversalVSMCP#readme
```

---

## 🔍 Step 4: Register with MCP Registry (Optional)

### 4.1 Docker MCP Community Registry

The Docker MCP Community Registry at `github.com/docker/mcp-community-registry` accepts server submissions.

**Submission Process:**

1. Fork the registry repository:
   ```bash
   git clone https://github.com/docker/mcp-community-registry.git
   cd mcp-community-registry
   ```

2. Create a new server entry in `registry/servers/`:
   ```bash
   mkdir -p registry/servers/universal-vsmcp
   ```

3. Create `registry/servers/universal-vsmcp/server.json`:
   ```json
   {
     "name": "universal-vsmcp",
     "version": "1.0.0",
     "description": "MCP Server for Visual Studio 2026/2022",
     "repository": "https://github.com/StarsailsClover/UniversalVSMCP",
     "command": "dotnet",
     "args": ["tool", "run", "--global", "universal-vsmcp", "--", "--stdio"],
     "transport": "stdio"
   }
   ```

4. Create a pull request with your changes.

### 4.2 Microsoft MCP Registry (if available)

Check if Microsoft has a separate registry for VS-integrated MCP servers:
- https://github.com/microsoft/mcp-dotnet-samples
- https://learn.microsoft.com/en-us/visualstudio/extensibility/mcp-servers

---

## 🔧 Step 5: VS 2026 Direct Integration (Alternative)

VS 2026 can also discover MCP servers directly from GitHub if the repository:

1. Contains a valid `server.json` at root ✅
2. Has GitHub releases with the server package
3. Is referenced in VS configuration

### VS 2026 Registry Configuration

In VS 2026, the MCP Server Manager has a "MCP Registry" section (as shown in your screenshot). To add the UniversalVSMCP registry:

1. In VS 2026: **Tools → Options → Environment → Extensions**
2. In **MCP Registry** section, click **Add**
3. Enter:
   - **Name**: `UniversalVSMCP Official`
   - **URL**: `https://github.com/StarsailsClover/UniversalVSMCP`
4. Save
5. VS will scan the repository for `server.json` and list available servers

---

## ✅ Verification Checklist

- [ ] `server.json` exists at repository root
- [ ] GitHub release created with tag `v1.0.0`
- [ ] NuGet package published
- [ ] VS 2026 can discover server via MCP Registry
- [ ] Server installs and runs via `dotnet tool run universal-vsmcp`
- [ ] Tools appear in VS MCP Server Manager

---

## 🐛 Troubleshooting

### VS 2026 doesn't show the server in Registry

1. Verify `server.json` is valid JSON
2. Ensure repository is public
3. Check that GitHub release exists
4. Try adding registry URL directly in VS settings

### Server fails to start

1. Ensure .NET 8.0 SDK is installed
2. Install as global tool: `dotnet tool install -g UniversalVSMCP`
3. Test manually: `universal-vsmcp --stdio`

### DTE connection fails

1. Start Visual Studio before running the server
2. Open at least one solution
3. Ensure VS is registered in Running Object Table (ROT)
