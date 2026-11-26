using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    /// <summary>弹窗组中的一行：序号、摘要、次数输入框、无限循环、编辑/删除。</summary>
    internal sealed class PopupRowControl : UserControl
    {
        private readonly RoundedTextBox _countBox = new RoundedTextBox();
        private readonly CheckBox _infiniteCheck = new CheckBox();
        private readonly Panel _infiniteHost = new Panel();
        private readonly CheckBox _outputCheck = new CheckBox();
        private readonly Panel _outputHost = new Panel();
        private readonly RoundedTextBox _outputName = new RoundedTextBox();
        private readonly Label _dragHandle = new Label();
        private readonly Label _indexLabel = new Label();
        private readonly TextBox _summaryLabel = new TextBox();
        private readonly Button _editButton = new Button();
        private readonly Button _deleteButton = new Button();

        private readonly float _scale;
        private readonly int _index;
        private bool _updating;
        private bool _dragging;
        private bool _suppressCountValidation;
        private Point _dragStart;

        public PopupDefinition Popup { get; }

        public event EventHandler? EditRequested;
        public event EventHandler? DeleteRequested;
        public event EventHandler? Changed;

        public PopupRowControl(PopupDefinition popup, int index, float scale, int rowHeight)
        {
            Popup = popup;
            _index = index;
            _scale = scale;
            Height = rowHeight;
            Margin = new Padding(0);
            Padding = new Padding(0, S(4), 0, S(4));
            BackColor = Color.White;
            DoubleBuffered = true;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 9,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(40)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(50)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(86)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(95)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(70)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(96)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(56)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(64)));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _dragHandle.Text = string.Empty;
            _dragHandle.TextAlign = ContentAlignment.MiddleCenter;
            _dragHandle.Dock = DockStyle.Fill;
            _dragHandle.Cursor = Cursors.SizeAll;
            _dragHandle.Margin = new Padding(0);
            _dragHandle.AccessibleName = "拖动排序";
            _dragHandle.Paint += DragHandlePaint;
            _dragHandle.MouseDown += DragHandleMouseDown;
            _dragHandle.MouseUp += DragHandleMouseUp;
            _dragHandle.MouseMove += DragHandleMouseMove;
            table.Controls.Add(_dragHandle, 0, 0);

            _indexLabel.Text = (index + 1).ToString();
            _indexLabel.TextAlign = ContentAlignment.MiddleCenter;
            _indexLabel.Dock = DockStyle.Fill;
            _indexLabel.Margin = new Padding(0);
            table.Controls.Add(_indexLabel, 1, 0);

            _summaryLabel.Text = popup.GetShortDisplayText();
            _summaryLabel.ReadOnly = true;
            _summaryLabel.BorderStyle = BorderStyle.None;
            _summaryLabel.BackColor = Color.White;
            _summaryLabel.ForeColor = Color.Black;
            _summaryLabel.TabStop = false;
            _summaryLabel.WordWrap = false;
            _summaryLabel.AutoSize = false;
            _summaryLabel.Height = S(20);
            _summaryLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _summaryLabel.Margin = new Padding(S(6), 0, S(6), 0);
            var summaryTip = new ToolTip();
            summaryTip.SetToolTip(_summaryLabel, popup.GetDisplayText());
            table.Controls.Add(_summaryLabel, 2, 0);

            _countBox.Height = S(20);
            _countBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _countBox.Margin = new Padding(S(4), 0, S(4), 0);
            _countBox.ValueKeyPress += CountBoxKeyPress;
            _countBox.ValueLeave += (_, _) => ValidateCount();
            _countBox.ValueTextChanged += CountBoxTextChanged;
            table.Controls.Add(_countBox, 3, 0);

            _infiniteCheck.Text = "无限循环";
            _infiniteCheck.AutoSize = true;
            _infiniteCheck.Margin = new Padding(0);
            _infiniteCheck.CheckedChanged += InfiniteCheckChanged;
            _infiniteCheck.SizeChanged += (_, _) => CenterInfiniteCheck();
            _infiniteHost.Dock = DockStyle.Fill;
            _infiniteHost.Margin = new Padding(0);
            _infiniteHost.BackColor = Color.Transparent;
            _infiniteHost.Controls.Add(_infiniteCheck);
            _infiniteHost.Resize += (_, _) => CenterInfiniteCheck();
            _infiniteHost.Layout += (_, _) => CenterInfiniteCheck();
            table.Controls.Add(_infiniteHost, 4, 0);

            _outputCheck.Text = "输出结果";
            _outputCheck.AutoSize = true;
            _outputCheck.Margin = new Padding(0);
            _outputCheck.CheckedChanged += OutputCheckChanged;
            _outputCheck.SizeChanged += (_, _) => CenterOutputCheck();
            _outputHost.Dock = DockStyle.Fill;
            _outputHost.Margin = new Padding(0);
            _outputHost.BackColor = Color.Transparent;
            _outputHost.Controls.Add(_outputCheck);
            _outputHost.Resize += (_, _) => CenterOutputCheck();
            _outputHost.Layout += (_, _) => CenterOutputCheck();
            table.Controls.Add(_outputHost, 5, 0);

            _outputName.Height = S(20);
            _outputName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _outputName.Margin = new Padding(S(4), 0, S(4), 0);
            _outputName.TextAlignment = HorizontalAlignment.Left;
            _outputName.ValueTextChanged += OutputNameTextChanged;
            table.Controls.Add(_outputName, 6, 0);

            _editButton.Text = "编辑";
            _editButton.Width = S(50);
            _editButton.Height = S(24);
            _editButton.Anchor = AnchorStyles.None;
            _editButton.Margin = new Padding(0);
            _editButton.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
            table.Controls.Add(_editButton, 7, 0);

            _deleteButton.Text = "删除";
            _deleteButton.Width = S(50);
            _deleteButton.Height = S(24);
            _deleteButton.Anchor = AnchorStyles.None;
            _deleteButton.Margin = new Padding(0);
            _deleteButton.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
            table.Controls.Add(_deleteButton, 8, 0);

            Controls.Add(table);
            ApplyState();
            CenterInfiniteCheck();
            CenterOutputCheck();
        }

        private void CenterInfiniteCheck()
        {
            if (_infiniteHost.ClientSize.Width <= 0)
            {
                return;
            }

            _infiniteCheck.Location = new Point(
                Math.Max(0, (_infiniteHost.ClientSize.Width - _infiniteCheck.Width) / 2),
                Math.Max(0, (_infiniteHost.ClientSize.Height - _infiniteCheck.Height) / 2));
        }

        private void CenterOutputCheck()
        {
            if (_outputHost.ClientSize.Width <= 0)
            {
                return;
            }

            _outputCheck.Location = new Point(
                Math.Max(0, (_outputHost.ClientSize.Width - _outputCheck.Width) / 2),
                Math.Max(0, (_outputHost.ClientSize.Height - _outputCheck.Height) / 2));
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            CenterInfiniteCheck();
            CenterOutputCheck();
        }

        private int S(float value)
        {
            return Math.Max(1, (int)Math.Round(value * _scale));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(225, 225, 225));
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }

        private void ApplyState()
        {
            _updating = true;
            if (Popup.OutputResult && string.IsNullOrWhiteSpace(Popup.OutputName))
            {
                Popup.OutputName = "result" + (_index + 1);
            }
            _infiniteCheck.Checked = Popup.Infinite;
            _countBox.Value = Popup.Infinite ? "∞" : Math.Max(1, Popup.RepeatCount).ToString();
            _countBox.ReadOnly = Popup.Infinite;
            _countBox.Muted = Popup.Infinite;
            _outputCheck.Checked = Popup.OutputResult;
            _outputName.Value = Popup.OutputName;
            _outputName.Enabled = Popup.OutputResult;
            _outputName.Muted = !Popup.OutputResult;
            _updating = false;
        }

        private void OutputCheckChanged(object? sender, EventArgs e)
        {
            if (_updating)
            {
                return;
            }

            Popup.OutputResult = _outputCheck.Checked;
            _updating = true;
            if (Popup.OutputResult && string.IsNullOrWhiteSpace(Popup.OutputName))
            {
                Popup.OutputName = "result" + (_index + 1);
            }
            _outputName.Value = Popup.OutputName;
            _outputName.Enabled = Popup.OutputResult;
            _outputName.Muted = !Popup.OutputResult;
            _updating = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void OutputNameTextChanged(object? sender, EventArgs e)
        {
            if (_updating)
            {
                return;
            }
            Popup.OutputName = _outputName.Value;
        }

        private void InfiniteCheckChanged(object? sender, EventArgs e)
        {
            if (_updating)
            {
                return;
            }

            Popup.Infinite = _infiniteCheck.Checked;
            if (Popup.Infinite)
            {
                Popup.RepeatCount = 1;
            }

            _updating = true;
            _countBox.Value = Popup.Infinite ? "∞" : "1";
            _countBox.ReadOnly = Popup.Infinite;
            _countBox.Muted = Popup.Infinite;
            _updating = false;

            if (!Popup.Infinite)
            {
                Popup.RepeatCount = 1;
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void CountBoxKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void CountBoxTextChanged(object? sender, EventArgs e)
        {
            if (Popup.Infinite)
            {
                return;
            }
            if (int.TryParse(_countBox.Value, out int count) && count >= 1)
            {
                Popup.RepeatCount = count;
            }
        }

        private void ValidateCount()
        {
            if (Popup.Infinite || _suppressCountValidation)
            {
                return;
            }

            if (!int.TryParse(_countBox.Value, out int count) || count < 1)
            {
                count = Math.Max(1, Popup.RepeatCount);
            }

            Popup.RepeatCount = count;
            _countBox.Value = count.ToString();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void DragHandlePaint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int lineWidth = S(14);
            int gap = S(5);
            int thickness = Math.Max(2, S(2));
            int centerX = _dragHandle.Width / 2;
            int centerY = _dragHandle.Height / 2;
            using var pen = new Pen(Color.FromArgb(105, 105, 105), thickness)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Flat,
                EndCap = System.Drawing.Drawing2D.LineCap.Flat,
            };
            for (int i = -1; i <= 1; i++)
            {
                int y = centerY + i * gap;
                e.Graphics.DrawLine(
                    pen,
                    centerX - lineWidth / 2,
                    y,
                    centerX + lineWidth / 2,
                    y);
            }
        }

        private void DragHandleMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // 拖拽开始时先提交输入框里的文字，避免随后的 Leave 事件读到中间状态。
            ValidateCount();
            _suppressCountValidation = true;
            _dragStart = e.Location;
            _dragging = true;
        }

        private void DragHandleMouseUp(object? sender, MouseEventArgs e)
        {
            _suppressCountValidation = false;
            _dragging = false;
        }

        private void DragHandleMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging || e.Button != MouseButtons.Left)
            {
                return;
            }
            if (Math.Abs(e.X - _dragStart.X) < 6 && Math.Abs(e.Y - _dragStart.Y) < 6)
            {
                return;
            }

            _dragging = false;
            try
            {
                DoDragDrop(_index, DragDropEffects.Move);
            }
            finally
            {
                _suppressCountValidation = false;
            }
        }
    }
}
