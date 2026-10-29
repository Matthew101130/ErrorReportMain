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

            float scale = DeviceDpi / 96f;
            int S(float value) => (int)Math.Round(value * scale);
            float SF(float value) => value * scale;

            Rectangle client = ClientRectangle;
            var window = new Rectangle(
                S(8),
                S(8),
                Math.Max(0, client.Width - S(16)),
                Math.Max(0, client.Height - S(16)));
            if (window.Width < S(120) || window.Height < S(90))
            {
                return;
            }

            using (var background = new SolidBrush(Color.White))
            {
                graphics.FillRectangle(background, window);
            }

            int titleHeight = S(30);
            var titleBar = new Rectangle(window.X, window.Y, window.Width, titleHeight);
            using (var titleBackground = new SolidBrush(Color.FromArgb(240, 240, 240)))
            {
                graphics.FillRectangle(titleBackground, titleBar);
            }
            using (var border = new Pen(Color.FromArgb(150, 150, 150), SF(1f)))
            {
                graphics.DrawRectangle(border, window);
            }

            TextRenderer.DrawText(
                graphics,
                _data.Title,
                Font,
                new Rectangle(window.X + S(10), window.Y, window.Width - S(50), titleHeight),
                Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (!_data.HideClose)
            {
                int x = window.Right - S(22);
                int y = window.Y + S(9);
                using var closePen = new Pen(Color.FromArgb(90, 90, 90), SF(1.5f));
                graphics.DrawLine(closePen, x, y, x + S(8), y + S(8));
                graphics.DrawLine(closePen, x + S(8), y, x, y + S(8));
            }

            int contentLeft = window.X + S(16);
            int contentTop = titleBar.Bottom + S(14);
            using (Bitmap? icon = CreateModernIcon(_data.Icon, S(32)))
            {
                if (icon is not null)
                {
                    graphics.DrawImage(icon, contentLeft, contentTop, S(32), S(32));
                    contentLeft += S(44);
                }
            }

            var bodyRectangle = new Rectangle(
                contentLeft,
                contentTop,
                Math.Max(S(40), window.Right - contentLeft - S(16)),
                Math.Max(S(20), window.Bottom - contentTop - S(54)));
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
            int buttonHeight = S(26);
            int gap = S(8);
            int horizontalPadding = S(12);
            int minimumButtonWidth = S(66);

            var widths = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Size size = TextRenderer.MeasureText(labels[i], Font);
                widths[i] = Math.Max(minimumButtonWidth, size.Width + horizontalPadding * 2);
            }

            int totalWidth = widths.Sum() + gap * (labels.Length - 1);
            int availableWidth = window.Width - S(32);
            if (totalWidth > availableWidth && totalWidth > 0)
            {
                double widthScale = (double)availableWidth / totalWidth;
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] = Math.Max(S(30), (int)(widths[i] * widthScale));
                }
                totalWidth = widths.Sum() + gap * (labels.Length - 1);
            }

            int buttonX = window.Right - S(16) - totalWidth;
            int buttonY = window.Bottom - S(16) - buttonHeight;
            for (int i = 0; i < labels.Length; i++)
            {
                var buttonRectangle = new Rectangle(buttonX, buttonY, widths[i], buttonHeight);
                using (var buttonBackground = new SolidBrush(Color.FromArgb(250, 250, 250)))
                {
                    graphics.FillRectangle(buttonBackground, buttonRectangle);
                }
                using (var buttonBorder = new Pen(Color.FromArgb(200, 200, 200), SF(1f)))
                {
                    graphics.DrawRectangle(buttonBorder, buttonRectangle);
                }
                if (i == 0)
                {
                    using var focusPen = new Pen(Color.FromArgb(0, 120, 212), SF(1.5f));
                    graphics.DrawRectangle(
                        focusPen,
                        buttonRectangle.X + S(1),
                        buttonRectangle.Y + S(1),
                        Math.Max(0, buttonRectangle.Width - S(3)),
                        Math.Max(0, buttonRectangle.Height - S(3)));
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

        private static Bitmap? CreateModernIcon(string icon, int size)
        {
            if (string.IsNullOrEmpty(icon) || icon == "none")
            {
                return null;
            }

            var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            switch (icon)
            {
                case "error":
                {
                    using var brush = new SolidBrush(Color.FromArgb(209, 52, 56));
                    graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);
                    using var pen = new Pen(Color.White, Math.Max(2f, size / 10f));
                    float padding = size * 0.32f;
                    graphics.DrawLine(pen, padding, padding, size - padding, size - padding);
                    graphics.DrawLine(pen, size - padding, padding, padding, size - padding);
                    break;
                }

                case "warning":
                {
                    var points = new[]
                    {
                        new PointF(size / 2f, 2f),
                        new PointF(size - 2f, size - 3f),
                        new PointF(2f, size - 3f),
                    };
                    using (var brush = new SolidBrush(Color.FromArgb(255, 185, 0)))
                    {
                        graphics.FillPolygon(brush, points);
                    }
                    using (var pen = new Pen(Color.FromArgb(120, 80, 0), 1.5f))
                    {
                        graphics.DrawPolygon(pen, points);
                    }
                    using (var pen = new Pen(Color.FromArgb(80, 55, 0), Math.Max(2f, size / 10f)))
                    {
                        graphics.DrawLine(pen, size / 2f, size * 0.38f, size / 2f, size * 0.64f);
                    }
                    using (var brush = new SolidBrush(Color.FromArgb(80, 55, 0)))
                    {
                        graphics.FillEllipse(brush, size * 0.45f, size * 0.72f, size * 0.1f, size * 0.1f);
                    }
                    break;
                }

                case "info":
                {
                    using (var brush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    {
                        graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);
                    }
                    using (var brush = new SolidBrush(Color.White))
                    {
                        graphics.FillEllipse(brush, size * 0.44f, size * 0.22f, size * 0.12f, size * 0.12f);
                        graphics.FillRectangle(brush, size * 0.44f, size * 0.4f, size * 0.12f, size * 0.36f);
                    }
                    break;
                }

                case "question":
                {
                    using (var circleBrush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    {
                        graphics.FillEllipse(circleBrush, 1, 1, size - 2, size - 2);
                    }
                    using var font = new Font("Segoe UI", size * 0.6f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var questionBrush = new SolidBrush(Color.White);
                    SizeF textSize = graphics.MeasureString("?", font);
                    graphics.DrawString(
                        "?",
                        font,
                        questionBrush,
                        (size - textSize.Width) / 2f,
                        (size - textSize.Height) / 2f - 1f);
                    break;
                }

                default:
                    bitmap.Dispose();
                    return null;
            }

            return bitmap;
        }
    }
}
