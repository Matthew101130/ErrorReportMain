using System;

namespace ErrorReportLauncher
{
    /// <summary>弹窗组中的一个弹窗定义。</summary>
    public class PopupDefinition
    {
        public ReportFunction Function { get; set; }

        public string ErrorProgram { get; set; } = string.Empty;
        public string MissingDll { get; set; } = string.Empty;
        public string ErrorName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Operate { get; set; } = string.Empty;
        public string MemoryAddress { get; set; } = string.Empty;
        public string CannotBe { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = "none";
        public string Buttons { get; set; } = string.Empty;
        public int HideCloseButton { get; set; }

        /// <summary>有限重复次数；必须为正整数。</summary>
        public int RepeatCount { get; set; } = 1;

        /// <summary>true 时一直重复；该弹窗必须是组内最后一个。</summary>
        public bool Infinite { get; set; }

        public PopupDefinition Clone()
        {
            return new PopupDefinition
            {
                Function = Function,
                ErrorProgram = ErrorProgram,
                MissingDll = MissingDll,
                ErrorName = ErrorName,
                ErrorCode = ErrorCode,
                Address = Address,
                Operate = Operate,
                MemoryAddress = MemoryAddress,
                CannotBe = CannotBe,
                Title = Title,
                Text = Text,
                Icon = Icon,
                Buttons = Buttons,
                HideCloseButton = HideCloseButton,
                RepeatCount = RepeatCount,
                Infinite = Infinite,
            };
        }

        public string GetDisplayText()
        {
            return Function switch
            {
                ReportFunction.DllMissing =>
                    $"DLL_MISSING_ERROR — {TextOr(ErrorProgram, "未知程序")} / {TextOr(MissingDll, "未知模块")}",
                ReportFunction.ExeError =>
                    $"EXE_ERROR — {TextOr(ErrorProgram, "未知程序")} / {TextOr(ErrorName, "未知异常")}",
                ReportFunction.MemoryError =>
                    $"MEMORY_ERROR — {TextOr(ErrorProgram, "未知程序")} / {TextOr(Operate, "0x00000000")}",
                ReportFunction.Custom =>
                    $"CUSTOM_ERROR — {TextOr(Title, "错误")}",
                ReportFunction.CustomMessageBox =>
                    $"CUSTOM_MESSAGEBOX — {TextOr(Title, "错误")}",
                _ => Function.ToString(),
            };
        }

        private static string TextOr(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
