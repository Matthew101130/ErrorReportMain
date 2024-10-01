int MEMORY_ERROR(const wchar_t *ErrorProgram, const wchar_t *Operate, const wchar_t *Address, const wchar_t *CannotBe) {
	wchar_t Text[strSize], Title[strSize];
	swprintf_s(Text, L"\"%s\" 指令引用的 \"%s\" 内存。该内存不能为 \"%s\"。\n\n要终止程序，请单击“确定”。\n要调试程序，请单击“取消”。", Operate, Address, CannotBe);
	swprintf_s(Title, L"%s - 应用程序错误", ErrorProgram);
	return MessageBox(NULL, Text, Title, MB_OKCANCEL + MB_ICONERROR);
}
