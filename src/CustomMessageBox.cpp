#include "pch.h"

#include "ErrorReport/CustomMessageBox.h"

#include "Detail/MessageBoxError.h"

namespace {

std::wstring Trim(const std::wstring& value) {
    const size_t begin = value.find_first_not_of(L" \t\r\n");
    if (begin == std::wstring::npos) {
        return {};
    }
    const size_t end = value.find_last_not_of(L" \t\r\n");
    return value.substr(begin, end - begin + 1);
}

unsigned int ParseIcon(const std::wstring& icon) noexcept {
    if (icon.empty() || _wcsicmp(icon.c_str(), L"none") == 0) {
        return 0;
    }
    if (_wcsicmp(icon.c_str(), L"error") == 0) {
        return MB_ICONERROR;
    }
    if (_wcsicmp(icon.c_str(), L"warning") == 0) {
        return MB_ICONWARNING;
    }
    if (_wcsicmp(icon.c_str(), L"info") == 0 || _wcsicmp(icon.c_str(), L"information") == 0) {
        return MB_ICONINFORMATION;
    }
    if (_wcsicmp(icon.c_str(), L"question") == 0) {
        return MB_ICONQUESTION;
    }
    return 0;
}

bool ParseButtons(const std::wstring& spec, unsigned int& flags) noexcept {
    flags = 0;
    if (spec.empty()) {
        flags = MB_OK;
        return true;
    }

    bool hasSet = false;
    bool hasHelp = false;
    size_t start = 0;
    while (start <= spec.size()) {
        const size_t end = spec.find(L'|', start);
        const std::wstring token = Trim(spec.substr(
            start, end == std::wstring::npos ? std::wstring::npos : end - start));
        start = end == std::wstring::npos ? spec.size() + 1 : end + 1;

        if (token.empty()) {
            continue;
        }
        if (_wcsicmp(token.c_str(), L"help") == 0) {
            if (hasHelp) {
                return false;
            }
            hasHelp = true;
            continue;
        }
        if (hasSet) {
            return false;
        }

        if (_wcsicmp(token.c_str(), L"ok") == 0) {
            flags = MB_OK;
        } else if (_wcsicmp(token.c_str(), L"okcancel") == 0) {
            flags = MB_OKCANCEL;
        } else if (_wcsicmp(token.c_str(), L"abortretryignore") == 0) {
            flags = MB_ABORTRETRYIGNORE;
        } else if (_wcsicmp(token.c_str(), L"yesnocancel") == 0) {
            flags = MB_YESNOCANCEL;
        } else if (_wcsicmp(token.c_str(), L"yesno") == 0) {
            flags = MB_YESNO;
        } else if (_wcsicmp(token.c_str(), L"retrycancel") == 0) {
            flags = MB_RETRYCANCEL;
        } else if (_wcsicmp(token.c_str(), L"canceltrycontinue") == 0) {
            flags = MB_CANCELTRYCONTINUE;
        } else {
            return false;
        }
        hasSet = true;
    }

    if (!hasSet) {
        flags = MB_OK;
    }
    if (hasHelp) {
        flags |= MB_HELP;
    }
    return true;
}

} // namespace

int CUSTOM_MESSAGEBOX(const wchar_t* Title,
                      const wchar_t* Text,
                      const wchar_t* Icon,
                      const wchar_t* Buttons) {
    using errorreport::detail::GetErrorMessageOwner;
    using errorreport::detail::ShowFallbackErrorBox;
    using errorreport::detail::ToWString;

    try {
        const std::wstring title = ToWString(Title, L"错误");
        const std::wstring text = ToWString(Text, L"");
        const std::wstring icon = Trim(ToWString(Icon, L"none"));
        const std::wstring buttons = Trim(ToWString(Buttons, L"ok"));

        unsigned int flags = 0;
        if (!ParseButtons(buttons, flags)) {
            return -2;
        }
        flags |= ParseIcon(icon);

        return ::MessageBoxW(GetErrorMessageOwner(), text.c_str(), title.c_str(), flags);
    } catch (...) {
        // 内存不足等情况下退回固定文本，避免 C++ 异常穿过 C 接口。
        return ShowFallbackErrorBox(MB_OK | MB_ICONERROR);
    }
}
