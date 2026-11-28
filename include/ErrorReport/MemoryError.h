#pragma once

#include "ErrorReport/Export.h"

// 旧名兼容：接口已更名为 MemoryError，旧名 MEMORY_ERROR 通过宏映射到新名，
// 引用旧名的源代码无需修改即可继续编译。
#ifndef MEMORY_ERROR
#  define MEMORY_ERROR MemoryError
#endif

#ifdef __cplusplus
extern "C" {
#endif

// 显示经典的“xxx 指令引用的 xxx 内存。该内存不能为 xxx”对话框。
//   ErrorProgram : 程序名，显示在标题栏。
//   Operate      : 指令地址。
//   Address      : 被引用的内存地址。
//   CannotBe     : 访问类型，通常为 read 或 written。
// 返回 MessageBoxW 的返回值（IDOK 或 IDCANCEL）。
ERRORREPORT_API int MemoryError(const wchar_t* ErrorProgram, const wchar_t* Operate, const wchar_t* Address, const wchar_t* CannotBe);

#ifdef __cplusplus
}
#endif
