#include "ExeError.h"
#include "pch.h"

constexpr size_t strSize = 10000;

DLL_API int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName, const wchar_t* ErrorCode, const wchar_t* Address) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"Ӧ�ó������쳣 %s (%s)��λ��Ϊ %s��\n\nҪ��ֹ�����뵥����ȷ������\nҪ���Գ����뵥����ȡ������", ErrorName, ErrorCode, Address);
	swprintf_s(Title, L"%s - Ӧ�ó������", ErrorProgram);
	return MessageBox(nullptr, Text, Title, MB_OKCANCEL + MB_ICONERROR);
};
