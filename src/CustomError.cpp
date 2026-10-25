#include "pch.h"

#include "ErrorReport/CustomError.h"

#include "Detail/MessageBoxError.h"


namespace {

constexpr int kBaseDpi = 96;
constexpr int kButtonIdBase = 1000;
constexpr int kMaxButtons = 8;

enum class IconKind {
    None,
    Error,
    Warning,
    Information,
    Question,
};

struct CustomErrorState {
    std::wstring text;
    std::vector<std::wstring> buttons;
    IconKind iconKind = IconKind::None;
    HFONT font = nullptr;
    bool fontOwned = false;
    bool hideClose = false;
    bool completed = false;
};

int Scale(int value, UINT dpi) noexcept {
    return ::MulDiv(value, static_cast<int>(dpi), kBaseDpi);
}

UINT GetDpiForWindowSafe(HWND hwnd) noexcept {
    using GetDpiForWindowFn = UINT (WINAPI*)(HWND);
    static const GetDpiForWindowFn getDpiForWindow = []() noexcept -> GetDpiForWindowFn {
        const HMODULE user32 = ::GetModuleHandleW(L"user32.dll");
        if (user32 == nullptr) {
            return nullptr;
        }
        return reinterpret_cast<GetDpiForWindowFn>(::GetProcAddress(user32, "GetDpiForWindow"));
    }();

    if (getDpiForWindow != nullptr) {
        return getDpiForWindow(hwnd);
    }

    HDC dc = ::GetDC(hwnd);
    if (dc == nullptr) {
        return kBaseDpi;
    }
    const int dpi = ::GetDeviceCaps(dc, LOGPIXELSX);
    ::ReleaseDC(hwnd, dc);
    return dpi > 0 ? static_cast<UINT>(dpi) : kBaseDpi;
}

BOOL AdjustWindowRectForDpi(RECT* rect, DWORD style, DWORD exStyle, UINT dpi) noexcept {
    using AdjustWindowRectExForDpiFn = BOOL (WINAPI*)(LPRECT, DWORD, BOOL, DWORD, UINT);
    static const AdjustWindowRectExForDpiFn adjustWindowRect = []() noexcept -> AdjustWindowRectExForDpiFn {
        const HMODULE user32 = ::GetModuleHandleW(L"user32.dll");
        if (user32 == nullptr) {
            return nullptr;
        }
        return reinterpret_cast<AdjustWindowRectExForDpiFn>(
            ::GetProcAddress(user32, "AdjustWindowRectExForDpi"));
    }();

    if (adjustWindowRect != nullptr) {
        return adjustWindowRect(rect, style, FALSE, exStyle, dpi);
    }
    return ::AdjustWindowRectEx(rect, style, FALSE, exStyle);
}

IconKind ParseIconKind(const wchar_t* icon) noexcept {
    if (icon == nullptr || icon[0] == L'\0') {
        return IconKind::None;
    }
    if (_wcsicmp(icon, L"error") == 0) {
        return IconKind::Error;
    }
    if (_wcsicmp(icon, L"warning") == 0) {
        return IconKind::Warning;
    }
    if (_wcsicmp(icon, L"info") == 0 || _wcsicmp(icon, L"information") == 0) {
        return IconKind::Information;
    }
    if (_wcsicmp(icon, L"question") == 0) {
        return IconKind::Question;
    }
    return IconKind::None;
}

LPCWSTR IconResource(IconKind kind) noexcept {
    switch (kind) {
    case IconKind::Error:
        return IDI_ERROR;
    case IconKind::Warning:
        return IDI_WARNING;
    case IconKind::Information:
        return IDI_INFORMATION;
    case IconKind::Question:
        return IDI_QUESTION;
    case IconKind::None:
    default:
        return nullptr;
    }
}

HICON LoadIconForDpi(IconKind kind, int width, int height) noexcept {
    const LPCWSTR resource = IconResource(kind);
    if (resource == nullptr || width <= 0 || height <= 0) {
        return nullptr;
    }

    HICON icon = reinterpret_cast<HICON>(
        ::LoadImageW(nullptr, resource, IMAGE_ICON, width, height, LR_SHARED));
    if (icon == nullptr) {
        icon = ::LoadIconW(nullptr, resource);
    }
    return icon;
}

std::wstring TrimLabel(const std::wstring& value) {
    const size_t begin = value.find_first_not_of(L" \t\r\n");
    if (begin == std::wstring::npos) {
        return {};
    }
    const size_t end = value.find_last_not_of(L" \t\r\n");
    return value.substr(begin, end - begin + 1);
}

std::vector<std::wstring> ParseButtons(const wchar_t* buttons) {
    std::vector<std::wstring> result;
    if (buttons != nullptr) {
        std::wstring current;
        for (const wchar_t* cursor = buttons;; ++cursor) {
            if (*cursor == L'|' || *cursor == L'\0') {
                std::wstring label = TrimLabel(current);
                if (!label.empty()) {
                    result.push_back(std::move(label));
                }
                current.clear();
                if (*cursor == L'\0') {
                    break;
                }
            } else {
                current.push_back(*cursor);
            }
        }
    }

    if (result.empty()) {
        result.emplace_back(L"确定");
    }
    return result;
}

HFONT CreateMessageFont(bool& owned) noexcept {
    NONCLIENTMETRICSW metrics{};
    metrics.cbSize = sizeof(metrics);
    if (::SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(metrics), &metrics, 0) != FALSE) {
        HFONT font = ::CreateFontIndirectW(&metrics.lfMessageFont);
        if (font != nullptr) {
            owned = true;
            return font;
        }
    }

