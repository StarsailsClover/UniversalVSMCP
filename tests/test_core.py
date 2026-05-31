"""测试 UniversalVSMCP 核心模块"""

import sys
import os

# 添加 src 到路径
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))

def test_imports():
    """测试基本导入"""
    print("Testing imports...")
    
    try:
        # 测试 Python 服务器模块导入
        from universal_vsmcp.server import (
            VsConnectionManager, 
            VSTools, 
            ProjectInfo,
            OperationResult,
            FileReadResult,
            BuildResult
        )
        print("  ✓ Python server modules imported successfully")
        
        # 测试数据模型
        info = ProjectInfo(
            name="TestProject",
            kind="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}",
            kind_name="C# Project",
            full_path="C:\\Test\\TestProject.csproj",
            unique_name="TestProject",
            is_dirty=False,
            build_state="Debug"
        )
        print(f"  ✓ ProjectInfo created: {info.name}")
        
        result = OperationResult.success("Test")
        assert result.success == True
        print("  ✓ OperationResult works")
        
        file_result = FileReadResult(
            success=True,
            file_path="test.cs",
            content="class Test {}",
            total_lines=1,
            read_lines=1,
            start_line=1,
            end_line=1
        )
        print(f"  ✓ FileReadResult created: {file_result.file_path}")
        
        build_result = BuildResult(
            success=True,
            project_name="TestProject",
            configuration="Debug",
            platform="Any CPU",
            output="Build succeeded",
            error_count=0
        )
        print(f"  ✓ BuildResult created: {build_result.success}")
        
    except ImportError as e:
        print(f"  ✗ Import failed: {e}")
        return False
    except Exception as e:
        print(f"  ✗ Error: {e}")
        return False
    
    return True


def test_vs_connection_manager():
    """测试 VS 连接管理器"""
    print("\nTesting VsConnectionManager...")
    
    try:
        from universal_vsmcp.server import VsConnectionManager
        
        manager = VsConnectionManager()
        assert not manager.is_connected
        print("  ✓ VsConnectionManager instantiated")
        print(f"    - is_connected: {manager.is_connected}")
        print(f"    - connected_version: {manager.connected_version}")
        
    except Exception as e:
        print(f"  ✗ Error: {e}")
        return False
    
    return True


def test_vs_tools():
    """测试 VS 工具集"""
    print("\nTesting VSTools...")
    
    try:
        from universal_vsmcp.server import VsConnectionManager, VSTools
        
        manager = VsConnectionManager()
        tools = VSTools(manager)
        
        # 测试未连接状态下的错误处理
        try:
            tools.get_solution_projects()
            print("  ✗ Should have raised RuntimeError")
            return False
        except RuntimeError as e:
            print(f"  ✓ Correctly raises RuntimeError when not connected: {e}")
        
    except Exception as e:
        print(f"  ✗ Error: {e}")
        return False
    
    return True


def test_config_templates():
    """测试配置模板"""
    print("\nTesting config templates...")
    
    try:
        # 读取并验证模板文件
        template_path = os.path.join(os.path.dirname(__file__), '..', 'config_templates.py')
        with open(template_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        assert 'mcpServers' in content
        assert 'uvx' in content
        assert 'stdio' in content
        print("  ✓ Config templates are valid")
        print(f"    - Contains mcpServers: True")
        print(f"    - Contains uvx: True")
        print(f"    - Contains stdio: True")
        
    except Exception as e:
        print(f"  ✗ Error: {e}")
        return False
    
    return True


def run_all_tests():
    """运行所有测试"""
    print("=" * 60)
    print("UniversalVSMCP 单元测试")
    print("=" * 60)
    
    results = []
    results.append(("Imports", test_imports()))
    results.append(("VsConnectionManager", test_vs_connection_manager()))
    results.append(("VSTools", test_vs_tools()))
    results.append(("Config Templates", test_config_templates()))
    
    print("\n" + "=" * 60)
    print("测试结果摘要")
    print("=" * 60)
    
    passed = sum(1 for _, result in results if result)
    total = len(results)
    
    for name, result in results:
        status = "✓ PASS" if result else "✗ FAIL"
        print(f"  {status}: {name}")
    
    print(f"\n总计: {passed}/{total} 通过")
    
    if passed == total:
        print("\n🎉 所有测试通过！")
        return 0
    else:
        print("\n⚠️ 部分测试失败")
        return 1


if __name__ == "__main__":
    sys.exit(run_all_tests())
