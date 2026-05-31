"""
VS 2026 MCP Configuration Templates
Copy the JSON below into Visual Studio's .mcp.json configuration file
"""

# ============================================================
# User-level configuration (recommended)
# Path: %APPDATA%\VisualStudio\18.0\mcp.json
# ============================================================

USER_LEVEL_CONFIG = """{
  "mcpServers": {
    "universal-vsmcp": {
      "command": "uvx",
      "args": [
        "git+https://github.com/StarsailsClover/UniversalVSMCP[mcp-server]",
        "mcp-server",
        "--stdio"
      ],
      "env": {
        "VS_AUTO_DETECT": "true",
        "PYTHONIOENCODING": "utf-8"
      },
      "transport": "stdio"
    }
  }
}"""

# ============================================================
# Solution-level configuration
# Path: <solution-root>/.mcp.json
# ============================================================

SOLUTION_LEVEL_CONFIG = """{
  "mcpServers": {
    "vsmcp": {
      "command": "uvx",
      "args": [
        "--from",
        "git+https://github.com/StarsailsClover/UniversalVSMCP[mcp-server]",
        "UniversalVSMCP"
      ],
      "env": {
        "VS_SOLUTION_PATH": "${workspaceFolder}",
        "PYTHONIOENCODING": "utf-8"
      },
      "transport": "stdio"
    }
  }
}"""

# ============================================================
# .NET version configuration (when .NET SDK is published)
# ============================================================

DOTNET_CONFIG = """{
  "mcpServers": {
    "vsmcp-dotnet": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\UniversalVSMCP\\src\\UniversalVSMCP\\UniversalVSMCP.csproj",
        "--",
        "--stdio"
      ],
      "transport": "stdio"
    }
  }
}"""

# ============================================================
# Multi-server configuration (using multiple MCP servers simultaneously)
# ============================================================

MULTI_SERVER_CONFIG = """{
  "mcpServers": {
    "universal-vsmcp": {
      "command": "uvx",
      "args": [
        "git+https://github.com/StarsailsClover/UniversalVSMCP[mcp-server]",
        "mcp-server",
        "--stdio"
      ],
      "env": {
        "VS_AUTO_DETECT": "true"
      }
    },
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\Users\\Sails\\source",
        "C:\\Users\\Sails\\Documents"
      ]
    },
    "github": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-github"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "<your-token>"
      }
    }
  }
}"""

# ============================================================
# VS 2026 internal configuration steps
# ============================================================

VS2026_SETUP_STEPS = """
1. Open Visual Studio 2026
2. Open or create a solution (.sln)
3. Press Ctrl+Q, type "MCP: Open User Configuration"
4. Paste the JSON from 'mcp_config.vs_mcp_json_templates.user_level.template'
5. Save the file
6. Restart VS if needed, then enable the MCP server in the MCP panel
"""

# ============================================================
# Claude Desktop / Cursor configuration (external AI Agent)
# ============================================================

CLAUDE_DESKTOP_CONFIG = """
Windows path:
%APPDATA%\\Claude\\claude_desktop_config.json

macOS path:
~/Library/Application Support/Claude/claude_desktop_config.json

Configuration:
{
  "mcpServers": {
    "vsmcp": {
      "command": "uvx",
      "args": [
        "git+https://github.com/StarsailsClover/UniversalVSMCP[mcp-server]",
        "mcp-server",
        "--stdio"
      ],
      "env": {
        "VS_AUTO_DETECT": "true"
      }
    }
  }
}
"""

if __name__ == "__main__":
    print("=== UniversalVSMCP Configuration Templates ===\n")
    print("1. User-level configuration (recommended):")
    print(USER_LEVEL_CONFIG)
    print("\n2. Solution-level configuration:")
    print(SOLUTION_LEVEL_CONFIG)
    print("\n3. VS 2026 setup steps:")
    print(VS2026_SETUP_STEPS)
