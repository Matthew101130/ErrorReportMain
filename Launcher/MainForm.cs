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
        private readonly ComboBox _functionCombo = new ComboBox();
        private readonly PreviewPanel _preview = new PreviewPanel();
        private readonly TableLayoutPanel _paramsTable = new TableLayoutPanel();
        private readonly TableLayoutPanel _content = new TableLayoutPanel();
        private readonly List<FunctionOption> _functions = new List<FunctionOption>();
        private readonly List<Label> _generatedLabels = new List<Label>();

        private readonly TextBox _errorProgram = new TextBox();
        private readonly TextBox _missingDll = new TextBox();
        private readonly TextBox _errorName = new TextBox();
        private readonly TextBox _errorCode = new TextBox();
        private readonly TextBox _address = new TextBox();
        private readonly TextBox _operate = new TextBox();
        private readonly TextBox _memoryAddress = new TextBox();
        private readonly TextBox _cannotBe = new TextBox();

        private readonly TextBox _customTitle = new TextBox();
        private readonly TextBox _customText = new TextBox();
        private readonly ComboBox _customIcon = new ComboBox();
        private readonly CheckBox _hideClose = new CheckBox();
        private readonly FlowLayoutPanel _buttonsPanel = new FlowLayoutPanel();
        private CheckBox[] _buttonChecks = Array.Empty<CheckBox>();

        private readonly TextBox _logFolder = new TextBox();
        private readonly CheckBox _useRuntimeFolder = new CheckBox();
        private readonly Button _browseButton = new Button();
        private Bitmap _browseIcon = CreateFolderIcon(16);
        private readonly Button _generateButton = new Button();
        private readonly Label _statusLabel = new Label();

        private bool _suspendPreview;

        public MainForm()
        {
            Text = "ErrorReportMain 官方启动器";
            Font = new Font("Microsoft YaHei UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(700, 650);
            MinimumSize = new Size(720, 620);
            AutoScroll = true;
            BackColor = Color.FromArgb(248, 248, 248);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;

            CreateControls();
            BuildLayout();

            _functionCombo.SelectedIndex = 0;
        }

        private void CreateControls()
        {
            _functions.Add(new FunctionOption(ReportFunction.DllMissing, "DLL_MISSING_ERROR — 缺少 DLL"));
            _functions.Add(new FunctionOption(ReportFunction.ExeError, "EXE_ERROR — 应用程序异常"));
            _functions.Add(new FunctionOption(ReportFunction.MemoryError, "MEMORY_ERROR — 内存错误"));
            _functions.Add(new FunctionOption(ReportFunction.Custom, "CUSTOM_ERROR — 自定义窗口"));
            foreach (FunctionOption option in _functions)
            {
                _functionCombo.Items.Add(option);
            }
            _functionCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _functionCombo.Width = 360;
            _functionCombo.SelectedIndexChanged += (_, _) =>
            {
                BuildParameterRows();
                UpdatePreview();
            };

            _preview.Width = 660;
            _preview.Height = 190;
            _preview.BorderStyle = BorderStyle.FixedSingle;
            _preview.Margin = new Padding(0, 8, 0, 8);

            _paramsTable.AutoSize = true;
            _paramsTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _paramsTable.ColumnCount = 2;
            _paramsTable.Dock = DockStyle.Top;
            _paramsTable.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            _paramsTable.Margin = new Padding(0);
            _paramsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            _paramsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _errorProgram.Text = "Demo.exe";
            _missingDll.Text = "MSVCP140.dll";
            _errorName.Text = "0xC0000005";
            _errorCode.Text = "Access violation";
            _address.Text = "0x00007FF612340000";
            _operate.Text = "0x00401000";
            _memoryAddress.Text = "0x00000000";
            _cannotBe.Text = "read";

            _customTitle.Text = "自定义错误";
            _customText.Text = "这里是可以自定义的正文。\r\n可以换行。";
            _customText.Multiline = true;
            _customText.Height = 64;
            _customText.ScrollBars = ScrollBars.Vertical;
            _customText.AcceptsReturn = true;

            _customIcon.DropDownStyle = ComboBoxStyle.DropDownList;
            _customIcon.Items.AddRange(new object[] { "无", "错误", "警告", "信息", "问号" });
            _customIcon.SelectedIndex = 1;

            _hideClose.Text = "隐藏右上角关闭按钮";
            _hideClose.AutoSize = true;

            _buttonsPanel.AutoSize = true;
            _buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttonsPanel.WrapContents = true;
            _buttonsPanel.Dock = DockStyle.Fill;
            _buttonsPanel.Margin = new Padding(0);
            _buttonsPanel.MinimumSize = new Size(0, 30);

            string[] buttonLabels = { "确定", "取消", "是", "否", "重试", "忽略", "中止", "继续" };
            _buttonChecks = new CheckBox[buttonLabels.Length];
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                var checkBox = new CheckBox
                {
                    Text = buttonLabels[i],
                    AutoSize = true,
                    Margin = new Padding(0, 0, 16, 4),
                };
                checkBox.CheckedChanged += (_, _) => UpdatePreview();
                _buttonsPanel.Controls.Add(checkBox);
                _buttonChecks[i] = checkBox;
            }
            _buttonChecks[0].Checked = true;

            foreach (TextBox textBox in new[]
                     {
                         _errorProgram, _missingDll, _errorName, _errorCode, _address,
                         _operate, _memoryAddress, _cannotBe, _customTitle, _customText,
                     })
            {
                textBox.TextChanged += (_, _) => UpdatePreview();
            }
            _customIcon.SelectedIndexChanged += (_, _) => UpdatePreview();
            _hideClose.CheckedChanged += (_, _) => UpdatePreview();

            _logFolder.Text = GetDefaultLogFolder();
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
            _content.AutoSize = true;
            _content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _content.ColumnCount = 1;
            _content.RowCount = 5;
            _content.Location = new Point(18, 18);
            _content.Margin = new Padding(0);
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 660F));
            for (int i = 0; i < 5; i++)
            {
                _content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var functionPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0),
            };
            functionPanel.Controls.Add(new Label
            {
                Text = "功能",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 8, 0),
            });
            functionPanel.Controls.Add(_functionCombo);
            _content.Controls.Add(functionPanel, 0, 0);
            _content.Controls.Add(_preview, 0, 1);
            _content.Controls.Add(_paramsTable, 0, 2);

            var logPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 8, 0, 0),
            };
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            logPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            logPanel.Controls.Add(new Label
            {
                Text = "日志文件夹",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 8, 0),
            }, 0, 0);
            logPanel.Controls.Add(_logFolder, 1, 0);
            logPanel.Controls.Add(_browseButton, 2, 0);
            logPanel.Controls.Add(_useRuntimeFolder, 3, 0);
            _content.Controls.Add(logPanel, 0, 3);

            var bottomPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 10, 0, 0),
            };
            bottomPanel.Controls.Add(_generateButton);
            bottomPanel.Controls.Add(_statusLabel);
            _content.Controls.Add(bottomPanel, 0, 4);

            Controls.Add(_content);
        }

        private void BuildParameterRows()
        {
            if (_functionCombo.SelectedItem is not FunctionOption option)
            {
                return;
            }

            _suspendPreview = true;
            foreach (Label label in _generatedLabels)
            {
                label.Dispose();
            }
            _generatedLabels.Clear();
            _paramsTable.Controls.Clear();
            _paramsTable.RowStyles.Clear();
            _paramsTable.RowCount = 0;

            switch (option.Kind)
            {
                case ReportFunction.DllMissing:
                    EnsureDefault(_errorProgram, "Demo.exe");
                    EnsureDefault(_missingDll, "MSVCP140.dll");
                    AddRow("程序名", _errorProgram);
                    AddRow("缺失模块", _missingDll);
                    break;

                case ReportFunction.ExeError:
                    EnsureDefault(_errorProgram, "Demo.exe");
                    EnsureDefault(_errorName, "0xC0000005");
                    EnsureDefault(_errorCode, "Access violation");
                    EnsureDefault(_address, "0x00007FF612340000");
                    AddRow("程序名", _errorProgram);
                    AddRow("异常名", _errorName);
                    AddRow("错误代码", _errorCode);
                    AddRow("地址", _address);
                    break;

                case ReportFunction.MemoryError:
                    EnsureDefault(_errorProgram, "Demo.exe");
                    EnsureDefault(_operate, "0x00401000");
                    EnsureDefault(_memoryAddress, "0x00000000");
                    EnsureDefault(_cannotBe, "read");
                    AddRow("程序名", _errorProgram);
                    AddRow("指令地址", _operate);
                    AddRow("被引用地址", _memoryAddress);
                    AddRow("访问类型", _cannotBe);
                    break;

                case ReportFunction.Custom:
                    AddRow("标题", _customTitle);
                    AddRow("正文", _customText, 74);
                    AddRow("图标", _customIcon);
                    AddRow("按钮", _buttonsPanel, 64);
                    AddRow("选项", _hideClose);
                    break;
            }

            _paramsTable.ResumeLayout(performLayout: true);
            _content.PerformLayout();
            _suspendPreview = false;
            UpdatePreview();
        }

        private void AddRow(string label, Control control, int height = 0)
        {
            int row = _paramsTable.RowCount++;
            _paramsTable.RowStyles.Add(height > 0
                ? new RowStyle(SizeType.Absolute, height)
                : new RowStyle(SizeType.AutoSize));

            var labelControl = new Label
            {
                Text = label,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 8, 0),
            };
            _generatedLabels.Add(labelControl);
            _paramsTable.Controls.Add(labelControl, 0, row);

            control.Dock = control is CheckBox ? DockStyle.None : DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);
            _paramsTable.Controls.Add(control, 1, row);
        }

        private void EnsureDefault(TextBox textBox, string value)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = value;
            }
        }

        private void UpdatePreview()
        {
            if (_suspendPreview)
            {
                return;
            }
            _preview.Data = BuildPreviewData();
        }

        private PreviewData BuildPreviewData()
        {
            if (_functionCombo.SelectedItem is not FunctionOption option)
            {
                return new PreviewData();
            }

            string program = TextOr(_errorProgram.Text, "未知程序");

            switch (option.Kind)
            {
                case ReportFunction.DllMissing:
                {
                    string missingDll = TextOr(_missingDll.Text, "未知模块");
                    return new PreviewData
                    {
                        Title = $"{program} - 系统错误",
                        Body = $"无法启动此程序，因为计算机中丢失 {missingDll}。尝试重新安装该程序以解决此问题。",
                        Icon = "error",
                        Buttons = new[] { "确定" },
                    };
                }

                case ReportFunction.ExeError:
                {
                    string errorName = TextOr(_errorName.Text, "未知异常");
                    string errorCode = TextOr(_errorCode.Text, "未知代码");
                    string address = TextOr(_address.Text, "未知地址");
                    return new PreviewData
                    {
                        Title = $"{program} - 应用程序错误",
                        Body = $"应用程序发生异常 {errorName} ({errorCode})，位置为 {address}。\r\n\r\n"
                               + "要终止程序，请单击“确定”。\r\n要调试程序，请单击“取消”。",
                        Icon = "error",
                        Buttons = new[] { "确定", "取消" },
                    };
                }

                case ReportFunction.MemoryError:
                {
                    string operate = TextOr(_operate.Text, "0x00000000");
                    string memoryAddress = TextOr(_memoryAddress.Text, "0x00000000");
                    string cannotBe = TextOr(_cannotBe.Text, "read");
                    return new PreviewData
                    {
                        Title = $"{program} - 应用程序错误",
                        Body = $"\"{operate}\" 指令引用的 \"{memoryAddress}\" 内存。该内存不能为 \"{cannotBe}\"。\r\n\r\n"
                               + "要终止程序，请单击“确定”。\r\n要调试程序，请单击“取消”。",
                        Icon = "error",
                        Buttons = new[] { "确定", "取消" },
                    };
                }

                case ReportFunction.Custom:
                {
                    string[] selectedButtons = _buttonChecks
                        .Where(checkBox => checkBox.Checked)
                        .Select(checkBox => checkBox.Text)
                        .ToArray();
                    if (selectedButtons.Length == 0)
                    {
                        selectedButtons = new[] { "确定" };
                    }
                    return new PreviewData
                    {
                        Title = TextOr(_customTitle.Text, "错误"),
                        Body = _customText.Text,
                        Icon = GetSelectedIconKey(),
                        Buttons = selectedButtons,
                        HideClose = _hideClose.Checked,
                    };
                }

                default:
                    return new PreviewData();
            }
        }

        private string GetSelectedIconKey()
        {
            switch (_customIcon.SelectedIndex)
            {
                case 1:
                    return "error";
                case 2:
                    return "warning";
                case 3:
                    return "info";
                case 4:
                    return "question";
                default:
                    return "none";
            }
        }

        private void UpdateLogFolderEnabledState()
        {
            // 勾选“日志生成在运行文件夹”时，日志文件夹输入无效，禁用输入框和浏览按钮。
            bool runtimeFolder = _useRuntimeFolder.Checked;
            _logFolder.Enabled = !runtimeFolder;
            _browseButton.Enabled = !runtimeFolder;
        }

        private void BrowseLogFolder()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择日志输出文件夹",
                SelectedPath = Directory.Exists(_logFolder.Text) ? _logFolder.Text : string.Empty,
                ShowNewFolderButton = true,
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _logFolder.Text = dialog.SelectedPath;
            }
        }

        private void GenerateExecutable()
        {
            string logFolder = _logFolder.Text.Trim();
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
                // 日志跟随生成程序的运行文件夹，这里只为“另存为”对话框准备一个初始目录。
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
                PlayerConfig config = BuildPlayerConfig(logFolder);
                Generator.Generate(Application.ExecutablePath, dllPath, dialog.FileName, config);
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

        private PlayerConfig BuildPlayerConfig(string logFolder)
        {
            var option = (FunctionOption)_functionCombo.SelectedItem!;
            var config = new PlayerConfig
            {
                Function = option.Kind,
                LogFolder = logFolder,
                UseRuntimeFolder = _useRuntimeFolder.Checked,
            };

            switch (option.Kind)
            {
                case ReportFunction.DllMissing:
                    config.ErrorProgram = _errorProgram.Text;
                    config.MissingDll = _missingDll.Text;
                    break;
                case ReportFunction.ExeError:
                    config.ErrorProgram = _errorProgram.Text;
                    config.ErrorName = _errorName.Text;
                    config.ErrorCode = _errorCode.Text;
                    config.Address = _address.Text;
                    break;
                case ReportFunction.MemoryError:
                    config.ErrorProgram = _errorProgram.Text;
                    config.Operate = _operate.Text;
                    config.MemoryAddress = _memoryAddress.Text;
                    config.CannotBe = _cannotBe.Text;
                    break;
                case ReportFunction.Custom:
                    config.Title = _customTitle.Text;
                    config.Text = _customText.Text;
                    config.Icon = GetSelectedIconKey();
                    config.Buttons = string.Join(
                        "|",
                        _buttonChecks.Where(checkBox => checkBox.Checked).Select(checkBox => checkBox.Text));
                    config.HideCloseButton = _hideClose.Checked ? 1 : 0;
                    break;
            }

            return config;
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

                ushort machine = reader.ReadUInt16();
                return machine == 0x8664;
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
            string current = Environment.CurrentDirectory;
            if (string.IsNullOrEmpty(current))
            {
                current = AppContext.BaseDirectory;
            }
            return current;
        }

        private static string TextOr(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
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

            float scale = DeviceDpi / 96f;
            if (scale <= 1.01f)
            {
                return;
            }

            SuspendLayout();
            Scale(new SizeF(scale, scale));
            ClientSize = new Size(
                (int)Math.Round(700 * scale),
                (int)Math.Round(650 * scale));
            MinimumSize = new Size(
                (int)Math.Round(720 * scale),
                (int)Math.Round(620 * scale));
            ResumeLayout(performLayout: true);

            int iconSize = Math.Max(16, (int)Math.Round(16 * scale));
            if (_browseIcon.Width != iconSize)
            {
                Bitmap icon = CreateFolderIcon(iconSize);
                _browseButton.Image = icon;
                _browseIcon.Dispose();
                _browseIcon = icon;
            }

            CenterToScreen();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _browseIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class FunctionOption
        {
            public FunctionOption(ReportFunction kind, string display)
            {
                Kind = kind;
                Display = display;
            }

            public ReportFunction Kind { get; }

            public string Display { get; }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}
