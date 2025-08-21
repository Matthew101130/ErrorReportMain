#include "DLLMissingError.h"
#include "pch.h"

constexpr size_t strSize = 10000;

DLL_API int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"无法启动此程序，因为计算机中丢失 %s。尝试重新安装该程序以解决此问题。", MissingDll);
	swprintf_s(Title, L"%s - 系统错误", ErrorProgram);
	return MessageBox(nullptr, Text, Title, MB_OK + MB_ICONERROR);
};

