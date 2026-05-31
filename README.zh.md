# UniversalVSMCP (UVM)
## Visual Studio 2026/2022 的 .NET 原生 MCP 服务器

**让 AI Agent 通过 Model Context Protocol (MCP) 原生控制 Visual Studio 2026/2022。**

---

## 安装方式（单一推荐方式）

### NuGet 包（推荐）

```bash
dotnet tool install -g UniversalVSMCP
```

运行：

```bash
universal-vsmcp --stdio
```

验证安装：

```bash
universal-vsmcp --help
```

**NuGet 包：** https://www.nuget.org/packages/UniversalVSMCP/

---

## 快速开始

### 1. 启动 Visual Studio 2026
打开任意解决方案或创建新项目。

### 2. 运行 MCP 服务器
```bash
universal-vsmcp --stdio
```

### 3. 在 VS 2026 中配置
转到 **工具 → 选项 → 环境 → 扩展**：
- 添加 MCP 服务器：`universal-vsmcp`
- 命令：`dotnet tool run --global universal-vsmcp -- --stdio`

---

## 可用工具（共 30 个）

### 解决方案管理（6个）
| 工具 | 描述 |
|------|------|
| `get_solution_projects` | 获取解决方案中所有项目 |
| `get_solution_path` | 获取当前解决方案路径 |
| `get_solution_name` | 获取当前解决方案名称 |
| `open_solution` | 打开 .sln 文件 |
| `close_solution` | 关闭当前解决方案 |
| `create_solution` | 创建新解决方案（工作区） |

### 项目管理（5个）
| 工具 | 描述 |
|------|------|
| `get_project_files` | 获取项目中的文件 |
| `get_startup_projects` | 获取启动项目 |
| `add_file_to_project` | 添加文件到项目 |
| `set_startup_project` | 设置启动项目 |
| `get_project_properties` | 获取项目属性 |

### 文件操作（5个）
| 工具 | 描述 |
|------|------|
| `open_file` | 在 VS 编辑器中打开文件 |
| `read_file` | 读取文件内容 |
| `write_file` | 写入文件内容 |
| `replace_in_file` | 替换文件中的文本 |
| `get_file_info` | 获取文件信息 |

### 构建操作（6个）
| 工具 | 描述 |
|------|------|
| `build_solution` | 构建解决方案 |
| `rebuild_solution` | 重新构建解决方案 |
| `clean_solution` | 清理解决方案 |
| `build_project` | 构建指定项目 |
| `get_build_errors` | 获取构建错误 |
| `get_build_configurations` | 获取构建配置 |

### 调试控制（8个）
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

### 诊断（3个）
| 工具 | 描述 |
|------|------|
| `health_check` | 验证服务器和 VS 连接 |
| `get_server_info` | 获取服务器信息 |
| `get_diagnostic_logs` | 获取最近的诊断日志 |

---

## 命令行选项

```bash
universal-vsmcp --stdio                    # 默认：stdio 传输
universal-vsmcp --stdio --log-file uv.log  # 带文件日志
universal-vsmcp --verify                   # 验证 VS 连接
universal-vsmcp --help                     # 显示帮助
```

---

## 配置

### VS 2026 配置

**方法 A：直接 JSON（推荐）**
1. 转到 **工具 → 选项 → 环境 → 扩展**
2. 勾选 **"将用户设置编辑为 JSON"**
3. 粘贴：

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

**方法 B：GUI**
1. **工具 → 选项 → 环境 → 扩展**
2. 在 **MCP 服务器列表** 点击 **添加**
3. 填写：
   - **名称**: `universal-vsmcp`
   - **命令**: `dotnet`
   - **参数**: `tool run --global universal-vsmcp -- --stdio`

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

## Localhost 支持

用于本地开发或 SSE 传输：

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

## 故障排除

### 验证连接

```bash
# 检查服务器是否已安装
dotnet tool list -g

# 运行健康检查工具（从 MCP 客户端）
# 或使用日志运行：
universal-vsmcp --stdio --log-file uv.log
```

### 常见问题

| 问题 | 解决方案 |
|------|----------|
| "找不到 VS 实例" | 先启动 Visual Studio |
| "服务器无响应" | 检查 `UVM_LOG_FILE` 中的错误 |
| "工具未找到" | 确认服务器正在运行 |
| "连接失败" | 确保 VS 不是以管理员身份运行 |

---

## 系统要求

- .NET 8.0 SDK 或更高版本
- Visual Studio 2026 (18.0) 或 VS 2022 (17.14+)
- Windows 10 或更高版本

---

## 许可

MIT © StarsailsClover

---

## 相关链接

- [NuGet 包](https://www.nuget.org/packages/UniversalVSMCP/)
- [GitHub 仓库](https://github.com/StarsailsClover/UniversalVSMCP)
- [MCP 文档](https://modelcontextprotocol.io/)
- [VS 2026 MCP 指南](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
