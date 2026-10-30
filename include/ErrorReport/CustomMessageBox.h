#pragma once

#include "ErrorReport/Export.h"

#ifdef __cplusplus
extern "C" {
#endif

// 显示一个使用标准 MessageBoxW 按钮和系统图标的消息框。
//
//   Title   : 标题；nullptr 或空串时使用“错误”。
//   Text    : 正文；nullptr 或空串时正文为空。
//   Icon    : "error" / "warning" / "info" / "question" / "none"；
//             nullptr、空串或 "none" 不显示图标；未知值也按无图标处理。
//   Buttons : MessageBoxW 标准按钮组合：
//               "ok" / "okcancel" / "abortretryignore" /
//               "yesnocancel" / "yesno" / "retrycancel" /
//               "canceltrycontinue"
//             可追加 "|help" 显示“帮助”按钮，例如 "ok|help"。
//             nullptr 或空串时使用 "ok"。
//
// 返回值：
//   MessageBoxW 的标准按钮 ID（IDOK / IDCANCEL / IDYES / IDNO /
//   IDABORT / IDRETRY / IDIGNORE / IDCONTINUE / IDTRYAGAIN / IDHELP），
//   0 表示 MessageBoxW 失败；
//   -2 表示按钮组合非法。
//
// 说明：
//   - 按钮文本由系统语言决定，不能像 CUSTOM_ERROR 一样自定义按钮文字；
//   - 不支持隐藏关闭按钮；
//   - 该函数是阻塞的，直到用户关闭消息框才返回。
ERRORREPORT_API int CUSTOM_MESSAGEBOX(const wchar_t* Title,
                                      const wchar_t* Text,
                                      const wchar_t* Icon,
                                      const wchar_t* Buttons);

#ifdef __cplusplus
}
#endif
