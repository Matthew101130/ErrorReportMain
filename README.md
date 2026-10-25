# ErrorReportMain

一个 Windows 动态链接库（DLL），用于在程序中复刻经典 Windows 系统错误弹窗。
库本身不处理真实崩溃，只导出三个 C 接口，供调用方按统一风格弹出提示框。

- 语言标准：C++20（`/std:c++20`），C 头文件兼容 C17
- 目标平台：Win32 / x64，Unicode
- 导出方式：`extern "C"` + `__declspec(dllexport)`，无名称修饰
- 运行时依赖：仅 `KERNEL32.dll`、`USER32.dll`（静态 CRT）

## 接口

三个接口均返回 `MessageBoxW` 的返回值，参数为空指针或空字符串时使用内置默认文本，不会崩溃。

```c
int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll);
int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName,
              const wchar_t* ErrorCode, const wchar_t* Address);
int MEMORY_ERROR(const wchar_t* ErrorProgram, const wchar_t* Operate,
                 const wchar_t* Address, const wchar_t* CannotBe);
```

| 接口 | 复刻的对话框 | 按钮 | 返回值 |
|---|---|---|---|
| `DLL_MISSING_ERROR` | “无法启动此程序，因为计算机中丢失 xxx。” | 确定 | `IDOK` |
| `EXE_ERROR` | “应用程序发生异常 xxx，位置为 xxx。” | 确定 / 取消 | `IDOK` / `IDCANCEL` |
| `MEMORY_ERROR` | “xxx 指令引用的 xxx 内存。该内存不能为 xxx。” | 确定 / 取消 | `IDOK` / `IDCANCEL` |

对话框标题统一为 `<程序名> - <分类>`，分类分别是“系统错误”“应用程序错误”；程序名为空时显示“未知程序”。

### 参数说明

| 接口 | 参数 | 含义 | 为空时的默认值 |
|---|---|---|---|
| 全部 | `ErrorProgram` | 程序名，显示在标题栏 | `未知程序` |
| `DLL_MISSING_ERROR` | `MissingDll` | 缺失的模块（DLL）名 | `未知模块` |
| `EXE_ERROR` | `ErrorName` | 异常名称，例如 `0xC0000005` | `未知异常` |
| `EXE_ERROR` | `ErrorCode` | 异常代码或描述 | `未知代码` |
| `EXE_ERROR` | `Address` | 出错地址 | `未知地址` |
| `MEMORY_ERROR` | `Operate` | 指令地址 | `0x00000000` |
| `MEMORY_ERROR` | `Address` | 被引用的内存地址 | `0x00000000` |
| `MEMORY_ERROR` | `CannotBe` | 访问类型，通常为 `read` / `written` | `read` |

## 目录结构

```
ErrorReportMain/
├─ ErrorReportMain.slnx            # 解决方案（VS 的 XML 解决方案格式）
├─ ErrorReportMain.vcxproj         # DLL 工程
├─ include/                        # 公共接口，调用方只需包含这里
│  ├─ ErrorReport.h                # 总入口头文件
│  └─ ErrorReport/
│     ├─ Export.h                  # 导出/导入/静态宏
│     ├─ DLLMissingError.h
│     ├─ ExeError.h
│     └─ MemoryError.h
├─ src/                            # 内部实现
│  ├─ dllmain.cpp
│  ├─ DLLMissingError.cpp
│  ├─ ExeError.cpp
│  ├─ MemoryError.cpp
│  └─ Detail/
│     └─ MessageBoxError.h/.cpp    # 文本兜底、标题拼接与 MessageBoxW 封装
├─ pch.h / pch.cpp                 # 预编译头（仅内部使用）
├─ framework.h                     # windows.h 等系统头
└─ cpp.hint                        # VS 辅助提示
```

公共接口只依赖 `include/`，内部实现全部放在 `src/`，调用方无需接触预编译头。

## 构建

用 Visual Studio 打开 `ErrorReportMain.slnx`，选择 `Debug`/`Release` 与 `Win32`/`x64` 后生成。
`.slnx` 是较新的解决方案格式，需要支持它的 Visual Studio（VS 2022 17.14 及以上或 VS 2026）；也可以直接打开 `ErrorReportMain.vcxproj`。

项目不锁定具体工具集：VS 2022 默认使用 v143，VS 2026（`VisualStudioVersion` 为 18.0）默认使用 v145，可在命令行用 `/p:PlatformToolset=...` 覆盖。

命令行构建示例：

```powershell
msbuild ErrorReportMain.slnx /m /p:Configuration=Release /p:Platform=x64
```

产物位于 `x64\Release\`（Win32 对应 `Win32\Release\`）：

```
ErrorReportMain.dll   # 运行时需要
ErrorReportMain.lib   # 链接导入库时使用
ErrorReportMain.pdb   # 调试符号
```

源文件统一使用 UTF-8 编码，工程已开启 `/utf-8`，中文提示文本不会随系统区域设置变化。

## 使用

### 方式一：链接导入库

```cpp
#include <ErrorReport.h>

int main() {
    EXE_ERROR(L"Demo.exe", L"0xC0000005",
              L"Access violation", L"0x00007FF612340000");
    return 0;
}
```

在调用方工程中：

1. 包含路径添加 `ErrorReportMain/include`；
2. 链接 `ErrorReportMain.lib`；
3. 运行时把 `ErrorReportMain.dll` 放到可执行文件目录或 `PATH` 中；
4. 若调用方工程启用了预编译头，不要包含 `pch.h`，公共头文件与预编译头无关。

### 方式二：运行时加载

```cpp
#include <windows.h>

using DllMissingErrorFn = int (__cdecl*)(const wchar_t*, const wchar_t*);

HMODULE module = LoadLibraryW(L"ErrorReportMain.dll");
if (module != nullptr) {
    auto showError = reinterpret_cast<DllMissingErrorFn>(
        GetProcAddress(module, "DLL_MISSING_ERROR"));
    if (showError != nullptr) {
        showError(L"Demo.exe", L"MSVCP140.dll");
    }
    FreeLibrary(module);
}
```

> 导出函数使用默认的 `__cdecl` 调用约定，`GetProcAddress` 得到的函数指针需按 `__cdecl` 声明。C# 等语言可通过 `DllImport("ErrorReportMain.dll")` 并指定 `CharSet = CharSet.Unicode` 直接调用。

### C# 示例

```csharp
using System.Runtime.InteropServices;

internal static class ErrorReport
{
    [DllImport("ErrorReportMain.dll", CharSet = CharSet.Unicode)]
    internal static extern int DLL_MISSING_ERROR(string errorProgram, string missingDll);
}
```

## 设计说明

- 文本通过 `std::wstring` 拼接，不使用固定长度栈缓冲区，不存在截断或格式字符串不匹配问题。
- 所有参数均做空指针/空串保护，传入 `nullptr` 不会崩溃。
- 公共头文件不包含 `pch.h`，可安全地被外部工程包含（包括 C 源文件）。
- `ERRORREPORT_API` 根据 `ERRORREPORTMAIN_EXPORTS` / `ERRORREPORTMAIN_STATIC` 自动切换导出、导入或静态声明；仓库工程只生成 DLL，如需把实现直接编入调用方，可手动编译 `src/` 并定义 `ERRORREPORTMAIN_STATIC`。
- 使用静态 CRT（Debug 为 `/MTd`，Release 为 `/MT`），DLL 只依赖 `KERNEL32.dll` 和 `USER32.dll`，目标机器无需安装 VC++ 运行库。
- 工程开启 `/W4`、SDL 检查与多处理器编译，Debug/Release 均生成调试信息。