    owned = false;
    return reinterpret_cast<HFONT>(::GetStockObject(DEFAULT_GUI_FONT));
}

std::vector<BYTE> BuildDialogTemplate(const std::wstring& title, bool hideClose) {
    const size_t titleBytes = (title.size() + 1) * sizeof(wchar_t);
    const size_t templateBytes = sizeof(DLGTEMPLATE) + 2 * sizeof(WORD) + titleBytes;
    std::vector<BYTE> buffer(templateBytes, 0);

    auto* dialog = reinterpret_cast<DLGTEMPLATE*>(buffer.data());
    dialog->style = WS_POPUP | WS_CAPTION | WS_CLIPCHILDREN | DS_MODALFRAME;
    if (!hideClose) {
        dialog->style |= WS_SYSMENU;
    }
    dialog->dwExtendedStyle = 0;
    dialog->cdit = 0;
    dialog->x = 0;
    dialog->y = 0;
    dialog->cx = 100;
    dialog->cy = 100;

    BYTE* cursor = buffer.data() + sizeof(DLGTEMPLATE);
    *reinterpret_cast<WORD*>(cursor) = 0; // menu
    cursor += sizeof(WORD);
    *reinterpret_cast<WORD*>(cursor) = 0; // window class
    cursor += sizeof(WORD);
    std::memcpy(cursor, title.c_str(), titleBytes);
    return buffer;
}

void RemoveCloseButton(HWND hDlg) noexcept {
    LONG_PTR style = ::GetWindowLongPtrW(hDlg, GWL_STYLE);
    if ((style & WS_SYSMENU) != 0) {
        style &= ~static_cast<LONG_PTR>(WS_SYSMENU);
        ::SetWindowLongPtrW(hDlg, GWL_STYLE, style);
    }

    if (HMENU systemMenu = ::GetSystemMenu(hDlg, FALSE); systemMenu != nullptr) {
        ::DeleteMenu(systemMenu, SC_CLOSE, MF_BYCOMMAND);
    }

    ::SetWindowPos(hDlg, nullptr, 0, 0, 0, 0,
                   SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
}

// 只读多行 EDIT 默认会吞掉 Enter/Esc/Tab；这里转发给对话框，保持默认按钮和
// 键盘焦点切换的行为。
LRESULT CALLBACK EditSubclassProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) {
    const auto oldProc = reinterpret_cast<WNDPROC>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    if (message == WM_KEYDOWN) {
        if (wParam == VK_RETURN || wParam == VK_ESCAPE) {
            const int commandId = (wParam == VK_ESCAPE) ? IDCANCEL : IDOK;
            ::PostMessageW(::GetParent(hWnd), WM_COMMAND, MAKEWPARAM(commandId, 0), 0);
            return 0;
        }
        if (wParam == VK_TAB) {
            const bool previous = (::GetKeyState(VK_SHIFT) < 0);
            ::PostMessageW(::GetParent(hWnd), WM_NEXTDLGCTL, previous ? 1 : 0, FALSE);
            return 0;
        }
    } else if (message == WM_CHAR) {
        if (wParam == L'\r' || wParam == L'\n' || wParam == L'\t' || wParam == 0x1B) {
            return 0;
        }
    }

    if (oldProc != nullptr) {
        return ::CallWindowProcW(oldProc, hWnd, message, wParam, lParam);
    }
    return ::DefWindowProcW(hWnd, message, wParam, lParam);
}

