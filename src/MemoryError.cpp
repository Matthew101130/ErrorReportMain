#include "pch.h"

#include "ErrorReport/MemoryError.h"

#include "Detail/MessageBoxError.h"

int MEMORY_ERROR(const wchar_t* ErrorProgram, const wchar_t* Operate, const wchar_t* Address, const wchar_t* CannotBe) {
    using errorreport::detail::ToWString;
    using errorreport::detail::ShowErrorBox;

    std::wstring text = L"\"";
    text += ToWString(Operate, L"0x00000000");
    text += L"\" 指令引用的 \"";
    text += ToWString(Address, L"0x00000000");
    text += L"\" 内存。该内存不能为 \"";
    text += ToWString(CannotBe, L"read");
    text += L"\"。\n\n要终止程序，请单击“确定”。\n要调试程序，请单击“取消”。";

    return ShowErrorBox(ErrorProgram, L"应用程序错误", text, MB_OKCANCEL | MB_ICONERROR);
}
