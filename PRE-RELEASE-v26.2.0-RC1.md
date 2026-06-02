# Pre-Release v26.2.0-RC1

**Release Candidate 1**  
**Date**: 2026-06-01  
**Status**: Ready for Testing

---

## 📦 Package Information

| Component | Version | Package | Status |
|-----------|---------|---------|--------|
| **LVS** | v26.2.0-RC1 | GitHub Release | Ready |
| **UVM** | v26.2.0-RC1 | NuGet + GitHub | Ready |
| **VUV** | v26.2.0-RC1 | VSIX + OpenVSX | Ready |

---

## 🎯 Release Highlights

### ✅ Phase 4-5 Complete: VS Code Extension Server
- Extension HTTP Server for UVM ↔ VS Code communication
- AI Agent can connect via `http://localhost:5001/sse`
- Native VS Code API integration

### ✅ Extension Tools (NEW!)
- `start_vscode_extension_server` - Start Extension MCP Server
- `stop_vscode_extension_server` - Stop Extension Server
- `get_extension_server_status` - Check server status
- `get_ai_agent_config` - Get AI Agent connection config
- `install_vscode_extension` - Install from marketplace

### ✅ Unified Architecture
- IIdeAdapter interface
- VsDteAdapter (Visual Studio)
- VsCodeAdapter (VS Code)
- IdeRouter (smart routing)

### ✅ Security Framework
- Workspace Trust
- Tool Permission Manager
- Prompt Injection Detector
- Authentication Middleware

---

## 📥 Installation

### 1. Link2VS Skill (LVS)

```bash
# Clone
git clone https://github.com/StarsailsClover/Link2VS.skill.git
cd Link2VS.skill
git checkout v26.2.0-RC1

# Or download release
# https://github.com/StarsailsClover/Link2VS.skill/releases/tag/v26.2.0-RC1
```

### 2. UniversalVSMCP (UVM)

```bash
# NuGet
dotnet tool install -g UniversalVSMCP --version 26.2.0-rc1

# Or download from GitHub
# https://github.com/StarsailsClover/UniversalVSMCP/releases/tag/v26.2.0-RC1
```

### 3. VS Code Extension (VUV)

**Option A: VS Code Marketplace**
```
Search: "Universal VS MCP"
Install v26.2.0-RC1
```

**Option B: Open VSX**
```
https://open-vsx.org/extension/StarsailsClover/universal-vsmcp
```

**Option C: VSIX**
```bash
# Download from GitHub Releases
code --install-extension universal-vsmcp-26.2.0-rc1.vsix
```

---

## 🔧 Configuration

### AI Agent Configuration

```json
{
  "mcpServers": {
    "universal-vsmcp": {
      "command": "universal-vsmcp",
      "args": ["--stdio"],
      "env": {
        "VS_AUTO_DETECT": "true"
      }
    },
    "vscode-extension": {
      "url": "http://localhost:5001/sse",
      "transport": "sse"
    }
  }
}
```

### VS Code Extension Settings

```json
{
  "uvm.server.autoStart": true,
  "uvm.http.port": 5001,
  "uvm.connectionStrategy": "prefer-native"
}
```

---

## 🧪 Testing Checklist

### Core Functionality
- [ ] Install all three components
- [ ] Start UVM: `universal-vsmcp --stdio`
- [ ] Start VS Code Extension
- [ ] Verify HTTP Server starts on port 5001
- [ ] AI Agent connects successfully

### Extension Tools
- [ ] `start_vscode_extension_server` works
- [ ] `get_extension_server_status` returns correct status
- [ ] `get_ai_agent_config` generates valid config
- [ ] Extension stops cleanly

### Multi-IDE Support
- [ ] Visual Studio connection works
- [ ] VS Code connection works
- [ ] Smart routing selects correct IDE
- [ ] File operations work in both IDEs

### Security
- [ ] Workspace Trust check works
- [ ] User confirmation dialogs appear
- [ ] Audit logs are generated
- [ ] Permission system blocks unauthorized actions

---

## 🐛 Known Issues

| Issue | Workaround | Status |
|-------|------------|--------|
| HTTP Server port conflict | Change port in settings | Documented |
| VS Code Extension requires reload after install | Reload window | Documented |
| Native MCP detection on Windows | Use stdio fallback | Documented |

---

## 📊 Test Matrix

| OS | VS Version | VS Code | Status |
|----|------------|---------|--------|
| Windows 11 | VS 2022 | 1.90 | ⏳ Pending |
| Windows 11 | VS 2026 | 1.90 | ⏳ Pending |
| macOS | - | 1.90 | ⏳ Pending |
| Linux | - | 1.90 | ⏳ Pending |

---

## 🚀 Next Steps

1. **Test** this RC1 release
2. **Report** issues at: https://github.com/StarsailsClover/UniversalVSMCP/issues
3. **Feedback** on new Extension Tools
4. **Stable Release** v26.2.0 after testing

---

## 📞 Support

- **GitHub Issues**: https://github.com/StarsailsClover/UniversalVSMCP/issues
- **Email**: SailsHuang@gmail.com
- **Discord**: [Link2VS Community]

---

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for full details.

---

**Published**: 2026-06-01  
**Publisher**: StarsailsClover  
**License**: MIT
