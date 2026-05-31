# UniversalVSMCP - VSC (Visual Studio Connector) 架构

## 概述

**混合传输架构** - 同时支持 stdio 和 HTTP/SSE，为不同使用场景提供最佳体验。

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

## 1. 传输模式对比

| 模式 | 命令 | 适用场景 | URL |
|------|------|----------|-----|
| **stdio** | `--stdio` | Claude/Cursor/AI Agent | 无 |
| **HTTP** | `--http 5000` | VS 2026 MCP Manager | `http://localhost:5000/sse` |
| **Hybrid** | `--hybrid` | 两者都需要 | 两者 |

---

## 2. VS 2026 MCP 管理器配置

### 方法A：使用 URL (推荐)

在 VS 2026 **Tools → Options → Environment → Extensions → MCP Servers** 中：

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

### 方法B：启动后连接

1. 首先启动 HTTP 服务器：
```bash
universal-vsmcp --http 5000
```

2. 在 VS 2026 MCP 管理器中添加：
   - **Name**: `universal-vsmcp`
   - **URL**: `http://localhost:5000/sse`

---

## 3. 智能体接入方案 (VSC)

### 3.1 直接 HTTP 接入

AI 智能体可以直接通过 HTTP API 调用：

```python
import requests

# List tools
response = requests.get("http://localhost:5000/tools")
tools = response.json()

# Call a tool
response = requests.post("http://localhost:5000/tools/call", json={
    "name": "get_solution_projects",
    "arguments": {}
})
result = response.json()
```

### 3.2 SSE 流式响应

```javascript
const eventSource = new EventSource('http://localhost:5000/sse');

eventSource.addEventListener('server-info', (e) => {
    const info = JSON.parse(e.data);
    console.log('Server:', info);
});

eventSource.addEventListener('tools-list', (e) => {
    const tools = JSON.parse(e.data);
    console.log('Tools:', tools);
});

eventSource.addEventListener('ping', (e) => {
    // Keep-alive
});
```

---

## 4. 路由器设计 (V2)

### 4.1 多 VS 实例路由

```csharp
public class VsRouter
{
    // Route to specific VS instance by version
    public IVsConnection GetInstance(string version)
    {
        // VS 2026: v18.x
        // VS 2022: v17.x
    }
    
    // Route by solution name
    public IVsConnection GetInstanceBySolution(string solutionName)
    {
        // Find VS with specific solution open
    }
    
    // Route by project type
    public IVsConnection GetInstanceByProjectType(string projectType)
    {
        // Find VS with specific project type
    }
}
```

### 4.2 负载均衡

```csharp
public class LoadBalancer
{
    private List<IVsConnection> _instances;
    private int _currentIndex = 0;
    
    public IVsConnection GetNextInstance()
    {
        // Round-robin selection
        var instance = _instances[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _instances.Count;
        return instance;
    }
}
```

---

## 5. 认证与安全 (Future)

### 5.1 API Key 认证

```csharp
public class ApiKeyAuthMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"];
        if (!ValidateApiKey(apiKey))
        {
            context.Response.StatusCode = 401;
            return;
        }
        await _next(context);
    }
}
```

### 5.2 OAuth 2.0 (Enterprise)

```csharp
public class OAuth2Auth
{
    // Token endpoint
    // Authorization endpoint
    // Refresh token flow
}
```

---

## 6. 监控与遥测

### 6.1 健康检查端点

- `GET /health` - 基础健康状态
- `GET /health/detailed` - 详细状态（VS连接、工具列表等）
- `GET /metrics` - Prometheus 格式指标

### 6.2 日志级别

```csharp
public enum LogLevel
{
    Debug,      // 开发调试
    Info,       // 一般信息
    Warning,    // 警告
    Error,      // 错误
    Critical    // 严重错误
}
```

---

## 7. 部署方案

### 7.1 本地开发

```bash
# 安装工具
dotnet tool install -g UniversalVSMCP

# 启动 HTTP 模式
universal-vsmcp --http 5000

# 或使用 hybrid 模式
universal-vsmcp --hybrid
```

### 7.2 团队共享

```bash
# 配置文件
~/.vsmcp/config.json
{
    "http": {
        "port": 5000,
        "host": "0.0.0.0",
        "auth": "apikey"
    },
    "vs": {
        "autoDetect": true,
        "preferredVersion": "18.0"
    }
}
```

### 7.3 Docker 容器 (Future)

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY . /app
WORKDIR /app
EXPOSE 5000
ENTRYPOINT ["dotnet", "UniversalVSMCP.dll", "--http", "5000"]
```

---

## 8. 扩展点

### 8.1 自定义工具

```csharp
[McpServerToolType]
public class CustomTools
{
    [McpServerTool(Name = "my_custom_tool")]
    public Task<OperationResult> MyCustomTool(string param)
    {
        // Custom implementation
    }
}
```

### 8.2 中间件管道

```csharp
public class CustomMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Pre-processing
        await _next(context);
        // Post-processing
    }
}
```

---

## 9. 路线图

### v26.1.0 (Next)
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

## 10. 快速参考

### 启动命令

```bash
# Stdio (Claude/Cursor)
universal-vsmcp --stdio

# HTTP (VS 2026)
universal-vsmcp --http 5000

# Hybrid (Both)
universal-vsmcp --hybrid

# With logging
universal-vsmcp --http 5000 --log-file vsmcp.log
```

### 端点速查

| 端点 | 方法 | 说明 |
|------|------|------|
| `/sse` | GET | SSE 连接（主端点） |
| `/health` | GET | 健康检查 |
| `/info` | GET | 服务器信息 |
| `/tools` | GET | 工具列表 |
| `/tools/call` | POST | 调用工具 |

---

**文档版本**: v26.0.0-20260531-UVM  
**更新日期**: 2026-05-31  
**作者**: StarsailsClover
