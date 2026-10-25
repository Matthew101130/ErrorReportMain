# ErrorReportMain

一个 Windows 动态链接库（DLL），用于在程序中复刻经典 Windows 系统错误弹窗。
库本身不处理真实崩溃，只提供三个 C 接口，供调用方按统一风格弹出提示框。

## 功能

| 接口 | 复刻的对话框 |
|---|---|
| `DLL_MISSING_ERROR` | “无法启动此程序，因为计算机中丢失 xxx。” |
| `EXE_ERROR` | “应用程序发生异常 xxx，位置为 xxx。” |
| `MEMORY_ERROR` | “xxx 指令引用的 xxx 内存。该内存不能为 xxx。” |

所有接口均为 Unicode（`wchar_t*`），通过 `extern "C"` 导出，可直接从 C/C++、Delphi、C#（P/Invoke）等语言调用。

## 目录结构

```
ErrorReportMain.sln
ErrorReportMain/
├─ include/
│  ├─ ErrorReport.h              # 总入口头文件
│  └─ ErrorReport/
│     ├─ Export.h                # 导出/导入宏
│     ├─ DLLMissingError.h
│     ├─ ExeError.h
│     └─ MemoryError.h
├─ src/
│  ├─ dllmain.cpp
│  ├─ DLLMissingError.cpp
│  ├─ ExeError.cpp
│  ├─ MemoryError.cpp
│  └─ Detail/
│     └─ MessageBoxError.h/.cpp  # 内部的文本组装与对话框封装
├─ pch.h / pch.cpp               # 预编译头
├─ framework.h                   # windows.h 等系统头
└─ ErrorReportMain.vcxproj
```

公共接口只依赖 `include/`，内部实现细节全部放在 `src/`，调用方无需接触预编译头。

## 构建

用 Visual Studio 打开 `ErrorReportMain.sln`，选择 `Debug`/`Release` 与 `Win32`/`x64` 后生成即可。
项目不锁定具体工具集，默认使用当前 Visual Studio 安装的工具集（VS2022 为 v143，VS2026 为 v145）。

命令行构建示例：

```powershell
msbuild ErrorReportMain.sln /m /p:Configuration=Release /p:Platform=x64
```

源文件统一使用 UTF-8 编码，项目已开启 `/utf-8`，因此中文提示文本不会随系统区域设置变化。

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
3. 运行时把 `ErrorReportMain.dll` 放到可执行文件目录或 `PATH` 中。

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

> 导出函数使用 `__cdecl` 调用约定，`GetProcAddress` 获得的函数指针需按 `__cdecl` 声明。

### 参数说明

| 参数 | 含义 |
|---|---|
| `ErrorProgram` | 程序名，显示在标题栏；为空时使用“未知程序” |
| `MissingDll` | 缺失的模块名 |
| `ErrorName` / `ErrorCode` / `Address` | 异常名称、异常代码、出错地址 |
| `Operate` / `Address` / `CannotBe` | 指令地址、被引用内存地址、访问类型（read/written） |

返回值即 `MessageBoxW` 的返回值：`IDOK` 或 `IDCANCEL`。

## 设计说明

- 文本通过 `std::wstring` 拼接，不使用固定长度栈缓冲区，不存在截断或格式字符串不匹配问题。
- 所有参数均做空指针/空串保护，传 `nullptr` 不会崩溃。
- 公共头文件不包含 `pch.h`，可安全地被外部工程包含（包括 C 源文件）。
- `ERRORREPORT_API` 根据 `ERRORREPORTMAIN_EXPORTS` / `ERRORREPORTMAIN_STATIC` 自动切换导出、导入或静态声明。
- 使用静态 CRT（`/MT`、`/MTd`），DLL 只依赖 `KERNEL32.dll` 和 `USER32.dll`，不要求目标机器安装 VC++ 运行库。
