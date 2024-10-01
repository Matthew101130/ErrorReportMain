// pch.h: 这是预编译标头文件。
// 下方列出的文件仅编译一次，提高了将来生成的生成性能。
// 这还将影响 IntelliSense 性能，包括代码完成和许多代码浏览功能。
// 但是，如果此处列出的文件中的任何一个在生成之间有更新，它们全部都将被重新编译。
// 请勿在此处添加要频繁更新的文件，这将使得性能优势无效。

#ifndef PCH_H
#define PCH_H

// 添加要在此处预编译的标头
#include "framework.h"

#endif //PCH_H


#ifndef IMPORT_DLL
#define IMPORT_DLL extern "C" _declspec(dllimport)
#endif

#include <string.h>
#include <stdio.h>
#include <tchar.h>

constexpr int strSize = 10000;

IMPORT_DLL int MEMORY_ERROR(const wchar_t* ErrorProgram, const wchar_t* Operate, const wchar_t* Address, const wchar_t* CannotBe);
IMPORT_DLL int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll);
IMPORT_DLL int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName, const wchar_t* ErrorCode, const wchar_t* Address);
