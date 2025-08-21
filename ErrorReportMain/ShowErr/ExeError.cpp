#include "ExeError.h"
#include "pch.h"

constexpr size_t strSize = 10000;

DLL_API int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName, const wchar_t* ErrorCode, const wchar_t* Address) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"应用程序发生异常 %s (%s)，位置为 %s。\n\n要终止程序，请单击“确定”。\n要调试程序，请单击“取消”。", ErrorName, ErrorCode, Address);
	swprintf_s(Title, L"%s - 应用程序错误", ErrorProgram);
	return MessageBox(nullptr, Text, Title, MB_OKCANCEL + MB_ICONERROR);
};
