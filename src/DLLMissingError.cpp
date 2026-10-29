#include "pch.h"

#include "ErrorReport/DLLMissingError.h"

#include "Detail/MessageBoxError.h"

int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll) {
    using errorreport::detail::ShowErrorBox;
    using errorreport::detail::ShowFallbackErrorBox;
    using errorreport::detail::ToWString;

    try {
        std::wstring text = L"无法启动此程序，因为计算机中丢失 ";
        text += ToWString(MissingDll, L"未知模块");
        text += L"。尝试重新安装该程序以解决此问题。";

        return ShowErrorBox(ErrorProgram, L"系统错误", text, MB_OK | MB_ICONERROR);
    } catch (...) {
        // 例如内存不足；此时连详细文本都构造不出来，退回固定文本。
        return ShowFallbackErrorBox(MB_OK | MB_ICONERROR);
    }
}
