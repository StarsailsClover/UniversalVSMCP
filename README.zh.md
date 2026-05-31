# UniversalVSMCP (UVM)
## Visual Studio 2026/2022 的 .NET 原生 MCP 服务器

**让 AI Agent 通过 Model Context Protocol (MCP) 原生控制 Visual Studio 2026/2022。**

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
2. **工具** → **选项** → **环境** → **扩展**
3. 在 **MCP Registry** 区域点击 **添加**
4. 输入：
   - **名称**: `UniversalVSMCP Official`
   - **URL**: `https://github.com/StarsailsClover/UniversalVSMCP`
5. VS 会扫描 `server.json` 并列出可用服务器
6. 选择 `universal-vsmcp` 并点击 **安装**

### 方式 B：手动添加 MCP 服务器

1. **工具** → **选项** → **环境** → **扩展**
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

| 工具 | 描述 | 类别 |
|------|------|------|
| `get_solution_projects` | 获取解决方案中所有项目 | Solution |
| `get_solution_path` | 获取当前解决方案路径 | Solution |
| `get_solution_name` | 获取当前解决方案名称 | Solution |
| `open_solution` | 打开 .sln 文件 | Solution |
| `close_solution` | 关闭当前解决方案 | Solution |
| `open_file` | 在 VS 编辑器中打开文件 | File |
| `read_file` | 读取文件内容 | File |
| `write_file` | 写入文件内容 | File |
| `build_solution` | 构建解决方案 | Build |
| `build_project` | 构建指定项目 | Build |
| `get_build_errors` | 获取构建错误 | Build |
| `get_build_configurations` | 获取可用构建配置 | Build |
| `start_debugging` | 开始调试 (F5) | Debug |
| `stop_debugging` | 停止调试 (Shift+F5) | Debug |
| `toggle_breakpoint` | 切换断点 | Debug |
| `step_over` | 单步跳过 (F10) | Debug |
| `step_into` | 单步进入 (F11) | Debug |
| `step_out` | 单步跳出 (Shift+F11) | Debug |
| `get_debug_state` | 获取当前调试状态 | Debug |

---

## 项目结构

```
UniversalVSMCP/
├── server.json              # MCP Registry 配置（VS 2026 发现）
├── REGISTRY.md              # Registry 注册指南
├── README.md                # English documentation
├── README.zh.md             # 中文文档
├── Link2VS.skill/          # AI Agent 技能包
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

## 许可

MIT © StarsailsClover

---

## 相关链接

- [MCP 官方文档](https://modelcontextprotocol.io/)
- [Microsoft MCP .NET Samples](https://github.com/microsoft/mcp-dotnet-samples)
- [Visual Studio 2026](https://learn.microsoft.com/en-us/visualstudio/)
- [GitHub 仓库](https://github.com/StarsailsClover/UniversalVSMCP)
