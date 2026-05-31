# VS Code Native MCP Support & Strategy Guide

## 📢 重要发现

VS Code 2026 已原生支持 MCP (Model Context Protocol)!

这意味着：
1. VS Code 现在内置了 MCP 客户端
2. 可以通过 `mcp.json` 配置 MCP 服务器
3. 扩展侧边栏下方会显示官方 MCP 服务器列表
4. 无需额外的扩展即可使用 MCP

---

## 🔄 当前方案对比

### 方案 A: VS Code 原生 MCP (推荐)

**工作原理**:
- 用户直接配置 `mcp.json`
- VS Code 内置客户端连接到 UVM
- 无需安装额外扩展

**优点**:
- ✅ 零扩展安装
- ✅ 原生体验
- ✅ VS Code 官方维护
- ✅ 自动更新支持

**缺点**:
- ⚠️ 需要 VS Code 2026+
- ⚠️ 功能受限于 VS Code 实现
- ⚠️ 无法自定义 UI (状态栏、树视图等)

**配置示例**:
```json
{
  "servers": {
    "universal-vsmcp": {
      "name": "Universal VS MCP",
      "command": "universal-vsmcp",
      "args": ["--stdio"],
      "env": { "VS_AUTO_DETECT": "true" }
    }
  }
}
```

---

### 方案 B: 我们的 VS Code 扩展 (当前)

**工作原理**:
- 安装 `universal-vsmcp-26.0.1.vsix`
- 扩展启动 UVM 进程
- 提供自定义 UI (状态栏、命令、树视图)

**优点**:
- ✅ 完整自定义 UI (状态栏、树视图、输出面板)
- ✅ 向后兼容旧版 VS Code
- ✅ 可以完全控制体验
- ✅ 安全确认对话框
- ✅ 解决方案浏览器

**缺点**:
- ⚠️ 需要安装扩展
- ⚠️ 维护负担

---

### 方案 C: 混合方案 (推荐长期)

**工作原理**:
- 支持原生 MCP (`mcp.json`)
- 可选扩展增强 (UI、树视图、安全)

**优点**:
- ✅ 最佳用户体验
- ✅ 灵活性最高
- ✅ 渐进式采用

**配置**:
```json
// mcp.json - 原生支持
{
  "servers": {
    "universal-vsmcp": {
      "name": "Universal VS MCP",
      "command": "universal-vsmcp",
      "args": ["--stdio"]
    }
  }
}
```

**可选扩展**:
- 解决方案浏览器
- 高级安全设置
- 自定义命令

---

## 🎯 建议策略

### 短期 (v26.0.x)
**采用当前扩展方案**

原因:
1. 原生 MCP 刚推出，稳定性待观察
2. 我们需要自定义 UI (解决方案浏览器)
3. 用户可以立即使用

### 中期 (v26.1.x)
**双轨支持**

1. 维护扩展 (向后兼容)
2. 优化 `mcp.json` 配置
3. 提供两种安装选项

### 长期 (v27.0.0)
**原生优先，扩展增强**

1. 原生 MCP 作为主要入口
2. 扩展变为"增强包"
3. 专注于 VS Code 不支持的功能

---

## 🛠️ 当前推荐操作

### 对于用户 (今天)

**选项 1: 使用 VS Code 扩展 (推荐)**
```bash
# 安装 VSIX
code --install-extension universal-vsmcp-26.0.1.vsix

# 在 VS Code 中
# Ctrl+Shift+P -> "UVM: Connect to Agent"
```

**选项 2: 使用原生 MCP (尝鲜)**
```bash
# 创建 mcp.json 在工作区根目录
echo '{
  "servers": {
    "universal-vsmcp": {
      "name": "Universal VS MCP",
      "command": "universal-vsmcp",
      "args": ["--stdio"]
    }
  }
}' > mcp.json
```

---

## 📊 功能对比

| 功能 | 原生 MCP | 我们的扩展 | 混合 |
|------|----------|------------|------|
| 基础连接 | ✅ | ✅ | ✅ |
| 状态栏指示 | ❌ | ✅ | ✅ |
| 解决方案浏览器 | ❌ | ✅ | ✅ |
| 用户确认对话框 | ❌ | ✅ | ✅ |
| 输出通道 | ❌ | ✅ | ✅ |
| 无需安装 | ✅ | ❌ | ⚠️ |
| VS Code 2022+ | ❌ | ✅ | ✅ |

---

## 🚀 下一步行动

### 立即可做
1. ✅ 修复连接超时问题 (已完成)
2. ✅ 重命名命令为 "Connect to Agent" (已完成)
3. ✅ 提供 `mcp.json` 示例 (已完成)

### 短期 (v26.1.0)
1. 测试原生 MCP 兼容性
2. 添加 HTTP 模式支持
3. 优化连接稳定性

### 中期 (v26.2.0)
1. 支持多 VS 实例路由
2. 探索 VS Code 内置 MCP API
3. 提供原生/扩展自动检测

### 长期 (v27.0.0)
1. 成为"原生 MCP 增强扩展"
2. 专注于 UI 和安全功能
3. 支持 VS Code 工作区转换

---

## 💡 用户问题解答

### Q: "Failed to connect... initialization timeout"
**A**: 已修复。更新到 v26.0.1，我们改进了连接检测逻辑。

### Q: 为什么命令叫 "Connect to Agent" 而不是 "Connect to VS"?
**A**: 更准确地反映架构：VS Code (Client) → Extension/Native MCP → UVM (Agent) → Visual Studio

### Q: 我应该用原生 MCP 还是扩展?
**A**: 
- 现在: 用扩展 (功能完整)
- 未来: 混合 (原生连接 + 扩展 UI)

### Q: "VS Code 扩展侧边栏下方多了官方支持的 MCP"
**A**: 这是 VS Code 2026 的新功能！可以在那里直接配置 MCP 服务器，无需扩展。

---

## 🔗 相关链接

- [VS Code MCP 配置](https://code.visualstudio.com/docs/remote/mcp)
- [MCP 协议规范](https://modelcontextprotocol.io)
- [Awesome MCP](https://github.com/punkpeye/awesome-mcp-servers)

---

**版本**: v26.0.1  
**更新**: 2026-05-31  
**状态**: 已修复连接问题，提供双方案支持
