using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ErrorReportLauncher
{
    public sealed class MainForm : Form
    {
        private readonly List<PopupDefinition> _popups = new List<PopupDefinition>();
        private readonly List<PopupRowControl> _rows = new List<PopupRowControl>();
        private readonly Panel _rowsPanel = new Panel();
        private readonly TableLayoutPanel _header = new TableLayoutPanel();
        private readonly Button _addButton = new Button();
        private readonly Button _generateButton = new Button();
        private readonly RoundedTextBox _logFolder = new RoundedTextBox();
        private readonly CheckBox _useRuntimeFolder = new CheckBox();
        private readonly Button _browseButton = new Button();
        private Bitmap _browseIcon = CreateFolderIcon(16);
        private readonly Label _statusLabel = new Label();

        private float _scale = 1F;
        private int _rowHeight = 44;
        private bool _repositioning;

        public MainForm()
        {
            Text = "ErrorReportMain 官方启动器";
            Font = new Font("Microsoft YaHei UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 650);
            MinimumSize = new Size(840, 600);
            BackColor = Color.FromArgb(248, 248, 248);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;

            CreateControls();
            BuildLayout();
            _popups.Add(new PopupDefinition
            {
                Function = ReportFunction.DllMissing,
                ErrorProgram = "Demo.exe",
                MissingDll = "MSVCP140.dll",
                RepeatCount = 1,
            });
            RefreshList();
        }

        private int S(float value)
        {
            return Math.Max(1, (int)Math.Round(value * _scale));
        }

        private void CreateControls()
        {
            _addButton.Text = "＋ 添加弹窗";
            _addButton.Width = 120;
            _addButton.Height = 32;
            _addButton.Margin = new Padding(0);
            _addButton.Click += (_, _) => AddPopup();

            _rowsPanel.AutoScroll = true;
            _rowsPanel.Dock = DockStyle.Fill;
            _rowsPanel.BackColor = Color.White;
            _rowsPanel.BorderStyle = BorderStyle.FixedSingle;
            _rowsPanel.AllowDrop = true;
            _rowsPanel.Margin = new Padding(0);
            _rowsPanel.Resize += (_, _) => RepositionRows();
            _rowsPanel.DragOver += RowsPanelDragOver;
            _rowsPanel.DragDrop += RowsPanelDragDrop;

            _logFolder.Value = GetDefaultLogFolder();
            _logFolder.TextAlignment = HorizontalAlignment.Left;
            _logFolder.Dock = DockStyle.Fill;
            _logFolder.Margin = new Padding(0, 4, 8, 4);

            _browseButton.Text = string.Empty;
            _browseButton.Image = _browseIcon;
            _browseButton.ImageAlign = ContentAlignment.MiddleCenter;
            _browseButton.AccessibleName = "选择日志文件夹";
            _browseButton.Width = 36;
            _browseButton.Height = 25;
            _browseButton.Margin = new Padding(0, 4, 0, 4);
            _browseButton.Click += (_, _) => BrowseLogFolder();
            var browseTip = new ToolTip();
            browseTip.SetToolTip(_browseButton, "选择日志文件夹");

            _useRuntimeFolder.Text = "日志生成在运行文件夹";
            _useRuntimeFolder.AutoSize = true;
            _useRuntimeFolder.Checked = true;
            _useRuntimeFolder.Margin = new Padding(0, 8, 0, 4);
            _useRuntimeFolder.CheckAlign = ContentAlignment.MiddleLeft;
            var runtimeTip = new ToolTip();
            runtimeTip.SetToolTip(
                _useRuntimeFolder,
                "勾选后，生成的程序会把日志写在它自己所在的文件夹，此时日志文件夹输入框被禁用；\n"
                + "取消勾选则日志固定写在上面指定的日志文件夹。");
            _useRuntimeFolder.CheckedChanged += (_, _) => UpdateLogFolderEnabledState();
            UpdateLogFolderEnabledState();

            _generateButton.Text = "生成 .exe";
            _generateButton.Width = 120;
            _generateButton.Height = 32;
            _generateButton.Margin = new Padding(0, 0, 12, 0);
            _generateButton.Click += (_, _) => GenerateExecutable();

            _statusLabel.AutoSize = false;
            _statusLabel.Width = 500;
            _statusLabel.Height = 24;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Margin = new Padding(0, 4, 0, 0);
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(18),
                BackColor = BackColor,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var topPanel = new TableLayoutPanel
            {
                AutoSize = false,
                Height = S(34),
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8),
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(132)));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, S(34)));
            topPanel.Controls.Add(new Label
            {
                Text = "弹窗组（按顺序执行）",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 0, S(16), 0),
            }, 0, 0);
            _addButton.Dock = DockStyle.Fill;
            _addButton.Margin = new Padding(0);
            topPanel.Controls.Add(_addButton, 1, 0);
            root.Controls.Add(topPanel, 0, 0);

            BuildHeader();
            root.Controls.Add(_header, 0, 1);

            root.Controls.Add(_rowsPanel, 0, 2);

            var logPanel = new TableLayoutPanel
            {
                AutoSize = false,
                Height = S(34),
                ColumnCount = 4,
                RowCount = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 8, 0, 0),
            };
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(92)));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(44)));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(220)));
            logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, S(34)));
            logPanel.Controls.Add(new Label
            {
                Text = "日志文件夹",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
            }, 0, 0);
            _logFolder.Dock = DockStyle.Fill;
            _logFolder.Margin = new Padding(0, 0, S(8), 0);
            logPanel.Controls.Add(_logFolder, 1, 0);
            _browseButton.Dock = DockStyle.Fill;
            _browseButton.Margin = new Padding(0);
            logPanel.Controls.Add(_browseButton, 2, 0);
            _useRuntimeFolder.AutoSize = false;
            _useRuntimeFolder.Dock = DockStyle.Fill;
            _useRuntimeFolder.TextAlign = ContentAlignment.MiddleLeft;
            _useRuntimeFolder.Margin = new Padding(S(8), 0, 0, 0);
            logPanel.Controls.Add(_useRuntimeFolder, 3, 0);
            root.Controls.Add(logPanel, 0, 3);

            var bottomPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 10, 0, 0),
            };
            bottomPanel.Controls.Add(_generateButton);
            bottomPanel.Controls.Add(_statusLabel);
            root.Controls.Add(bottomPanel, 0, 4);

            Controls.Add(root);
        }

        private void BuildHeader()
        {
            _header.Dock = DockStyle.Top;
            _header.Height = S(30);
            _header.ColumnCount = 9;
            _header.RowCount = 1;
            _header.Margin = new Padding(0);
            _header.BackColor = Color.FromArgb(242, 244, 247);
            _header.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            _header.Paint += HeaderPaint;
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(40)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(50)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(86)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(95)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(70)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(96)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(56)));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(64)));
            _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AddHeaderLabel(string.Empty, 0, 1);
            AddHeaderLabel("序号", 1, 1);
            AddHeaderLabel("弹窗", 2, 1);
            AddHeaderLabel("弹出次数", 3, 1);
            AddHeaderLabel("无限循环", 4, 1);
            AddHeaderLabel("输出结果", 5, 1);
            AddHeaderLabel("名称", 6, 1);
            AddHeaderLabel("操作", 7, 2);
        }

        private void AddHeaderLabel(string text, int column, int span)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Font = new Font(Font, FontStyle.Bold),
            };
            _header.Controls.Add(label, column, 0);
            if (span > 1)
            {
                _header.SetColumnSpan(label, span);
            }
        }

        private void HeaderPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(214, 216, 220));
            e.Graphics.DrawLine(pen, 0, _header.Height - 1, _header.Width, _header.Height - 1);
        }

        private void RefreshList()
        {
            _rowsPanel.SuspendLayout();
            foreach (PopupRowControl row in _rows)
            {
                row.Dispose();
            }
            _rows.Clear();
            _rowsPanel.Controls.Clear();

            _rowHeight = S(44);
            for (int i = 0; i < _popups.Count; i++)
            {
                int index = i;
                var row = new PopupRowControl(_popups[i], index, _scale, _rowHeight);
                row.AllowDrop = true;
                row.DragOver += RowsPanelDragOver;
                row.DragDrop += RowsPanelDragDrop;
                row.EditRequested += (_, _) => EditPopup(index);
                row.DeleteRequested += (_, _) => DeletePopup(index);
                row.Changed += (_, _) => UpdateStatus();
                _rows.Add(row);
                _rowsPanel.Controls.Add(row);
            }

            _rowsPanel.ResumeLayout(performLayout: false);
            RepositionRows();
            UpdateStatus();
        }

        private void RepositionRows()
        {
            if (_repositioning)
            {
                return;
            }

            _repositioning = true;
            try
            {
                _rowHeight = S(44);
                int count = _rows.Count;
                bool needScroll = count * _rowHeight > _rowsPanel.ClientSize.Height;
                int width = _rowsPanel.ClientSize.Width - 2;
                if (needScroll)
                {
                    width -= SystemInformation.VerticalScrollBarWidth;
                }
                width = Math.Max(200, width);

                int y = 0;
                foreach (PopupRowControl row in _rows)
                {
                    row.Location = new Point(0, y);
                    row.Size = new Size(width, _rowHeight);
                    y += _rowHeight;
                }

                _rowsPanel.AutoScrollMinSize = new Size(0, Math.Max(0, count * _rowHeight + 2));
            }
            finally
            {
                _repositioning = false;
            }
        }

        private void AddPopup()
        {
            try
            {
                using var editor = new PopupEditorForm(null, isEdit: false);
                if (editor.ShowDialog(this) == DialogResult.OK && editor.Result is not null)
                {
                    PopupDefinition popup = editor.Result;
                    popup.RepeatCount = 1;
                    popup.Infinite = false;
                    _popups.Add(popup);
                    RefreshList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "无法打开弹窗编辑器",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditPopup(int index)
        {
            PopupDefinition old = _popups[index];
            using var editor = new PopupEditorForm(old.Clone(), isEdit: true);
            if (editor.ShowDialog(this) == DialogResult.OK && editor.Result is not null)
            {
                PopupDefinition popup = editor.Result;
                popup.RepeatCount = old.RepeatCount;
                popup.Infinite = old.Infinite;
                popup.OutputResult = old.OutputResult;
                popup.OutputName = old.OutputName;
                _popups[index] = popup;
                RefreshList();
            }
        }

        private void DeletePopup(int index)
        {
            DialogResult result = MessageBox.Show(
                this,
                $"删除第 {index + 1} 个弹窗？",
                "ErrorReportMain",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _popups.RemoveAt(index);
                RefreshList();
            }
        }

        private void UpdateStatus()
        {
            int infiniteIndex = _popups.FindIndex(popup => popup.Infinite);
            if (infiniteIndex >= 0 && infiniteIndex != _popups.Count - 1)
            {
                _statusLabel.Text = "错误：无限循环弹窗必须是最后一个。";
                _statusLabel.ForeColor = Color.Firebrick;
                return;
            }

            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = $"{_popups.Count} 个弹窗";
        }

        private void RowsPanelDragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(typeof(int))
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void RowsPanelDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data is null || !e.Data.GetDataPresent(typeof(int)))
            {
                return;
            }

            int source = (int)e.Data.GetData(typeof(int));
            if (source < 0 || source >= _popups.Count)
            {
                return;
            }

            Point point = _rowsPanel.PointToClient(new Point(e.X, e.Y));
            int target = _rows.Count;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (point.Y < _rows[i].Top + _rows[i].Height / 2)
                {
                    target = i;
                    break;
                }
            }

            PopupDefinition popup = _popups[source];
            _popups.RemoveAt(source);
            if (target > source)
            {
                target--;
            }
            target = Math.Max(0, Math.Min(target, _popups.Count));
            _popups.Insert(target, popup);
            RefreshList();
        }

        private void UpdateLogFolderEnabledState()
        {
            bool runtimeFolder = _useRuntimeFolder.Checked;
            _logFolder.Enabled = !runtimeFolder;
            _browseButton.Enabled = !runtimeFolder;
        }

        private void BrowseLogFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择日志输出文件夹",
                SelectedPath = Directory.Exists(_logFolder.Value) ? _logFolder.Value : string.Empty,
                ShowNewFolderButton = true,
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _logFolder.Value = dialog.SelectedPath;
            }
        }

        private List<string>? GetValidatedOutputNames()
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _popups.Count; i++)
            {
                PopupDefinition popup = _popups[i];
                if (!popup.OutputResult)
                {
                    continue;
                }
                if (popup.Infinite)
                {
                    MessageBox.Show(
                        this,
                        $"第 {i + 1} 个弹窗是无限循环，永远不会结束，不能输出结果。",
                        "ErrorReportMain",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return null;
                }

                string outputName = popup.OutputName.Trim();
                if (!IsValidCppIdentifier(outputName))
                {
                    MessageBox.Show(
                        this,
                        $"第 {i + 1} 个弹窗的输出名称无效：\"{outputName}\"。\n"
                        + "请使用 C++ 标识符（字母/下划线开头，只含字母、数字、下划线）。",
                        "ErrorReportMain",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return null;
                }
                if (!seen.Add(outputName))
                {
                    MessageBox.Show(
                        this,
                        $"输出名称重复：\"{outputName}\"。",
                        "ErrorReportMain",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return null;
                }
                popup.OutputName = outputName;
                names.Add(outputName);
            }
            return names;
        }

        private void GenerateExecutable()
        {
            if (_popups.Count == 0)
            {
                MessageBox.Show(this, "请先添加至少一个弹窗。", "ErrorReportMain",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int infiniteIndex = _popups.FindIndex(popup => popup.Infinite);
            if (infiniteIndex >= 0 && infiniteIndex != _popups.Count - 1)
            {
                MessageBox.Show(
                    this,
                    "无限循环弹窗必须是最后一个。\n\n它后面的弹窗永远不会执行，请调整顺序。",
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            for (int i = 0; i < _popups.Count; i++)
            {
                PopupDefinition popup = _popups[i];
                if (!popup.Infinite && popup.RepeatCount < 1)
                {
                    MessageBox.Show(
                        this,
                        $"第 {i + 1} 个弹窗的弹出次数必须是正整数。",
                        "ErrorReportMain",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            List<string>? outputNames = GetValidatedOutputNames();
            if (outputNames is null)
            {
                return;
            }

            string logFolder = _logFolder.Value.Trim();
            if (!_useRuntimeFolder.Checked)
            {
                if (logFolder.Length == 0)
                {
                    MessageBox.Show(this, "请填写日志文件夹。", "ErrorReportMain",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    logFolder = Path.GetFullPath(logFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "日志文件夹路径无效：\n" + ex.Message, "ErrorReportMain",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(logFolder))
                {
                    DialogResult create = MessageBox.Show(
                        this,
                        $"日志文件夹不存在：\n{logFolder}\n\n是否创建？",
                        "ErrorReportMain",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (create != DialogResult.Yes)
                    {
                        return;
                    }
                    try
                    {
                        Directory.CreateDirectory(logFolder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "无法创建日志文件夹：\n" + ex.Message, "ErrorReportMain",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            else
            {
                try
                {
                    logFolder = logFolder.Length > 0
                        ? Path.GetFullPath(logFolder)
                        : GetDefaultLogFolder();
                }
                catch (Exception)
                {
                    logFolder = GetDefaultLogFolder();
                }
            }

            string? dllPath = FindDllPath();
            if (dllPath is null)
            {
                MessageBox.Show(
                    this,
                    "找不到 ErrorReportMain.dll。\n\n"
                    + "请先构建 x64 Release 版本的 DLL，并把 DLL 放在启动器同目录，"
                    + "或者放在项目的 x64\\Release 目录。",
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!IsX64Dll(dllPath))
            {
                MessageBox.Show(
                    this,
                    "找到的 ErrorReportMain.dll 不是 x64 版本。\n\n"
                    + "当前启动器是 x64，请使用 x64 Release 构建的 DLL。",
                    "ErrorReportMain",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "生成播放器",
                Filter = "可执行文件 (*.exe)|*.exe",
                FileName = "ErrorReportPlayer.exe",
                DefaultExt = "exe",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = logFolder,
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                _statusLabel.Text = "已取消";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                PopupDefinition first = _popups[0];
                var config = new PlayerConfig
                {
                    Function = first.Function,
                    LogFolder = logFolder,
                    UseRuntimeFolder = _useRuntimeFolder.Checked,
                    Popups = _popups.Select(popup => popup.Clone()).ToList(),
                };
                Generator.Generate(Application.ExecutablePath, dllPath, dialog.FileName, config);
                _statusLabel.ForeColor = Color.DimGray;
                _statusLabel.Text = "已生成：" + dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "生成失败：\n" + ex.Message, "ErrorReportMain",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static bool IsValidCppIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            char first = name[0];
            if (!(first == '_' || (first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z')))
            {
                return false;
            }
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!(c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                {
                    return false;
                }
            }

            switch (name)
            {
                case "alignas":
                case "alignof":
                case "and":
                case "and_eq":
                case "asm":
                case "auto":
                case "bitand":
                case "bitor":
                case "bool":
                case "break":
                case "case":
                case "catch":
                case "char":
                case "class":
                case "compl":
                case "concept":
                case "const":
                case "consteval":
                case "constexpr":
                case "constinit":
                case "const_cast":
                case "continue":
                case "co_await":
                case "co_return":
                case "co_yield":
                case "decltype":
                case "default":
                case "delete":
                case "do":
                case "double":
                case "dynamic_cast":
                case "else":
                case "enum":
                case "explicit":
                case "export":
                case "extern":
                case "false":
                case "float":
                case "for":
                case "friend":
                case "goto":
                case "if":
                case "inline":
                case "int":
                case "long":
                case "mutable":
                case "namespace":
                case "new":
                case "noexcept":
                case "not":
                case "not_eq":
                case "nullptr":
                case "operator":
                case "or":
                case "or_eq":
                case "private":
                case "protected":
                case "public":
                case "register":
                case "reinterpret_cast":
                case "requires":
                case "return":
                case "short":
                case "signed":
                case "sizeof":
                case "static":
                case "static_assert":
                case "static_cast":
                case "struct":
                case "switch":
                case "template":
                case "this":
                case "thread_local":
                case "throw":
                case "true":
                case "try":
                case "typedef":
                case "typeid":
                case "typename":
                case "union":
                case "unsigned":
                case "using":
                case "virtual":
                case "void":
                case "volatile":
                case "wchar_t":
                case "while":
                case "xor":
                case "xor_eq":
                    return false;
                default:
                    return true;
            }
        }

        private static string? FindDllPath()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string localPath = Path.Combine(baseDirectory, "ErrorReportMain.dll");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            var directory = new DirectoryInfo(baseDirectory);
            for (int i = 0; i < 8 && directory is not null; i++, directory = directory.Parent)
            {
                foreach (string configuration in new[] { "Release", "Debug" })
                {
                    string candidate = Path.Combine(
                        directory.FullName, "x64", configuration, "ErrorReportMain.dll");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static bool IsX64Dll(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);
                if (stream.Length < 0x40)
                {
                    return false;
                }

                stream.Seek(0x3C, SeekOrigin.Begin);
                int peOffset = reader.ReadInt32();
                if (peOffset <= 0 || peOffset + 6 > stream.Length)
                {
                    return false;
                }

                stream.Seek(peOffset, SeekOrigin.Begin);
                if (reader.ReadUInt32() != 0x00004550)
                {
                    return false;
                }

                return reader.ReadUInt16() == 0x8664;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string GetDefaultLogFolder()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(documents))
            {
                documents = AppContext.BaseDirectory;
            }
            return Path.Combine(documents, "ErrorReportLogs");
        }

        private static Bitmap CreateFolderIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.ScaleTransform(size / 16f, size / 16f);
            using var tab = new SolidBrush(Color.FromArgb(220, 170, 60));
            using var body = new SolidBrush(Color.FromArgb(245, 205, 100));
            using var outline = new Pen(Color.FromArgb(150, 115, 40));
            graphics.FillRectangle(tab, 1, 3, 7, 3);
            graphics.FillRectangle(body, 1, 5, 14, 9);
            graphics.DrawRectangle(outline, 1, 5, 13, 8);
            return bitmap;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            float scale = DpiHelper.GetScale(Handle);
            if (scale > 1.01f)
            {
                SuspendLayout();
                Scale(new SizeF(scale, scale));
                ClientSize = new Size(
                    (int)Math.Round(820 * scale),
                    (int)Math.Round(650 * scale));
                MinimumSize = new Size(
                    (int)Math.Round(840 * scale),
                    (int)Math.Round(600 * scale));
                ResumeLayout(performLayout: true);
                _scale = scale;
            }

            ApplyHeaderScale();
            RefreshList();

            int iconSize = Math.Max(16, S(16));
            if (_browseIcon.Width != iconSize)
            {
                Bitmap icon = CreateFolderIcon(iconSize);
                _browseButton.Image = icon;
                _browseIcon.Dispose();
                _browseIcon = icon;
            }

            CenterToScreen();
        }

        private void ApplyHeaderScale()
        {
            _header.Height = S(30);
            _header.ColumnStyles[0].Width = S(40);
            _header.ColumnStyles[1].Width = S(50);
            _header.ColumnStyles[3].Width = S(86);
            _header.ColumnStyles[4].Width = S(95);
            _header.ColumnStyles[5].Width = S(70);
            _header.ColumnStyles[6].Width = S(96);
            _header.ColumnStyles[7].Width = S(56);
            _header.ColumnStyles[8].Width = S(64);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _browseIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
