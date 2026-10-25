# ErrorReportMain

一个 Windows 动态链接库（DLL），用于在程序中复刻经典 Windows 系统错误弹窗。
库本身不处理真实崩溃，只导出三个 C 接口，供调用方按统一风格弹出提示框。

- 语言标准：C++20（`/std:c++20`），C 头文件兼容 C17
- 目标平台：Win32 / x64，Unicode（解决方案平台写作 x86，工程平台写作 Win32）
- 导出方式：`extern "C"` + `ErrorReportMain.def`，三个导出名在 x86/x64 下都无名称修饰
- 运行时依赖：仅 `KERNEL32.dll`、`USER32.dll`（静态 CRT）

## 接口

三个接口均返回 `MessageBoxW` 的返回值，参数为空指针或空字符串时使用内置默认文本，不会崩溃。
内存不足等极端情况下会退回固定文本提示，不会让 C++ 异常穿越 C 接口。

```c
int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll);
int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName,
              const wchar_t* ErrorCode, const wchar_t* Address);
int MEMORY_ERROR(const wchar_t* ErrorProgram, const wchar_t* Operate,
                 const wchar_t* Address, const wchar_t* CannotBe);
```

| 接口 | 复刻的对话框 | 按钮 | 返回值 |
|---|---|---|---|
| `DLL_MISSING_ERROR` | “无法启动此程序，因为计算机中丢失 xxx。尝试重新安装该程序以解决此问题。” | 确定 | `IDOK` |
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
├─ ErrorReportMain.def             # 模块定义文件，固定三个无修饰导出名
├─ CppProperties.json              # VS“打开文件夹”模式的 IntelliSense 配置
├─ .gitignore
├─ include/                        # 公共接口，调用方只需包含这里
│  ├─ ErrorReport.h                # 总入口头文件
│  └─ ErrorReport/
│     ├─ Export.h                  # 导出/导入/静态宏
│     ├─ DLLMissingError.h
│     ├─ ExeError.h
│     └─ MemoryError.h
├─ src/                            # 内部实现
│  ├─ dllmain.cpp                  # DLL 入口点（空实现）
│  ├─ DLLMissingError.cpp
│  ├─ ExeError.cpp
│  ├─ MemoryError.cpp
│  └─ Detail/
│     └─ MessageBoxError.h/.cpp    # 文本兜底、标题拼接、owner 选择与 MessageBoxW 封装
├─ pch.h / pch.cpp                 # 预编译头（仅内部使用）
├─ framework.h                     # windows.h 等系统头
└─ cpp.hint                        # VS 辅助提示
```

公共接口只依赖 `include/`，内部实现全部放在 `src/`，调用方无需接触预编译头。
`ErrorReportMain.def` 是导出名稳定的关键：它把三个 `extern "C"` 函数以无修饰名字导出，
避免 x86 下 `__cdecl` 的 `_DLL_MISSING_ERROR` 之类修饰名进入导出表。

## 构建

用 Visual Studio 打开 `ErrorReportMain.slnx`，选择 `Debug`/`Release` 与 `x86`/`x64` 后生成。
`.slnx` 是较新的解决方案格式，需要支持它的 Visual Studio（VS 2022 17.14 及以上或 VS 2026）；也可以直接打开 `ErrorReportMain.vcxproj`。

解决方案里的平台名是 `x86`/`x64`，工程里的平台名是 `Win32`/`x64`，二者对应同一个 Win32 目标。在 VS 的配置下拉框中选择 `x86` 即构建 Win32；命令行直接构建工程时则要写 `Win32`：

```powershell
# x64（解决方案）
msbuild ErrorReportMain.slnx /m /p:Configuration=Release /p:Platform=x64

