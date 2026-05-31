"""
VS 2026 MCP 配置示例
将以下内容复制到 Visual Studio 的 .mcp.json 配置文件中
"""

# ============================================================
# 用户级配置（推荐）
# 路径：%APPDATA%\VisualStudio\18.0\mcp.json
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
# 方案级配置
# 路径：<solution-root>/.mcp.json
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
# .NET 版本配置（当 .NET SDK 发布后）
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
# 多服务器配置（同时使用多个 MCP 服务器）
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
# VS 2026 内配置步骤
# ============================================================

VS2026_SETUP_STEPS = """
1. 打开 Visual Studio 2026
2. 打开或创建一个解决方案 (.sln)
3. 按下 Ctrl+Q，输入 "MCP: Open User Configuration"
4. 在打开的 mcp.json 文件中粘贴配置
5. 保存文件
6. 在 VS 的 MCP 面板中找到 "universal-vsmcp" 并启用
7. 重启 Visual Studio（如需要）
"""

# ============================================================
# Claude Desktop / Cursor 配置（外部 AI Agent）
# ============================================================

CLAUDE_DESKTOP_CONFIG = """
Windows 路径：
%APPDATA%\\Claude\\claude_desktop_config.json

macOS 路径：
~/Library/Application Support/Claude/claude_desktop_config.json

配置内容：
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
    print("=== UniversalVSMCP 配置模板 ===\n")
    print("1. 用户级配置（推荐）：")
    print(USER_LEVEL_CONFIG)
    print("\n2. 方案级配置：")
    print(SOLUTION_LEVEL_CONFIG)
    print("\n3. VS 2026 设置步骤：")
    print(VS2026_SETUP_STEPS)
