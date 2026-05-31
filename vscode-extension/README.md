# VS Code Extension for UniversalVSMCP

## Overview

This VS Code Extension connects to UniversalVSMCP, allowing you to control Visual Studio 2026/2022 directly from VS Code.

## Installation

### From VSIX
1. Download `universal-vsmcp-26.0.0.vsix`
2. Open VS Code
3. Extensions → `...` → "Install from VSIX"
4. Select the downloaded file

### From Marketplace
Search for "Universal VS MCP" in the VS Code marketplace.

## Quick Start

1. Ensure UniversalVSMCP is installed:
   ```bash
   dotnet tool install -g UniversalVSMCP
   ```

2. Open VS Code with the extension installed

3. Press `Ctrl+Shift+P` → "UVM: Connect to VS"

4. Use commands:
   - `Ctrl+Shift+B` - Build Solution
   - `F5` - Start Debugging

## Features

- 🔗 **Connect to VS** - Control Visual Studio from VS Code
- 🔨 **Build & Debug** - Build solutions and start debugging
- 📁 **Solution Explorer** - Browse VS solution structure
- 🔍 **Find in Solution** - Search across the entire solution
- 🛡️ **Security** - Workspace trust and user confirmation

## Commands

| Command | Keybinding | Description |
|---------|------------|-------------|
| UVM: Connect to VS | - | Connect to UVM server |
| UVM: Build Solution | `Ctrl+Shift+B` | Build current solution |
| UVM: Start Debugging | `F5` | Start debugging in VS |
| UVM: Find in Solution | - | Search in solution |
| UVM: Get Solution Info | - | Show solution details |

## Configuration

Open VS Code settings (`Ctrl+,`) and search for "UVM":

```json
{
  "uvm.serverPath": "universal-vsmcp",
  "uvm.transport": "stdio",
  "uvm.autoConnect": false,
  "uvm.security.alwaysConfirmBuild": false
}
```

## Development

See [vsc-extension-quickstart.md](vsc-extension-quickstart.md) for development guide.

## Links

- [UniversalVSMCP](https://github.com/StarsailsClover/UniversalVSMCP)
- [Issues](https://github.com/StarsailsClover/UniversalVSMCP/issues)
