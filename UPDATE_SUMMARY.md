# UniversalVSMCP v26.0.0 - 混合传输架构更新

## 🎯 本次更新核心内容

### ✅ 已实现：HTTP/SSE 传输支持

解决了 VS 2026 MCP 服务器管理器只能使用 URL 配置的问题。

**传输模式对比：**

| 模式 | 命令 | 适用场景 | URL |
|------|------|----------|-----|
| **stdio** | `--stdio` | Claude/Cursor/AI Agent | 无 |
| **HTTP** | `--http 5000` | VS 2026 MCP 管理器 | `http://localhost:5000/sse` |
| **Hybrid** | `--hybrid` | 两者都需要 | 两者 |

---

## 📋 VS 2026 配置方法

### 方法A：URL 配置（推荐）

在 VS 2026 **Tools → Options → Environment → Extensions → MCP Servers** 中添加：

```json
{
  "mcpServers": {
    "universal-vsmcp": {
      "name": "Universal VS MCP",
      "url": "http://localhost:5000/sse",
      "transport": "sse"
    }
  }
}
```

### 方法B：先启动后配置

1. 打开命令行，启动 HTTP 服务器：
```bash
universal-vsmcp --http 5000
```

2. 在 VS 2026 MCP 管理器中添加：
   - **Name**: `universal-vsmcp`
   - **URL**: `http://localhost:5000/sse`

---

## 🚀 使用方式

### 安装
```bash
dotnet tool install -g UniversalVSMCP
```

### 启动方式

**stdio 模式**（Claude/Cursor）：
```bash
universal-vsmcp --stdio
```

**HTTP 模式**（VS 2026）：
```bash
universal-vsmcp --http 5000
```

**Hybrid 模式**（两者）：
```bash
universal-vsmcp --hybrid 5000
```

---

## 🔌 HTTP 端点

启动 HTTP 服务器后，可用端点：

| 端点 | 方法 | 说明 |
|------|------|------|
| `/sse` | GET | **SSE 连接（主端点）** |
| `/health` | GET | 健康检查 |
| `/info` | GET | 服务器信息 |
| `/tools` | GET | 工具列表 |
| `/tools/call` | POST | 调用工具 |

---

## 🏗️ VSC (Visual Studio Connector) 架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         UniversalVSMCP (UVM)                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────────────┐    │
│  │   stdio      │      │  HTTP/SSE    │      │    VS Router         │    │
│  │  Transport   │      │  Transport   │      │   (Future)           │    │
│  └──────┬───────┘      └──────┬───────┘      └──────────┬───────────┘    │
│         │                     │                         │                 │
│         ▼                     ▼                         ▼                 │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────────────┐    │
│  │ Claude/      │      │ VS 2026 MCP  │      │  External            │    │
│  │ Cursor       │      │ Manager      │      │  Agents              │    │
│  └──────────────┘      └──────────────┘      └──────────────────────┘    │
│                                                                            │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │                    DTE/OM Connection Layer                          │  │
│  │              (Connects to VS 2026/2022)                             │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 30个 MCP 工具

### 解决方案管理 (6)
- `get_solution_projects`, `get_solution_path`, `get_solution_name`
- `open_solution`, `close_solution`, `create_solution`

### 项目管理 (5)
- `get_project_files`, `get_startup_projects`, `add_file_to_project`
- `set_startup_project`, `get_project_properties`

### 文件操作 (5)
- `open_file`, `read_file`, `write_file`, `replace_in_file`, `get_file_info`

### 构建操作 (6)
- `build_solution`, `rebuild_solution`, `clean_solution`, `build_project`
- `get_build_configurations`, `set_build_configuration`

### 调试操作 (6)
- `start_debugging`, `stop_debugging`, `set_breakpoint`, `remove_breakpoint`
- `get_breakpoints`, `step_over`

### 诊断工具 (2)
- `health_check`, `get_server_info`

---

## 🔮 未来路线图 (VSC)

### v26.1.0
- [ ] 多 VS 实例路由
- [ ] API Key 认证
- [ ] 详细遥测指标

### v26.2.0
- [ ] WebSocket 传输
- [ ] 插件系统
- [ ] 配置文件热重载

### v27.0.0
- [ ] 远程 VS 连接 (SSH)
- [ ] 集群支持
- [ ] 企业级安全

---

## 📁 新增/修改文件

```
UniversalVSMCP/
├── src/UniversalVSMCP/
│   ├── HttpServer.cs          # 新增：HTTP/SSE 服务器
│   ├── McpModels.cs           # 新增：HTTP 传输模型
│   ├── Program.cs             # 修改：支持多传输模式
│   └── UniversalVSMCP.csproj  # 修改：添加 Web SDK 支持
├── server.json                # 修改：添加 HTTP 配置
└── docs/
    └── VSC-Architecture.md    # 新增：架构文档
```

---

## 🎉 总结

**本次更新彻底解决了 VS 2026 MCP 管理器配置问题：**

✅ 支持 URL 配置 (`http://localhost:5000/sse`)
✅ 向后兼容 stdio 模式
✅ 支持混合模式（同时运行）
✅ 30个工具完整可用
✅ 详细架构文档

**使用建议：**
- **AI Agent (Claude/Cursor)** → stdio 模式
- **VS 2026 MCP 管理器** → HTTP 模式
- **两者同时用** → Hybrid 模式

---

**版本**: v26.0.0-20260531-UVM  
**更新日期**: 2026-05-31  
**作者**: StarsailsClover
