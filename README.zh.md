# UniversalVSMCP (UVM)
## Visual Studio 2026/2022 的 .NET 原生 MCP 服务器

**让 AI Agent 通过 Model Context Protocol (MCP) 原生控制 Visual Studio 2026/2022。**

---

## v1.0.0 新特性

- **.NET 8.0 原生实现** - 无需 Python 或 Node.js 依赖
- **28 个 MCP 工具** - 全面的 VS 自动化（解决方案、项目、文件、构建、调试）
- **MCP Registry 兼容** - 在 VS 2026 中一键安装
- **全局 .NET 工具** - `dotnet tool install -g UniversalVSMCP`
- **原生 DTE/COM 集成** - 直接通过 Windows COM 与 Visual Studio 交互

---

## 为什么选择 .NET 原生方案？

| 特性 | Python (旧版) | .NET 原生 (当前版) |
|------|--------------|------------------|
| VS 2026 集成 | 需要手动配置 | 可从 MCP Registry 一键安装 |
| 运行时依赖 | 需要 Python + uvx | 仅需 .NET 8.0 SDK |
| DTE 自动化 | 通过 pywin32 | 原生 COM 互操作 |
| 性能 | 中等 | 最优 |
| VS 2026 发现 | 不支持 | 自动发现 |
| 发布方式 | PyPI/uvx | NuGet + MCP Registry |

---

## 安装方式

### 方式一：全局工具（推荐）

```bash
dotnet tool install -g UniversalVSMCP
```

安装后运行：

```bash
universal-vsmcp --stdio
```

### 方式二：从源码运行

```bash
git clone https://github.com/StarsailsClover/UniversalVSMCP.git
cd UniversalVSMCP/src/UniversalVSMCP
dotnet run -- --stdio
```

### 方式三：通过 NuGet（未来）

```bash
dotnet tool install -g UniversalVSMCP --version 1.0.0
```

---

## Visual Studio 2026 配置（3 种方式）

### 方式 A：MCP Registry（推荐）

1. 打开 **Visual Studio 2026**
2. **工具** -> **选项** -> **环境** -> **扩展**
3. 在 **MCP Registry** 区域点击 **添加**
4. 输入：
   - **名称**: `UniversalVSMCP Official`
   - **URL**: `https://github.com/StarsailsClover/UniversalVSMCP`
5. VS 会扫描 `server.json` 并列出可用服务器
6. 选择 `universal-vsmcp` 并点击 **安装**
7. 如需要请重启 Visual Studio

### 方式 B：手动添加 MCP 服务器

1. **工具** -> **选项** -> **环境** -> **扩展**
2. 在 **MCP 服务器列表** 点击 **添加**
3. 填写：
   - **名称**: `universal-vsmcp`
   - **命令**: `dotnet`
   - **参数**: `tool run --global universal-vsmcp -- --stdio`
4. 保存并重启 VS

### 方式 C：直接编辑 JSON

勾选 **"将用户设置编辑为 JSON"**，粘贴：

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

## 可用工具

### 解决方案管理
| 工具 | 描述 |
|------|------|
| `get_solution_projects` | 获取解决方案中所有项目 |
| `get_solution_path` | 获取当前解决方案路径 |
| `get_solution_name` | 获取当前解决方案名称 |
| `open_solution` | 打开 .sln 文件 |
| `close_solution` | 关闭当前解决方案 |

### 项目管理
| 工具 | 描述 |
|------|------|
| `get_project_files` | 获取项目中的文件 |
| `get_startup_projects` | 获取启动项目 |
| `add_file_to_project` | 添加文件到项目 |
| `set_startup_project` | 设置启动项目 |
| `get_project_properties` | 获取项目属性 |

### 文件操作
| 工具 | 描述 |
|------|------|
| `open_file` | 在 VS 编辑器中打开文件 |
| `read_file` | 读取文件内容 |
| `write_file` | 写入文件内容 |
| `replace_in_file` | 替换文件中的文本 |
| `get_file_info` | 获取文件信息 |