# Win32（直接构建工程；解决方案模式请按 .slnx 中实际列出的平台名传参）
msbuild ErrorReportMain.vcxproj /m /p:Configuration=Release /p:Platform=Win32
```

项目不锁定具体工具集：VS 2022 默认使用 v143，VS 2026（`VisualStudioVersion` 为 18.0）默认使用 v145，可在命令行用 `/p:PlatformToolset=...` 覆盖。

导出名由 `ErrorReportMain.def` 固定，构建后可以在 Developer Command Prompt / Developer PowerShell 中验证：

```powershell
dumpbin /exports x64\Release\ErrorReportMain.dll
dumpbin /exports Win32\Release\ErrorReportMain.dll
```

两个产物都应列出 `DLL_MISSING_ERROR`、`EXE_ERROR`、`MEMORY_ERROR`。x86 的输出会写成
`DLL_MISSING_ERROR = _DLL_MISSING_ERROR`：等号左边是导出名（无修饰），右边是 DLL 内部的
C 修饰符号；判断导出名时以等号左边为准。

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

> 公共头文件使用 UTF-8 编码。调用方若使用 MSVC 且源码页不是 UTF-8，建议在工程中开启 `/utf-8`；否则包含这些头文件时可能看到 C4819 警告（只是注释编码提示，不影响生成）。

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

> 导出函数使用默认的 `__cdecl` 调用约定，`GetProcAddress` 得到的函数指针需按 `__cdecl` 声明。导出名由 `.def` 固定，x86/x64 都可以直接用 `DLL_MISSING_ERROR` 等名字查找。C# 等语言可通过 `DllImport` 调用；注意同时设置 `ExactSpelling = true` 和 `CallingConvention = CallingConvention.Cdecl`，否则 CLR 会查找 `...W` 后缀，且 x86 下默认的 StdCall 会破坏栈。

### C# 示例

```csharp
using System.Runtime.InteropServices;

internal static class ErrorReport
{
    [DllImport("ErrorReportMain.dll",
               CharSet = CharSet.Unicode,
               ExactSpelling = true,
               CallingConvention = CallingConvention.Cdecl)]
    internal static extern int DLL_MISSING_ERROR(string errorProgram, string missingDll);
}
```

## 设计说明

- 文本通过 `std::wstring` 拼接，不使用固定长度栈缓冲区，不存在截断或格式字符串不匹配问题。
- 所有参数均做空指针/空串保护，传入 `nullptr` 不会崩溃。
- 公共头文件不包含 `pch.h`，可安全地被外部工程包含（包括 C 源文件）。
- 导出名由 `ErrorReportMain.def` 固定；构建 DLL 时 `ERRORREPORT_API` 为空，链接 DLL 时为 `__declspec(dllimport)`，静态编译时为空。仓库工程只生成 DLL，如需把实现直接编入调用方，可手动编译 `src/` 并定义 `ERRORREPORTMAIN_STATIC`（不需要 `.def`）。迁移到其他构建系统时，记得保留 `.def` 或改用 `__declspec(dllexport)`。
- 消息框优先挂到调用线程的活动窗口；没有活动窗口时，只接受同进程的前台窗口，避免对话框被其他窗口遮挡或错误绑定到其他进程。
- 导出函数捕获所有 C++ 异常；内存不足等情况下退回固定文本，不让异常穿越 C 接口。
- 使用静态 CRT（Debug 为 `/MTd`，Release 为 `/MT`），DLL 只依赖 `KERNEL32.dll` 和 `USER32.dll`，目标机器无需安装 VC++ 运行库。
- 工程开启 `/W4`、SDL 检查与多处理器编译，Debug/Release 均生成调试信息。

## 已知限制

- 本库只负责弹窗，不捕获真实崩溃、不生成 dump，也不判断进程是否真的处于异常状态。
- `MessageBoxW` 在 Windows 10/11 上使用系统主题，文本和按钮能复刻，但视觉样式不会与 XP 经典弹窗逐像素一致；如果要求像素级复刻，需要自绘对话框。
- 提示文本目前是简体中文硬编码，没有资源 DLL 或多语言切换。
- 仓库没有自动化测试或 demo 宿主；`dumpbin` 验证和实际弹框行为需要手工检查。
