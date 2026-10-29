#include "pch.h"

#include "ErrorReport/ExeError.h"

#include "Detail/MessageBoxError.h"

int EXE_ERROR(const wchar_t* ErrorProgram, const wchar_t* ErrorName, const wchar_t* ErrorCode, const wchar_t* Address) {
    using errorreport::detail::ShowErrorBox;
    using errorreport::detail::ShowFallbackErrorBox;
    using errorreport::detail::ToWString;

    try {
        std::wstring text = L"应用程序发生异常 ";
        text += ToWString(ErrorName, L"未知异常");
        text += L" (";
        text += ToWString(ErrorCode, L"未知代码");
        text += L")，位置为 ";
        text += ToWString(Address, L"未知地址");
        text += L"。\n\n要终止程序，请单击“确定”。\n要调试程序，请单击“取消”。";

        return ShowErrorBox(ErrorProgram, L"应用程序错误", text, MB_OKCANCEL | MB_ICONERROR);
    } catch (...) {
        // 例如内存不足；此时连详细文本都构造不出来，退回固定文本。
        return ShowFallbackErrorBox(MB_OK | MB_ICONERROR);
    }
}
