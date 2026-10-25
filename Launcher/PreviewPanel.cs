using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    internal sealed class PreviewData
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Icon { get; set; } = "none";
        public string[] Buttons { get; set; } = Array.Empty<string>();
        public bool HideClose { get; set; }
    }

    /// <summary>用 GDI+ 画一个错误窗口的示意图，随参数实时更新。</summary>
    internal sealed class PreviewPanel : Panel
    {
        private PreviewData _data = new PreviewData();

        public PreviewPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(245, 245, 245);
        }

        public PreviewData Data
        {
            get => _data;
            set
            {
                _data = value ?? new PreviewData();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle client = ClientRectangle;
            var window = new Rectangle(
                8,
                8,
                Math.Max(0, client.Width - 16),
                Math.Max(0, client.Height - 16));
            if (window.Width < 120 || window.Height < 90)
            {
                return;
            }

            using (var background = new SolidBrush(Color.White))
            {
                graphics.FillRectangle(background, window);
            }

            const int titleHeight = 30;
            var titleBar = new Rectangle(window.X, window.Y, window.Width, titleHeight);
            using (var titleBackground = new SolidBrush(Color.FromArgb(240, 240, 240)))
            {
                graphics.FillRectangle(titleBackground, titleBar);
            }
            using (var border = new Pen(Color.FromArgb(150, 150, 150)))
            {
                graphics.DrawRectangle(border, window);
            }

            using (var titleFont = new Font(Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    graphics,
                    _data.Title,
                    titleFont,
                    new Rectangle(window.X + 10, window.Y, window.Width - 50, titleHeight),
                    Color.Black,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            if (!_data.HideClose)
            {
                int x = window.Right - 22;
                int y = window.Y + 9;
                using var closePen = new Pen(Color.FromArgb(90, 90, 90), 1.5f);
                graphics.DrawLine(closePen, x, y, x + 8, y + 8);
                graphics.DrawLine(closePen, x + 8, y, x, y + 8);
            }

            int contentLeft = window.X + 16;
            int contentTop = titleBar.Bottom + 14;
            using (Image? icon = GetIcon(_data.Icon))
            {
                if (icon is not null)
                {
                    graphics.DrawImage(icon, contentLeft, contentTop, 32, 32);
                    contentLeft += 44;
                }
            }

            var bodyRectangle = new Rectangle(
                contentLeft,
                contentTop,
                Math.Max(40, window.Right - contentLeft - 16),
                Math.Max(20, window.Bottom - contentTop - 54));
            TextRenderer.DrawText(
                graphics,
                _data.Body,
                Font,
                bodyRectangle,
                Color.Black,
                TextFormatFlags.WordBreak
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.TextBoxControl
                | TextFormatFlags.EndEllipsis);

            string[] labels = _data.Buttons.Length > 0 ? _data.Buttons : new[] { "确定" };
            const int buttonHeight = 26;
            const int gap = 8;
            const int horizontalPadding = 12;
            const int minimumButtonWidth = 66;

            var widths = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Size size = TextRenderer.MeasureText(labels[i], Font);
                widths[i] = Math.Max(minimumButtonWidth, size.Width + horizontalPadding * 2);
            }

            int totalWidth = widths.Sum() + gap * (labels.Length - 1);
            int availableWidth = window.Width - 32;
            if (totalWidth > availableWidth && totalWidth > 0)
            {
                double scale = (double)availableWidth / totalWidth;
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = Math.Max(30, (int)(widths[i] * scale));
                }
                totalWidth = widths.Sum() + gap * (labels.Length - 1);
            }

            int buttonX = window.Right - 16 - totalWidth;
            int buttonY = window.Bottom - 16 - buttonHeight;
            for (int i = 0; i < labels.Length; i++)
            {
                var buttonRectangle = new Rectangle(buttonX, buttonY, widths[i], buttonHeight);
                using (var buttonBackground = new SolidBrush(Color.FromArgb(240, 240, 240)))
                {
                    graphics.FillRectangle(buttonBackground, buttonRectangle);
                }
                using (var buttonBorder = new Pen(Color.FromArgb(140, 140, 140)))
                {
                    graphics.DrawRectangle(buttonBorder, buttonRectangle);
                }
                TextRenderer.DrawText(
                    graphics,
                    labels[i],
                    Font,
                    buttonRectangle,
                    Color.Black,
                    TextFormatFlags.HorizontalCenter
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.EndEllipsis
                    | TextFormatFlags.NoPrefix);
                buttonX += widths[i] + gap;
            }
        }

        private static Image? GetIcon(string icon)
        {
            switch (icon)
            {
                case "error":
                    return SystemIcons.Error.ToBitmap();
                case "warning":
                    return SystemIcons.Warning.ToBitmap();
                case "info":
                    return SystemIcons.Information.ToBitmap();
                case "question":
                    return SystemIcons.Question.ToBitmap();
                default:
                    return null;
            }
        }
    }
}
