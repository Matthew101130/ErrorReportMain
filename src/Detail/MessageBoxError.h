#pragma once

#include <string>

namespace errorreport::detail {

// 空指针或空字符串时返回 fallback，否则返回 value 的副本。
std::wstring ToWString(const wchar_t* value, const wchar_t* fallback);

// 以 "<程序名> - <分类>" 的形式生成对话框标题。
std::wstring MakeErrorTitle(const wchar_t* errorProgram, const wchar_t* category);

// 统一弹出错误对话框，返回 MessageBoxW 的结果。
int ShowErrorBox(const wchar_t* errorProgram, const wchar_t* category, const std::wstring& text, unsigned int flags);

// 内存不足等导致详细文本无法构造时的兜底对话框；不抛出 C++ 异常。
int ShowFallbackErrorBox(unsigned int flags) noexcept;

} // namespace errorreport::detail
