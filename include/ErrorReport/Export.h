#pragma once

// C 语言下 wchar_t 由 <stddef.h> 定义；C++ 中为内建类型。
#include <stddef.h>

// 导出/导入宏：
//   - 编译本 DLL 时，项目已定义 ERRORREPORTMAIN_EXPORTS。函数按普通 C 链接定义，
//     由工程中的 ErrorReportMain.def 以不带修饰的名字导出（x86/x64 一致）。
//     如果不用本工程的 .def，需要自行提供等效的导出机制（例如 .def 或
//     __declspec(dllexport)），否则 DLL 不会导出这些函数。
//   - 其他工程链接本 DLL 的导入库时，自动按 __declspec(dllimport) 声明；
//   - 若把实现直接静态编译进调用方，定义 ERRORREPORTMAIN_STATIC 即可。
#if defined(ERRORREPORTMAIN_STATIC) || defined(ERRORREPORTMAIN_EXPORTS)
#  define ERRORREPORT_API
#else
#  define ERRORREPORT_API __declspec(dllimport)
#endif
