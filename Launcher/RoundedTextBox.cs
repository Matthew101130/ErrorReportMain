using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    /// <summary>Win11 风格的圆角输入框：无边框 TextBox + 自绘圆角外框。</summary>
    internal sealed class RoundedTextBox : UserControl
    {
        private readonly TextBox _inner = new TextBox();
        private readonly bool _nativeStyle = UiTheme.UseNativeTextStyle;
        private bool _muted;
        private bool _focused;

        public RoundedTextBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);

            BackColor = _nativeStyle ? SystemColors.Window : Color.White;
            Padding = new Padding(0);

            _inner.BorderStyle = _nativeStyle ? BorderStyle.FixedSingle : BorderStyle.None;
            _inner.TextAlign = HorizontalAlignment.Center;
            _inner.BackColor = _nativeStyle ? SystemColors.Window : Color.White;
            _inner.ForeColor = _nativeStyle ? SystemColors.WindowText : Color.Black;
            _inner.AutoSize = false;
            if (_nativeStyle)
            {
                _inner.Dock = DockStyle.Fill;
            }
            _inner.Enter += (_, _) =>
            {
                _focused = true;
                Invalidate();
            };
            _inner.Leave += (_, _) =>
            {
                _focused = false;
                Invalidate();
            };
            Controls.Add(_inner);
        }

        public string Value
        {
            get => _inner.Text;
            set => _inner.Text = value;
        }

        public HorizontalAlignment TextAlignment
        {
            get => _inner.TextAlign;
            set => _inner.TextAlign = value;
        }

        public bool ReadOnly
        {
            get => _inner.ReadOnly;
            set
            {
                _inner.ReadOnly = value;
                Invalidate();
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                _inner.BackColor = value ? SystemColors.Control : (_nativeStyle ? SystemColors.Window : Color.White);
                _inner.ForeColor = value ? SystemColors.GrayText : (_nativeStyle ? SystemColors.WindowText : Color.Black);
                Invalidate();
            }
        }

        public event KeyPressEventHandler? ValueKeyPress
        {
            add => _inner.KeyPress += value;
            remove => _inner.KeyPress -= value;
        }

        public event EventHandler? ValueLeave
        {
            add => _inner.Leave += value;
            remove => _inner.Leave -= value;
        }

        public event EventHandler? ValueTextChanged
        {
            add => _inner.TextChanged += value;
            remove => _inner.TextChanged -= value;
        }

        public void FocusInput()
        {
            _inner.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_nativeStyle)
            {
                LayoutInner();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _inner.Font = Font;
            if (!_nativeStyle)
            {
                LayoutInner();
            }
        }

        private void LayoutInner()
        {
            if (_nativeStyle)
            {
                return;
            }

            int left = Math.Max(4, (int)Math.Round(8 * DeviceDpi / 96.0));
            int height = _inner.PreferredHeight;
            int top = Math.Max(2, (Height - height) / 2);
            _inner.SetBounds(
                left,
                top,
                Math.Max(10, Width - left * 2),
                height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_nativeStyle)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            int radius = Math.Max(4, Math.Min(Height, Width) / 5);
            using GraphicsPath path = CreateRoundedPath(bounds, radius);
            using var fill = new SolidBrush(_muted ? SystemColors.Control : Color.White);
            e.Graphics.FillPath(fill, path);

            Color borderColor = _focused && !_muted
                ? Color.FromArgb(0, 120, 212)
                : _muted
                    ? Color.FromArgb(200, 200, 200)
                    : Color.FromArgb(190, 190, 190);
            using var pen = new Pen(borderColor);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
