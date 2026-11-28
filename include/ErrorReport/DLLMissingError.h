#pragma once

#include "ErrorReport/Export.h"

// 旧名兼容：接口已更名为 DllMissingError，旧名 DLL_MISSING_ERROR 通过宏映射到新名，
// 引用旧名的源代码无需修改即可继续编译。
#ifndef DLL_MISSING_ERROR
#  define DLL_MISSING_ERROR DllMissingError
#endif

#ifdef __cplusplus
extern "C" {
#endif

// 显示“无法启动此程序，因为计算机中丢失 xxx”的系统错误对话框。
//   ErrorProgram : 程序名，显示在标题栏。
//   MissingDll   : 缺失的模块（DLL）名。
// 返回 MessageBoxW 的返回值（IDOK）。
ERRORREPORT_API int DllMissingError(const wchar_t* ErrorProgram, const wchar_t* MissingDll);

#ifdef __cplusplus
}
#endif
