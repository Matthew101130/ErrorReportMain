#pragma once

#include "ErrorReport/Export.h"

// 旧名兼容：接口已更名为 ExeError，旧名 EXE_ERROR 通过宏映射到新名，
// 引用旧名的源代码无需修改即可继续编译。
#ifndef EXE_ERROR
#  define EXE_ERROR ExeError
#endif

#ifdef __cplusplus
extern "C" {
#endif

// 显示“应用程序发生异常 ...，位置为 ...”的应用程序错误对话框。
//   ErrorProgram : 程序名，显示在标题栏。
//   ErrorName    : 异常名称，例如 0xC0000005。
//   ErrorCode    : 异常代码/描述。
//   Address      : 出错地址。
// 返回 MessageBoxW 的返回值（IDOK 或 IDCANCEL）。
ERRORREPORT_API int ExeError(const wchar_t* ErrorProgram, const wchar_t* ErrorName, const wchar_t* ErrorCode, const wchar_t* Address);

#ifdef __cplusplus
}
#endif
