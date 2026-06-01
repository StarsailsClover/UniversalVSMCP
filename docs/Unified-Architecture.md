# Unified IDE Architecture - Link2VS + UniversalVSMCP

## 🎯 架构愿景

**不是 Trae，而是 "超越 Trae"**

- Trae: 独立 AI IDE (无法使用 MCP)
- LVS+UVM: **AI Agent 原生连接现有 IDE** (通过 MCP)
- 核心优势: 利用现有 IDE 生态，同时获得 AI Agent 能力

---

## 🏗️ 统一架构设计

### 架构层级

```
┌─────────────────────────────────────────────────────────────────┐
│                     AI Agent Layer                                │
│  (Claude Code, Cursor, GitHub Copilot, Custom Agents)            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ MCP Protocol (stdio / HTTP)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Unified MCP Server (UVM)                        │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐      │
│  │   Security    │  │   Routing     │  │   Protocol    │      │
│  │    Layer      │  │    Layer      │  │   Adapter     │      │
│  └───────────────┘  └───────────────┘  └───────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  VS 2022/2026   │  │    VS Code      │  │  Future IDEs    │
│   (COM/DTE)     │  │  (Extension)    │  │  (JetBrains...) │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

---

## 📐 核心组件

### 1. 抽象 IDE 接口层 (IIdeAdapter)

```csharp
public interface IIdeAdapter
{
    string IdeName { get; }
    string IdeVersion { get; }
    IdeCapabilities Capabilities { get; }
    
    // Solution/Project
    Task<SolutionInfo> GetSolutionAsync();
    Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync();
    Task OpenSolutionAsync(string path);
    
    // File Operations
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
    
    // Build
    Task<BuildResult> BuildAsync(BuildConfiguration config);
    
    // Debug
    Task StartDebuggingAsync();
    Task SetBreakpointAsync(string file, int line);
}
```

### 2. IDE 适配器实现

| 适配器 | 技术 | 状态 |
|--------|------|------|
| `VsDteAdapter` | COM/DTE | ✅ 已实现 |
| `VsCodeAdapter` | Extension API | 🔄 开发中 |
| `JetBrainsAdapter` | IntelliJ SDK | 📋 规划中 |

### 3. 统一路由层

```csharp
public class IdeRouter
{
    private readonly Dictionary<string, IIdeAdapter> _adapters;
    
    public IIdeAdapter Route(RoutingCriteria criteria)
    {
        return criteria switch {
            { IdeType: "vs", Version: >= 17 } => _adapters["vs2022"],
            { IdeType: "vscode" } => _adapters["vscode"],
            { FileExtension: ".sln" } => _adapters["vs2022"],
            _ => _adapters["default"]
        };
    }
}
```

---

## 🔄 与 Trae 的对比

| 维度 | Trae | LVS+UVM (我们) |
|------|------|----------------|
| **架构** | 独立 IDE | MCP 桥接 |
| **MCP 支持** | ❌ 无法使用 | ✅ 原生支持 |
| **现有项目** | 需要迁移 | 零迁移成本 |
| **生态系统** | 从头建设 | 继承 VS/VS Code 生态 |
| **调试能力** | 基础 | 完整 VS 调试器 |
| **扩展市场** | 无 | 完整 Marketplace |
| **企业采用** | 难（新工具） | 易（现有工具增强） |

**我们不是 Trae，我们让 VS/VS Code 变成 "有 AI Agent 能力的 Trae"**

---

## 🛡️ 安全架构

```
AI Agent Request
       │
       ▼
┌─────────────────┐
│  Injection      │  ← PromptInjectionDetector
│  Detector       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Permission     │  ← ToolPermissionManager
│  Manager        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  User           │  ← UserConfirmationManager
│  Confirmation   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  IDE Router     │  ← Route to VS/VS Code
│                 │
└─────────────────┘
```

---

## 📊 技术选型

### 为什么不是 Trae 路线？

**Trae 的限制**:
1. 需要用户切换 IDE（习惯成本）
2. 调试能力有限（无法匹敌 VS）
3. 生态系统从零开始
4. 企业采购障碍（新供应商审查）

**我们的优势**:
1. 零切换成本（使用现有 IDE）
2. 完整调试能力（原生 VS 调试器）
3. 继承完整生态（扩展、主题、配置）
4. 企业友好（现有供应商 Microsoft）

### MCP 协议的优势

- **标准化**: 微软主导的开放协议
- **多客户端**: Claude、Cursor、Copilot 都支持
- **安全**: 标准输入/输出隔离
- **可扩展**: 易于添加新工具

---

## 🚀 部署模式

### 模式 1: VS 主导（企业开发）

```
AI Agent → UVM → VS 2026
                 ↓
            [调试、分析、重构]
```

**适用场景**: C#/.NET 企业开发、复杂调试

### 模式 2: VS Code 主导（轻量编辑）

```
AI Agent → UVM → VS Code
                 ↓
            [快速编辑、Git、轻量调试]
```

**适用场景**: Web 开发、配置文件编辑

### 模式 3: 双 IDE（混合模式）

```
AI Agent → UVM → ┬→ VS 2026 (主开发)
                 └→ VS Code (辅助)
```

**适用场景**: 大型项目，不同任务用不同 IDE

---

## 📈 扩展性设计

### 支持未来 IDE

```csharp
// 新增 IDE 只需实现 IIdeAdapter
public class JetBrainsAdapter : IIdeAdapter { ... }
public class XcodeAdapter : IIdeAdapter { ... }
public class AndroidStudioAdapter : IIdeAdapter { ... }
```

### 动态加载

```csharp
// 通过配置文件动态加载适配器
"ideAdapters": {
    "vs2022": "UniversalVSMCP.Adapters.VsDteAdapter",
    "vscode": "UniversalVSMCP.Adapters.VsCodeAdapter",
    "rider": "UniversalVSMCP.Adapters.JetBrainsAdapter"
}
```

---

## 🔮 架构演进

### v26.x (当前)
- ✅ VS 2022/2026 COM/DTE
- 🔄 VS Code 基础支持

### v27.0 (下一版本)
- ✅ 统一 IDE 接口
- ✅ 动态适配器加载
- ✅ VS Code 完整支持

### v28.0 (未来)
- ✅ JetBrains 系列支持
- ✅ 多 IDE 同时连接
- ✅ 智能路由决策

---

## 📝 总结

**核心设计原则**:
1. **不是替代，是增强** - 让现有 IDE 更智能
2. **开放协议** - MCP 标准，多客户端支持
3. **渐进采用** - 零迁移成本，按需启用
4. **安全第一** - 多层防护，企业就绪

**与 Trae 的关系**:
- 不是竞争对手，是互补方案
- Trae: 想要全新 AI IDE 体验的用户
- 我们: 想要 AI 增强现有 IDE 体验的用户

---

**架构设计完成** ✅  
**下一步**: 定义路线图 →
