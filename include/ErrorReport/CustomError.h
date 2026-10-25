#pragma once

#include "ErrorReport/Export.h"

#ifdef __cplusplus
extern "C" {
#endif

// 显示一个完全自定义的错误窗口。
//
//   Title           : 标题；nullptr 或空串时使用“错误”。
//   Text            : 正文；nullptr 或空串时正文为空。
//   Icon            : "error" / "warning" / "info" / "question" / "none"；
//                     nullptr、空串或 "none" 不显示图标；未知值也按无图标处理。
//   Buttons         : 以 '|' 分隔的按钮文本，例如 L"确定|取消"；
//                     nullptr 或空串时使用“确定”；最多 8 个，空段忽略。
//                     第一个按钮是默认按钮。按钮文本中的 '&' 按 Win32 习惯
//                     作为加速键前缀；要显示 '&' 请写成 "&&"。
//   HideCloseButton : 非 0 时隐藏标题栏关闭按钮，并忽略 X、Alt+F4 和 Esc；
//                     此时只能通过按钮退出。
//
// 返回值：
//   >= 0 : 被点击按钮的 0 基索引。
//   -1   : 通过关闭按钮或 Alt+F4 关闭（HideCloseButton 为 0 时）。
//   -2   : 参数错误（例如按钮超过 8 个）或窗口创建失败。
//
// 说明：
//   - HideCloseButton 为 0 时，按 Esc 会选择最后一个按钮；
//     若没有任何按钮（实际不会发生，至少会有一个“确定”），则返回 -1。
//   - 该函数是阻塞的，直到用户点击按钮或关闭窗口才返回。
//   - 图标只使用系统图标，不支持从 .ico 文件加载。
ERRORREPORT_API int CUSTOM_ERROR(const wchar_t* Title,
                                 const wchar_t* Text,
                                 const wchar_t* Icon,
                                 const wchar_t* Buttons,
                                 int HideCloseButton);

#ifdef __cplusplus
}
#endif
