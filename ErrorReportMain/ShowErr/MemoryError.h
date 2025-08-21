#pragma once

#ifndef MemoryError_h

#define MemoryError_h

#include "pch.h"

DLL_API int MEMORY_ERROR(const wchar_t* ErrorProgram, const wchar_t* Operate, const wchar_t* Address, const wchar_t* CannotBe);

#endif
