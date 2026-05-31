# VS 2026 MCP 服务器快速配置指南

## 🚀 5分钟快速开始

### 步骤 1: 安装工具

```bash
dotnet tool install -g UniversalVSMCP
```

### 步骤 2: 启动 HTTP 服务器

```bash
universal-vsmcp --http 5000
```

你会看到：
```
=================================================================
     UniversalVSMCP (UVM) v26.0.0-20260531-UVM - VS 2026/2022 MCP Server
              AI Agent <-> Visual Studio Bridge
=================================================================
Transport:  Http
HTTP Port:  5000
VS 2026 URL: http://localhost:5000/sse
VS Target:  Auto-detect latest instance
...
✓ HTTP Server ready at: http://localhost:5000/sse
```

### 步骤 3: 在 VS 2026 中配置

1. 打开 **Tools → Options → Environment → Extensions**
2. 点击 **MCP Servers**
3. 点击 **Add** 添加新服务器
4. 填写配置：

```json
{
  "name": "universal-vsmcp",
  "url": "http://localhost:5000/sse",
  "transport": "sse"
}
```

或者直接填写：
- **Name**: `Universal VS MCP`
- **URL**: `http://localhost:5000/sse`
- **Transport**: `sse`

5. 点击 **Save**

6. VS 2026 会自动检测并连接服务器

---

## 📋 配置详情

### MCP Server Manager 界面配置

**Name**: `Universal VS MCP`

**URL**: `http://localhost:5000/sse`

**Transport**: `sse` (Server-Sent Events)

**可选参数**:
- 如果 VS 未自动检测，可设置环境变量 `VS_AUTO_DETECT=true`

---

## 🔄 持久化配置

### 方法A: User Settings (全局)

编辑 `%USERPROFILE%\.vscode\mcp.json` 或 VS 2026 用户设置：

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

### 方法B: Workspace Settings (工作区)

在项目根目录创建 `.mcp.json`：

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

---

## 🔧 高级配置

### 自定义端口

```bash
# 使用端口 8080
universal-vsmcp --http 8080
```

然后配置 URL：`http://localhost:8080/sse`

### Hybrid 模式 (推荐用于开发)

同时使用 stdio 和 HTTP：

```bash
universal-vsmcp --hybrid 5000
```

- AI Agent 可以通过 stdio 连接
- VS 2026 可以通过 HTTP 连接

---

## 🛠️ 故障排除

### 问题 1: 端口被占用

```bash
# 错误：端口 5000 已被占用
# 解决方案：使用其他端口
universal-vsmcp --http 5001
```

### 问题 2: VS 未连接

1. 确保 Visual Studio 2026 已打开
2. 确保已加载解决方案
3. 检查服务器健康状态：

```bash
curl http://localhost:5000/health
```

### 问题 3: 防火墙阻挡

确保 Windows 防火墙允许端口 5000：

```powershell
# 管理员权限 PowerShell
New-NetFirewallRule -DisplayName "UniversalVSMCP" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

---

## 📊 验证连接

### 1. 检查服务器状态

浏览器访问：`http://localhost:5000/health`

响应示例：
```json
{
  "status": "healthy",
  "vsConnected": true,
  "timestamp": "2026-05-31T18:30:00Z"
}
```

### 2. 查看工具列表

浏览器访问：`http://localhost:5000/tools`

### 3. 查看服务器信息

浏览器访问：`http://localhost:5000/info`

---

## 🎯 使用场景

### 场景 A: 纯 VS 2026 使用

```bash
# 启动 HTTP 服务器
universal-vsmcp --http 5000

# 在 VS 2026 中配置 URL
# 然后就可以在 Copilot Agent Mode 中使用
```

### 场景 B: AI Agent + VS 2026

```bash
# 启动混合模式
universal-vsmcp --hybrid 5000

# Claude/Cursor 使用 stdio
# VS 2026 使用 http://localhost:5000/sse
```

### 场景 C: 团队共享

```bash
# 启动并允许外部访问 (需配置防火墙)
universal-vsmcp --http 0.0.0.0:5000
```

---

## 📝 命令参考

```bash
# 标准输入输出 (AI Agent)
universal-vsmcp --stdio

# HTTP 模式 (VS 2026)
universal-vsmcp --http 5000

# 混合模式 (两者)
universal-vsmcp --hybrid

# 带日志
universal-vsmcp --http 5000 --log-file vsmcp.log

# 验证 VS 连接
universal-vsmcp --verify

# 显示帮助
universal-vsmcp --help
```

---

**配置完成！** 现在你可以在 VS 2026 MCP Server Manager 中看到 UniversalVSMCP 已连接。  
**版本**: v26.0.2  
**最后更新**: 2026-05-31
