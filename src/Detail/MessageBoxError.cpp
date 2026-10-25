#include "pch.h"

#include "Detail/MessageBoxError.h"

namespace errorreport::detail {
namespace {

constexpr const wchar_t* kUnknownProgram = L"未知程序";
constexpr const wchar_t* kFallbackText = L"发生错误，但无法显示详细信息。";
constexpr const wchar_t* kFallbackCategory = L"应用程序错误";

} // namespace

HWND GetErrorMessageOwner() noexcept {
    if (HWND active = ::GetActiveWindow(); active != nullptr) {
        return active;
    }

    const HWND foreground = ::GetForegroundWindow();
    if (foreground == nullptr) {
        return nullptr;
    }

    DWORD processId = 0;
    ::GetWindowThreadProcessId(foreground, &processId);
    return processId == ::GetCurrentProcessId() ? foreground : nullptr;
}

std::wstring ToWString(const wchar_t* value, const wchar_t* fallback) {
    if (value != nullptr && value[0] != L'\0') {
        return std::wstring(value);
    }
    return std::wstring(fallback != nullptr ? fallback : L"");
}

std::wstring MakeErrorTitle(const wchar_t* errorProgram, const wchar_t* category) {
    std::wstring title = ToWString(errorProgram, kUnknownProgram);
    title += L" - ";
    title += ToWString(category, L"错误");
    return title;
}

int ShowErrorBox(const wchar_t* errorProgram, const wchar_t* category, const std::wstring& text, unsigned int flags) {
    const std::wstring title = MakeErrorTitle(errorProgram, category);
    return ::MessageBoxW(GetErrorMessageOwner(), text.c_str(), title.c_str(), flags);
}

int ShowFallbackErrorBox(unsigned int flags) noexcept {
    return ::MessageBoxW(GetErrorMessageOwner(), kFallbackText, kFallbackCategory, flags);
}

} // namespace errorreport::detail
