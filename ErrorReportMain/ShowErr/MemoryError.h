int MEMORY_ERROR(const wchar_t *ErrorProgram, const wchar_t *Operate, const wchar_t *Address, const wchar_t *CannotBe) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"\"%s\" ָ�����õ� \"%s\" �ڴ档���ڴ治��Ϊ \"%s\"��\n\nҪ��ֹ�����뵥����ȷ������\nҪ���Գ����뵥����ȡ������", Operate, Address, CannotBe);
	swprintf_s(Title, L"%s - Ӧ�ó������", ErrorProgram);
	return MessageBox(NULL, Text, Title, MB_OKCANCEL + MB_ICONERROR);
}
