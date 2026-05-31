# VS Code Extension for UniversalVSMCP

## Overview

This document outlines the VS Code Extension (VSC) architecture for UniversalVSMCP, bringing Visual Studio automation capabilities to VS Code users.

## Architecture

```
VS Code Extension (VSC)
├── Extension Host (TypeScript/Node.js)
│   ├── Activation
│   ├── Command Registration
│   └── Status Bar Integration
├── MCP Client
│   ├── Connection to UVM (stdio/http)
│   ├── Tool Discovery
│   └── Request Routing
├── Security Layer
│   ├── Workspace Trust
│   ├── User Confirmation UI
│   └── Audit Logging
└── UI Components
    ├── Output Channel
    ├── Tree View (Solution Explorer)
    ├── Webview (Build/Output)
    └── Quick Pick (Confirmation)
```

## Features

### 1. Solution Explorer Panel

```typescript
// Tree data provider for solution/projects
class SolutionTreeProvider implements vscode.TreeDataProvider<SolutionItem>
{
    async getChildren(element?: SolutionItem): Promise<SolutionItem[]> {
        // Query UVM for solution structure
        const result = await this.mcpClient.callTool('get_solution_projects', {});
        return this.parseProjects(result);
    }
}
```

### 2. Integrated Build Output

```typescript
// Output channel for build results
const buildOutput = vscode.window.createOutputChannel('Universal VS MCP');

// Hook into UVM build events
this.mcpClient.onBuildEvent(event => {
    buildOutput.appendLine(event.message);
});
```

### 3. Debug Session Integration

```typescript
// Start debugging from VS Code
vscode.commands.registerCommand('uvm.startDebugging', async () => {
    await this.mcpClient.callTool('start_debugging', {});
    vscode.debug.startDebugging(undefined, 'uvm');
});
```

### 4. User Confirmation UI

```typescript
// Show confirmation dialog for sensitive operations
async showConfirmation(operation: string, target: string): Promise<boolean> {
    const result = await vscode.window.showWarningMessage(
        `${operation}: ${target}`,
        { modal: true, detail: 'This operation will modify files. Continue?' },
        'Confirm',
        'Cancel'
    );
    return result === 'Confirm';
}
```

## Security Integration

### Workspace Trust

```typescript
// Check workspace trust before operations
if (!vscode.workspace.isTrusted) {
    const result = await vscode.window.showInformationMessage(
        'This workspace is not trusted. UniversalVSMCP requires trusted workspace for file operations.',
        'Trust Workspace',
        'Cancel'
    );
    if (result === 'Trust Workspace') {
        vscode.commands.executeCommand('workbench.trust.manage');
    }
    return;
}
```

### Permission Prompts

```typescript
// Configuration for permission levels
interface SecurityConfig {
    autoConfirmRead: boolean;
    autoConfirmWriteInTrusted: boolean;
    alwaysConfirmDelete: boolean;
    alwaysConfirmBuild: boolean;
}
```

## Implementation Plan

### Phase 1: Core Extension
- [ ] Extension scaffold
- [ ] MCP client integration
- [ ] Basic command palette commands
- [ ] Output channel

### Phase 2: UI Components
- [ ] Solution explorer tree view
- [ ] Status bar indicator
- [ ] Build output panel
- [ ] Quick pick for operations

### Phase 3: Security
- [ ] Workspace trust integration
- [ ] Confirmation dialogs
- [ ] Audit log viewer
- [ ] Permission settings

### Phase 4: Advanced Features
- [ ] Debug adapter protocol integration
- [ ] Code lens for breakpoints
- [ ] Hover provider for symbols
- [ ] Code actions (refactoring)

## Extension Manifest

```json
{
  "name": "universal-vsmcp-vscode",
  "displayName": "Universal VS MCP",
  "description": "Visual Studio automation in VS Code",
  "version": "26.0.0",
  "publisher": "StarsailsClover",
  "engines": {
    "vscode": "^1.90.0"
  },
  "categories": ["Debuggers", "Machine Learning", "Other"],
  "activationEvents": [
    "onCommand:uvm.connect",
    "workspaceContains:**/*.sln"
  ],
  "main": "./out/extension.js",
  "contributes": {
    "commands": [
      {
        "command": "uvm.connect",
        "title": "Connect to VS",
        "category": "UVM"
      },
      {
        "command": "uvm.build",
        "title": "Build Solution",
        "category": "UVM"
      },
      {
        "command": "uvm.debug",
        "title": "Start Debugging",
        "category": "UVM"
      }
    ],
    "views": {
      "explorer": [
        {
          "id": "uvm.solutionExplorer",
          "name": "VS Solution"
        }
      ]
    },
    "configuration": {
      "title": "Universal VS MCP",
      "properties": {
        "uvm.serverPath": {
          "type": "string",
          "default": "universal-vsmcp",
          "description": "Path to UVM server executable"
        },
        "uvm.transport": {
          "type": "string",
          "enum": ["stdio", "http"],
          "default": "stdio",
          "description": "Transport mode"
        },
        "uvm.security.autoConfirmRead": {
          "type": "boolean",
          "default": true,
          "description": "Auto-confirm read operations in trusted workspaces"
        },
        "uvm.security.alwaysConfirmDelete": {
          "type": "boolean",
          "default": true,
          "description": "Always confirm delete operations"
        }
      }
    }
  }
}
```

## Communication Protocol

```typescript
// Message protocol between VS Code and UVM
interface MCPMessage {
    jsonrpc: "2.0";
    id?: number | string;
    method?: string;
    params?: any;
    result?: any;
    error?: {
        code: number;
        message: string;
        data?: any;
    };
}

// Tool call request
interface ToolCallRequest {
    name: string;
    arguments: Record<string, any>;
}

// Tool call response
interface ToolCallResponse {
    content: Array<{
        type: "text" | "image";
        text?: string;
        data?: string;
    }>;
    isError?: boolean;
}
```

## Development Setup

```bash
# Clone extension repo
git clone https://github.com/StarsailsClover/vscode-universal-vsmcp.git

# Install dependencies
cd vscode-universal-vsmcp
npm install

# Build
npm run compile

# Launch extension host
F5  # In VS Code

# Test
npm test
```

## Publishing

```bash
# Package extension
vsce package

# Publish to marketplace
vsce publish

# Publish to Open VSX
npx ovsx publish
```

---

**Note**: VSC in this context refers to VS Code Extension, not Visual Studio Connector.
