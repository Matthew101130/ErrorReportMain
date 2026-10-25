#pragma once

// C 语言下 wchar_t 由 <stddef.h> 定义；C++ 中为内建类型。
#include <stddef.h>

// 导出/导入宏：
//   - 编译本 DLL 时，项目已定义 ERRORREPORTMAIN_EXPORTS，函数被导出；
//   - 其他工程链接本 DLL 的导入库时，自动按 __declspec(dllimport) 声明；
//   - 若把实现直接静态编译进调用方，定义 ERRORREPORTMAIN_STATIC 即可。
#if defined(ERRORREPORTMAIN_STATIC)
#  define ERRORREPORT_API
#elif defined(ERRORREPORTMAIN_EXPORTS)
#  define ERRORREPORT_API __declspec(dllexport)
#else
#  define ERRORREPORT_API __declspec(dllimport)
#endif