bool InitializeDialogImpl(HWND hDlg, CustomErrorState* state) {
    const HWND owner = ::GetWindow(hDlg, GW_OWNER);
    const UINT dpi = GetDpiForWindowSafe(owner != nullptr ? owner : hDlg);

    if (state->hideClose) {
        RemoveCloseButton(hDlg);
    }

    state->font = CreateMessageFont(state->fontOwned);
    if (state->font == nullptr) {
        state->completed = true;
        ::EndDialog(hDlg, -2);
        return false;
    }

    const int margin = Scale(16, dpi);
    const int gap = Scale(12, dpi);
    const int iconSize = Scale(32, dpi);
    const int minTextWidth = Scale(260, dpi);
    const int maxTextWidth = Scale(420, dpi);
    const int maxTextHeight = Scale(360, dpi);
    const int buttonHeight = Scale(26, dpi);
    const int buttonGap = Scale(8, dpi);
    const int buttonPadding = Scale(12, dpi);
    const int minButtonWidth = Scale(75, dpi);
    const int minClientWidth = Scale(320, dpi);

    HDC dc = ::GetDC(hDlg);
    if (dc == nullptr) {
        state->completed = true;
        ::EndDialog(hDlg, -2);
        return false;
    }
    HGDIOBJ oldFont = ::SelectObject(dc, state->font);

    RECT textRect{0, 0, maxTextWidth, 0};
    if (!state->text.empty()) {
        ::DrawTextW(dc, state->text.c_str(), -1, &textRect,
                    DT_CALCRECT | DT_WORDBREAK | DT_NOPREFIX | DT_EXPANDTABS);
    }
    int textWidth = state->text.empty() ? 0 : static_cast<int>(textRect.right - textRect.left);
    int textHeight = state->text.empty() ? 0 : static_cast<int>(textRect.bottom - textRect.top);
    if (!state->text.empty() && textWidth < minTextWidth) {
        textWidth = minTextWidth;
    }
    if (textWidth > maxTextWidth) {
        textWidth = maxTextWidth;
    }
    const bool useEdit = textHeight > maxTextHeight;
    if (useEdit) {
        textHeight = maxTextHeight;
    }
    if (textHeight > 0) {
        textHeight += Scale(2, dpi);
    }

    std::vector<int> buttonWidths;
    buttonWidths.reserve(state->buttons.size());
    int totalButtonsWidth = 0;
    for (const std::wstring& label : state->buttons) {
        SIZE textSize{};
        ::GetTextExtentPoint32W(dc, label.c_str(), static_cast<int>(label.size()), &textSize);
        int width = static_cast<int>(textSize.cx) + 2 * buttonPadding;
        if (width < minButtonWidth) {
            width = minButtonWidth;
        }
        buttonWidths.push_back(width);
        totalButtonsWidth += width;
    }
    if (buttonWidths.size() > 1) {
        totalButtonsWidth += buttonGap * (static_cast<int>(buttonWidths.size()) - 1);
    }

    ::SelectObject(dc, oldFont);
    ::ReleaseDC(hDlg, dc);

    const HICON icon = LoadIconForDpi(state->iconKind, iconSize, iconSize);
    const int iconWidth = icon != nullptr ? iconSize : 0;
    const int textOffset = icon != nullptr ? iconSize + gap : 0;
    const int contentWidth = textOffset + textWidth;
    int clientWidth = std::max({contentWidth, totalButtonsWidth, minClientWidth}) + 2 * margin;
    const int bodyHeight = std::max(iconWidth, textHeight);
    const int clientHeight = margin + bodyHeight + (bodyHeight > 0 ? gap : 0) + buttonHeight + margin;

    if (icon != nullptr) {
        HWND iconControl = ::CreateWindowExW(
            0, L"STATIC", nullptr,
            WS_CHILD | WS_VISIBLE | SS_ICON,
            margin, margin, iconSize, iconSize,
            hDlg, reinterpret_cast<HMENU>(static_cast<INT_PTR>(100)),
            ::GetModuleHandleW(nullptr), nullptr);
        if (iconControl == nullptr) {
            state->completed = true;
            ::EndDialog(hDlg, -2);
            return false;
        }
        ::SendMessageW(iconControl, STM_SETICON, reinterpret_cast<WPARAM>(icon), 0);
        ::SendMessageW(hDlg, WM_SETICON, ICON_SMALL, reinterpret_cast<LPARAM>(icon));
        ::SendMessageW(hDlg, WM_SETICON, ICON_BIG, reinterpret_cast<LPARAM>(icon));
    }

    if (!state->text.empty()) {
        const DWORD textStyle = useEdit
            ? (WS_CHILD | WS_VISIBLE | ES_MULTILINE | ES_READONLY | ES_AUTOVSCROLL | WS_VSCROLL)
            : (WS_CHILD | WS_VISIBLE | SS_LEFT | SS_NOPREFIX);
        HWND textControl = ::CreateWindowExW(
            0, useEdit ? L"EDIT" : L"STATIC", state->text.c_str(), textStyle,
            margin + textOffset, margin, textWidth, textHeight,
            hDlg, reinterpret_cast<HMENU>(static_cast<INT_PTR>(101)),
            ::GetModuleHandleW(nullptr), nullptr);
        if (textControl == nullptr) {
            state->completed = true;
            ::EndDialog(hDlg, -2);
            return false;
        }
        ::SendMessageW(textControl, WM_SETFONT, reinterpret_cast<WPARAM>(state->font), TRUE);
        if (useEdit) {
            ::SendMessageW(textControl, EM_SETMARGINS, EC_LEFTMARGIN | EC_RIGHTMARGIN, 0);
            const WNDPROC oldProc = reinterpret_cast<WNDPROC>(
                ::GetWindowLongPtrW(textControl, GWLP_WNDPROC));
            if (oldProc != nullptr) {
                ::SetWindowLongPtrW(textControl, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(oldProc));
                ::SetWindowLongPtrW(textControl, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(EditSubclassProc));
            }
        }
    }

    const int buttonY = clientHeight - margin - buttonHeight;
    int buttonX = clientWidth - margin - totalButtonsWidth;
    HWND firstButton = nullptr;
    for (size_t index = 0; index < state->buttons.size(); ++index) {
        DWORD buttonStyle = WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON;
        if (index == 0) {
            buttonStyle |= BS_DEFPUSHBUTTON;
        }

        HWND button = ::CreateWindowExW(
            0, L"BUTTON", state->buttons[index].c_str(), buttonStyle,
            buttonX, buttonY, buttonWidths[index], buttonHeight,
            hDlg,
            reinterpret_cast<HMENU>(static_cast<INT_PTR>(kButtonIdBase + static_cast<int>(index))),
            ::GetModuleHandleW(nullptr), nullptr);
        if (button == nullptr) {
            state->completed = true;
            ::EndDialog(hDlg, -2);
            return false;
        }
        ::SendMessageW(button, WM_SETFONT, reinterpret_cast<WPARAM>(state->font), TRUE);
        if (index == 0) {
            firstButton = button;
        }
        buttonX += buttonWidths[index] + buttonGap;
    }

    if (firstButton == nullptr) {
        state->completed = true;
        ::EndDialog(hDlg, -2);
        return false;
    }
    ::SendMessageW(hDlg, DM_SETDEFID, kButtonIdBase, 0);

    RECT windowRect{0, 0, clientWidth, clientHeight};
    const DWORD style = static_cast<DWORD>(::GetWindowLongPtrW(hDlg, GWL_STYLE));
    const DWORD exStyle = static_cast<DWORD>(::GetWindowLongPtrW(hDlg, GWL_EXSTYLE));
    if (AdjustWindowRectForDpi(&windowRect, style, exStyle, dpi) == FALSE) {
        windowRect.right = clientWidth;
        windowRect.bottom = clientHeight;
    }
    const int windowWidth = static_cast<int>(windowRect.right - windowRect.left);
    const int windowHeight = static_cast<int>(windowRect.bottom - windowRect.top);

    HMONITOR monitor = nullptr;
    if (owner != nullptr) {
        monitor = ::MonitorFromWindow(owner, MONITOR_DEFAULTTONEAREST);
    } else {
        const POINT origin{0, 0};
        monitor = ::MonitorFromPoint(origin, MONITOR_DEFAULTTOPRIMARY);
    }

    RECT workArea{};
    MONITORINFO monitorInfo{};
    monitorInfo.cbSize = sizeof(monitorInfo);
    if (monitor != nullptr && ::GetMonitorInfoW(monitor, &monitorInfo) != FALSE) {
        workArea = monitorInfo.rcWork;
    } else {
        ::SystemParametersInfoW(SPI_GETWORKAREA, 0, &workArea, 0);
    }

    const int windowX = workArea.left + ((workArea.right - workArea.left) - windowWidth) / 2;
    const int windowY = workArea.top + ((workArea.bottom - workArea.top) - windowHeight) / 2;
    ::SetWindowPos(hDlg, nullptr, windowX, windowY, windowWidth, windowHeight,
                   SWP_NOZORDER | SWP_NOACTIVATE);

    return true;
}

