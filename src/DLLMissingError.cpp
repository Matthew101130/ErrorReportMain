#include "pch.h"

#include "ErrorReport/DLLMissingError.h"

#include "Detail/MessageBoxError.h"

int DLL_MISSING_ERROR(const wchar_t* ErrorProgram, const wchar_t* MissingDll) {
    using errorreport::detail::ToWString;
    using errorreport::detail::ShowErrorBox;

    std::wstring text = L"无法启动此程序，因为计算机中丢失 ";
    text += ToWString(MissingDll, L"未知模块");
    text += L"。尝试重新安装该程序以解决此问题。";

    return ShowErrorBox(ErrorProgram, L"系统错误", text, MB_OK | MB_ICONERROR);
}
