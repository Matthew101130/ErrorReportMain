#include "pch.h"

#include "Detail/MessageBoxError.h"

namespace errorreport::detail {
namespace {

constexpr const wchar_t* kUnknownProgram = L"未知程序";

} // namespace

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
    return ::MessageBoxW(nullptr, text.c_str(), title.c_str(), flags);
}

} // namespace errorreport::detail