bool InitializeDialog(HWND hDlg, CustomErrorState* state) {
    try {
        return InitializeDialogImpl(hDlg, state);
    } catch (...) {
        // 不要让 C++ 异常穿过 Windows 对话框管理器。
        state->completed = true;
        ::EndDialog(hDlg, -2);
        return false;
    }
}

INT_PTR CALLBACK CustomErrorDialogProc(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam) {
    auto* state = reinterpret_cast<CustomErrorState*>(::GetWindowLongPtrW(hDlg, GWLP_USERDATA));

    switch (message) {
    case WM_INITDIALOG: {
        state = reinterpret_cast<CustomErrorState*>(lParam);
        ::SetWindowLongPtrW(hDlg, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(state));
        if (state == nullptr) {
            ::EndDialog(hDlg, -2);
            return TRUE;
        }

        state->completed = false;
        if (!InitializeDialog(hDlg, state)) {
            return TRUE;
        }
        return TRUE;
    }
    case WM_COMMAND: {
        if (state == nullptr) {
            break;
        }

        const int controlId = LOWORD(wParam);
        if (HIWORD(wParam) == BN_CLICKED
            && controlId >= kButtonIdBase
            && controlId < kButtonIdBase + static_cast<int>(state->buttons.size())) {
            state->completed = true;
            ::EndDialog(hDlg, controlId - kButtonIdBase);
            return TRUE;
        }

        if (controlId == IDOK) {
            if (!state->buttons.empty()) {
                state->completed = true;
                ::EndDialog(hDlg, 0);
            }
            return TRUE;
        }

        if (controlId == IDCANCEL) {
            if (state->hideClose) {
                return TRUE;
            }
            state->completed = true;
            ::EndDialog(hDlg, state->buttons.empty()
                ? -1
                : static_cast<int>(state->buttons.size()) - 1);
            return TRUE;
        }
        break;
    }
    case WM_CLOSE:
        if (state != nullptr && state->hideClose) {
            return TRUE;
        }
        if (state != nullptr) {
            state->completed = true;
        }
        ::EndDialog(hDlg, -1);
        return TRUE;
    case WM_SYSCOMMAND:
        if (state != nullptr && state->hideClose && (wParam & 0xFFF0) == SC_CLOSE) {
            return TRUE;
        }
        break;
    case WM_CTLCOLORSTATIC:
    case WM_CTLCOLOREDIT: {
        HDC hdc = reinterpret_cast<HDC>(wParam);
        ::SetBkMode(hdc, TRANSPARENT);
        ::SetBkColor(hdc, ::GetSysColor(COLOR_3DFACE));
        return reinterpret_cast<INT_PTR>(::GetSysColorBrush(COLOR_3DFACE));
    }
    case WM_NCDESTROY:
        if (state != nullptr) {
            if (state->fontOwned && state->font != nullptr) {
                ::DeleteObject(state->font);
            }
            state->font = nullptr;
            state->fontOwned = false;
            ::SetWindowLongPtrW(hDlg, GWLP_USERDATA, 0);
        }
        break;
    default:
        break;
    }
    return FALSE;
}

} // namespace

