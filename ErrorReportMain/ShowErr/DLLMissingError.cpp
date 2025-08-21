#include "DLLMissingError.h"
#include "pch.h"

constexpr size_t strSize = 10000;

DLL_API int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"�޷������˳�����Ϊ������ж�ʧ %s���������°�װ�ó����Խ�������⡣", MissingDll);
	swprintf_s(Title, L"%s - ϵͳ����", ErrorProgram);
	return MessageBox(nullptr, Text, Title, MB_OK + MB_ICONERROR);
};

