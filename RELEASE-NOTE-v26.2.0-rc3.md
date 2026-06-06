# UniversalVSMCP (UVM) v26.2.0-RC3 Release Notes

**Release Date**: 2026-06-06  
**Status**: Pre-Release  
**Tag**: v26.2.0-rc3

---

## 🎯 Highlights

This is a **Pre-Release** candidate for the Link2VS Suite v26.2.0 series, containing critical bug fixes and improvements over RC2.

---

## 🐛 Bug Fixes

### Critical: Dependency Injection Error (RC2 → RC3)
- **Issue**: `No service for type 'UniversalVSMCP.IVsConnectionManager' has been registered`
- **Impact**: HTTP server failed to start, MCP communication unavailable
- **Fix**: Added `IVsConnectionManager` service registration in `Program.cs`

```csharp
// Added in BuildHost() method
services.AddSingleton<IVsConnectionManager, VsConnectionManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<VsConnectionManager>>();
    return new VsConnectionManager(logger);
});
```

### Version Display
- Fixed version string from "26.0.3" to "26.2.0-rc3" in banner and MCP ServerInfo

---

## 📊 Compilation Status

| Metric | RC2 | RC3 |
|--------|-----|-----|
| Errors | ~50 | **0** |
| Warnings | ~30 | **29** |
| Status | ❌ Failed | ✅ **Success** |

**Note**: Remaining 29 warnings are platform compatibility warnings (Windows-specific APIs) that don't affect functionality.

---

## 🏗️ Architecture

```
AI Agent ←→ MCP ←→ IdeRouter ←→ IIdeAdapter ←→ VS/VS Code
                ↓
         IVsConnectionManager (NEW)
                ↓
         HTTP Server / Stdio Transport
```

---

## 📦 What's Included (Source)

This repository contains:
- ✅ Source code (C# .NET 8.0)
- ✅ Project files (.csproj, .sln)
- ✅ Documentation (README, Architecture, Security)
- ✅ Configuration examples
- ✅ .gitignore for clean builds

**Not included** (see Release Assets):
- Compiled binaries (.exe, .dll)
- NuGet packages
- Build outputs

---

## 🔧 Building from Source

```bash
# Clone
git clone https://github.com/StarsailsClover/UniversalVSMCP.git
cd UniversalVSMCP

# Checkout RC3
git checkout v26.2.0-rc3

# Build
cd src/UniversalVSMCP
dotnet build -c Release

# Or publish self-contained
dotnet publish -c Release --self-contained -r win-x64
```

---

## 🚀 Usage

```powershell
# Stdio mode (for MCP)
$env:VS_AUTO_DETECT = "true"
UniversalVSMCP.exe --stdio

# HTTP mode
UniversalVSMCP.exe --http 5000

# Verify connection
UniversalVSMCP.exe --verify
```

---

## 🔗 Related Components

| Component | Version | Repository |
|-----------|---------|------------|
| UVM | v26.2.0-rc3 | This repo |
| VUV | v26.2.0-rc3 | [VscodeUniversalVSMCP](https://github.com/StarsailsClover/VscodeUniversalVSMCP) |
| LVS | v26.2.0-rc3 | [Link2VS.skill](https://github.com/StarsailsClover/Link2VS.skill) |

---

## ⚠️ Known Limitations

1. **Windows Only**: DTE/COM dependencies require Windows + Visual Studio
2. **VS Code Support**: Limited via HTTP mode (full support in VUV extension)
3. **Platform Warnings**: 29 warnings for Windows-specific APIs

---

## 📋 Changelog (RC1 → RC2 → RC3)

### RC1 → RC2
- Fixed ~50 compilation errors
- Resolved duplicate type definitions
- Fixed FileInfo type ambiguity
- Fixed SecureExecutionContext naming
- Updated all versions to v26.2.0-rc2

### RC2 → RC3
- Fixed IVsConnectionManager DI registration
- Fixed version display (26.0.3 → 26.2.0-rc3)
- Added compiled binaries to release assets
- Cleaned repository with .gitignore

---

## 📝 License

MIT License - See LICENSE file

---

**Full Release**: https://github.com/StarsailsClover/UniversalVSMCP/releases/tag/v26.2.0-rc3