int CUSTOM_ERROR(const wchar_t* Title,
                 const wchar_t* Text,
                 const wchar_t* Icon,
                 const wchar_t* Buttons,
                 int HideCloseButton) {
    try {
        CustomErrorState state;
        state.text = (Text != nullptr) ? Text : L"";
        state.buttons = ParseButtons(Buttons);
        if (state.buttons.size() > static_cast<size_t>(kMaxButtons)) {
            return -2;
        }
        state.iconKind = ParseIconKind(Icon);
        state.hideClose = (HideCloseButton != 0);

        const std::wstring title = (Title != nullptr && Title[0] != L'\0') ? Title : L"错误";
        const std::vector<BYTE> dialogTemplate = BuildDialogTemplate(title, state.hideClose);

        const HWND owner = errorreport::detail::GetErrorMessageOwner();
        const INT_PTR result = ::DialogBoxIndirectParamW(
            ::GetModuleHandleW(nullptr),
            reinterpret_cast<LPCDLGTEMPLATEW>(dialogTemplate.data()),
            owner,
            CustomErrorDialogProc,
            reinterpret_cast<LPARAM>(&state));

        if (state.fontOwned && state.font != nullptr) {
            ::DeleteObject(state.font);
            state.font = nullptr;
            state.fontOwned = false;
        }

        if (result == -1 && !state.completed) {
            return -2;
        }
        return static_cast<int>(result);
    } catch (...) {
        return -2;
    }
}