### 构建操作
| 工具 | 描述 |
|------|------|
| `build_solution` | 构建解决方案 |
| `rebuild_solution` | 重新构建解决方案 |
| `clean_solution` | 清理解决方案 |
| `build_project` | 构建指定项目 |
| `get_build_errors` | 获取构建错误 |
| `get_build_configurations` | 获取构建配置 |

### 调试控制
| 工具 | 描述 |
|------|------|
| `start_debugging` | 开始调试 (F5) |
| `stop_debugging` | 停止调试 (Shift+F5) |
| `toggle_breakpoint` | 导航到断点行 |
| `continue_execution` | 继续执行 (F5) |
| `step_over` | 单步跳过 (F10) |
| `step_into` | 单步进入 (F11) |
| `step_out` | 单步跳出 (Shift+F11) |
| `get_debug_state` | 获取调试状态 |

---

## 项目结构

```
UniversalVSMCP/
├── server.json              # MCP Registry 配置（VS 2026 发现）
├── REGISTRY.md              # Registry 注册指南
├── README.md                # English documentation (default)
├── README.zh.md             # 中文文档
├── Link2VS.skill/          # AI Agent 技能包
├── pyproject.toml           # Python 包配置（遗留）
├── requirements.txt         # Python 依赖（遗留）
├── config_templates.py      # VS 配置模板（遗留）
├── tests/
│   └── test_core.py         # Python 测试（遗留）
└── src/UniversalVSMCP/
    ├── UniversalVSMCP.csproj
    ├── Program.cs           # 入口点 + MCP Server 配置
    ├── VsConnectionManager.cs  # VS DTE 连接管理
    ├── SolutionTools.cs     # 解决方案操作
    ├── ProjectTools.cs      # 项目管理
    ├── FileTools.cs         # 文件操作
    ├── BuildTools.cs        # 构建操作
    └── DebugTools.cs        # 调试控制
```

---

## AI Agent 集成

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

## 技术架构

```
┌─────────────┐     MCP 协议      ┌──────────────────┐     DTE COM      ┌─────────────┐
│   AI Agent   │◄────────────────►│  UniversalVSMCP  │◄───────────────►│  VS 2026/22  │
│  (Claude)   │   (JSON-RPC)     │   (.NET 8.0)     │   (Windows COM)  │   Instance   │
└─────────────┘                  └──────────────────┘                    └─────────────┘
```

### 技术栈

- **Runtime**: .NET 8.0
- **MCP SDK**: Microsoft.ModelContextProtocol (0.3.0-preview)
- **VS Automation**: EnvDTE / EnvDTE80
- **Transport**: stdio (本地), SSE (远程)
- **Deployment**: Global Tool, NuGet, MCP Registry

---

## 系统要求

- .NET 8.0 SDK 或更高版本
- Visual Studio 2026 (18.0) 或 VS 2022 (17.14+)
- Windows 10 或更高版本

---

## 故障排除

### MCP Registry 不显示服务器

1. 确认 `server.json` 在仓库根目录且为有效 JSON
2. 确保仓库是公开的
3. 添加 Registry 后重启 Visual Studio
4. 如果 Registry 功能不稳定，使用方法 B（手动配置）

### 服务器无法启动

1. 确认 .NET 8.0 SDK 已安装：`dotnet --version`
2. 安装全局工具：`dotnet tool install -g UniversalVSMCP`
3. 手动测试：`universal-vsmcp --stdio`

### DTE 连接失败

1. 运行服务器前先启动 Visual Studio
2. 至少打开一个解决方案
3. 确认 VS 不是以管理员身份运行（与用户令牌不匹配）

---

## 许可

MIT © StarsailsClover

---

## 相关链接

- [MCP 官方文档](https://modelcontextprotocol.io/)
- [Microsoft MCP .NET Samples](https://github.com/microsoft/mcp-dotnet-samples)
- [Visual Studio 2026](https://learn.microsoft.com/en-us/visualstudio/)
- [GitHub 仓库](https://github.com/StarsailsClover/UniversalVSMCP)
- [NuGet 包](https://www.nuget.org/packages/UniversalVSMCP/)
