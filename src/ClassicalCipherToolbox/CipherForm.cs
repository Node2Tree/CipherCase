using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox
{
    internal sealed partial class CipherForm : Form
    {
        private static readonly Color Background = Color.FromArgb(247, 248, 250);
        private static readonly Color Surface = Color.White;
        private static readonly Color Primary = Color.FromArgb(34, 41, 54);
        private static readonly Color Muted = Color.FromArgb(116, 124, 138);
        private static readonly Color Accent = Color.FromArgb(47, 102, 246);
        private static readonly Color Soft = Color.FromArgb(232, 236, 243);
        private static readonly Color Error = Color.FromArgb(196, 52, 52);
        private readonly IList<ICryptoTool> allTools;
        private readonly TableLayoutPanel rootLayout;
        private readonly ComboBox categoryPicker;
        private readonly ComboBox tagPicker;
        private readonly ComboBox toolPicker;
        private readonly FlowLayoutPanel modePanel;
        private readonly FlowLayoutPanel parameterPanel;
        private readonly Dictionary<string, TextBox> parameterBoxes;
        private readonly Dictionary<string, ComboBox> parameterPickers;
        private readonly Dictionary<string, string> parameterValues;
        private readonly Dictionary<ToolMode, Button> modeButtons;
        private readonly Timer liveTimer;
        private readonly List<BatchDocument> batchDocuments;
        private readonly TextBox inputBox;
        private readonly TextBox outputBox;
        private readonly DataGridView candidateGrid;
        private readonly ToolTip tips;
        private TextRuleOptions textRules;
        private readonly Label statusLabel;
        private readonly ProgressBar workProgress;
        private readonly Button cancelWorkButton;
        private readonly Button colorPickButton;
        private readonly Button identifyButton;
        private readonly Button universalButton;
        private readonly Button clueButton;
        private readonly FlowLayoutPanel colorPalettePanel;
        private readonly TableLayoutPanel colorPaletteLayout;
        private ICryptoTool currentTool;
        private ToolMode activeMode;
        private bool loadingBatch;
        private bool workRunning;
        private bool rerunPending;
        private int executionVersion;

        internal CipherForm()
        {
            Text = "密码箱 1.2.4";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 600);
            ClientSize = new Size(960, 700);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            BackColor = Background;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            DoubleBuffered = true;
            allTools = ToolRegistry.CreateAll();
            parameterBoxes = new Dictionary<string, TextBox>();
            parameterPickers = new Dictionary<string, ComboBox>();
            parameterValues = new Dictionary<string, string>();
            modeButtons = new Dictionary<ToolMode, Button>();
            batchDocuments = new List<BatchDocument>();
            textRules = new TextRuleOptions();
            tips = new ToolTip { AutoPopDelay = 12000, InitialDelay = 350, ReshowDelay = 100, ShowAlways = true };
            liveTimer = new Timer();
            liveTimer.Interval = 180;
            liveTimer.Tick += delegate { liveTimer.Stop(); BeginExecution(); };

            rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Padding = new Padding(24, 12, 24, 12);
            rootLayout.BackColor = Background;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 6;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            Controls.Add(rootLayout);

            FlowLayoutPanel topBar = CreateBar(FlowDirection.LeftToRight, 4);
            rootLayout.Controls.Add(topBar, 0, 0);
            categoryPicker = CreatePicker(104);
            categoryPicker.Margin = new Padding(0, 0, 8, 0);
            categoryPicker.Items.AddRange(new object[] { ToolCategories.General, ToolCategories.Chinese, ToolCategories.Encoding, ToolCategories.Substitution, ToolCategories.Polyalphabetic, ToolCategories.Transposition, ToolCategories.Grid });
            UpdatePickerDropDownWidth(categoryPicker);
            categoryPicker.SelectedIndexChanged += delegate { PopulateTools(); };
            topBar.Controls.Add(categoryPicker);
            tagPicker = CreatePicker(116);
            tagPicker.Margin = new Padding(0, 0, 8, 0);
            tagPicker.SelectedIndexChanged += delegate { PopulateTools(); };
            topBar.Controls.Add(tagPicker);
            toolPicker = CreatePicker(204);
            toolPicker.Margin = new Padding(0, 0, 10, 0);
            toolPicker.SelectedIndexChanged += ToolPickerSelectedIndexChanged;
            topBar.Controls.Add(toolPicker);
            modePanel = new FlowLayoutPanel();
            modePanel.AutoSize = true;
            modePanel.WrapContents = false;
            modePanel.Margin = Padding.Empty;
            modePanel.Padding = Padding.Empty;
            topBar.Controls.Add(modePanel);
            Button helpButton = CreateButton("?", 36, false);
            helpButton.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            helpButton.Margin = new Padding(10, 0, 0, 0);
            helpButton.Click += delegate { ShowHelp(); };
            topBar.Controls.Add(helpButton);
            Button rulesButton = CreateButton("Ω", 36, false);
            rulesButton.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            rulesButton.Margin = new Padding(4, 0, 0, 0);
            rulesButton.Click += delegate { ShowTextRules(); };
            topBar.Controls.Add(rulesButton);

            parameterPanel = CreateBar(FlowDirection.LeftToRight, 5);
            parameterPanel.WrapContents = true;
            parameterPanel.AutoScroll = true;
            rootLayout.Controls.Add(parameterPanel, 0, 1);
            inputBox = CreateTextArea(false);
            inputBox.TextChanged += InputTextChanged;
            inputBox.AllowDrop = true;
            inputBox.DragEnter += FilesDragEnter;
            inputBox.DragDrop += FilesDragDrop;
            inputBox.Margin = new Padding(0, 2, 0, 2);
            rootLayout.Controls.Add(inputBox, 0, 2);
            FlowLayoutPanel actionBar = CreateBar(FlowDirection.RightToLeft, 6);
            rootLayout.Controls.Add(actionBar, 0, 3);
            Button clearButton = CreateButton("清空", 64, false);
            clearButton.Margin = new Padding(6, 0, 0, 0);
            clearButton.Click += delegate { ClearText(); };
            actionBar.Controls.Add(clearButton);
            Button copyButton = CreateButton("复制", 64, false);
            copyButton.Margin = new Padding(6, 0, 0, 0);
            copyButton.Click += delegate { CopyOutput(); };
            actionBar.Controls.Add(copyButton);
            Button swapButton = CreateButton("互换", 64, false);
            swapButton.Margin = new Padding(6, 0, 0, 0);
            swapButton.Click += delegate { SwapText(); };
            actionBar.Controls.Add(swapButton);
            Button pasteButton = CreateButton("粘贴", 64, false);
            pasteButton.Margin = Padding.Empty;
            pasteButton.Click += delegate { PasteInput(); };
            actionBar.Controls.Add(pasteButton);
            Button openButton = CreateButton("打开", 64, false);
            openButton.Margin = new Padding(6, 0, 0, 0);
            openButton.Click += delegate { OpenFiles(); };
            actionBar.Controls.Add(openButton);
            colorPickButton = CreateButton("取色", 64, false);
            colorPickButton.Margin = new Padding(6, 0, 0, 0); colorPickButton.Visible = false;
            colorPickButton.Click += delegate { PickColor(); };
            actionBar.Controls.Add(colorPickButton);
            identifyButton = CreateButton("识别", 64, false);
            identifyButton.Margin = new Padding(6, 0, 0, 0); identifyButton.Visible = false;
            identifyButton.Click += delegate { NavigateToTool("密码识别器", ToolMode.Analyze); };
            actionBar.Controls.Add(identifyButton);
            universalButton = CreateButton("通用", 64, false);
            universalButton.Margin = new Padding(6, 0, 0, 0); universalButton.Visible = false;
            universalButton.Click += delegate { NavigateToTool("通用破解", ToolMode.Crack); };
            actionBar.Controls.Add(universalButton);
            clueButton = CreateButton("线索", 64, false);
            clueButton.Margin = new Padding(6, 0, 0, 0); clueButton.Visible = false;
            clueButton.Click += delegate { ShowKnownPlaintextEditor(); };
            actionBar.Controls.Add(clueButton);
            outputBox = CreateTextArea(true);
            outputBox.Margin = new Padding(0, 2, 0, 2);
            Panel outputPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            candidateGrid = CreateCandidateGrid();
            colorPaletteLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty, ColumnCount = 1, RowCount = 2, BackColor = Surface };
            colorPaletteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            colorPaletteLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            colorPaletteLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            colorPalettePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(0, 58), Visible = false, BackColor = Surface, Padding = new Padding(4), Margin = Padding.Empty, WrapContents = false, AutoScroll = true };
            candidateGrid.SelectionChanged += CandidateSelectionChanged;
            candidateGrid.CellDoubleClick += CandidateGridDoubleClick;
            colorPaletteLayout.Controls.Add(colorPalettePanel, 0, 0);
            colorPaletteLayout.Controls.Add(outputBox, 0, 1);
            InitializeSemaphorePreview();
            outputPanel.Controls.Add(colorPaletteLayout);
            outputPanel.Controls.Add(candidateGrid);
            candidateGrid.BringToFront();
            outputPanel.Resize += delegate { candidateGrid.Width = Math.Min(560, Math.Max(320, outputPanel.ClientSize.Width / 2)); };
            rootLayout.Controls.Add(outputPanel, 0, 4);
            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = Muted;
            statusLabel.Margin = Padding.Empty;
            Panel statusPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Background };
            Panel workPanel = new Panel { Dock = DockStyle.Right, Width = 210, Padding = new Padding(0, 5, 0, 4), Visible = false, BackColor = Background };
            workProgress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous };
            cancelWorkButton = new Button { Dock = DockStyle.Right, Width = 30, Text = "×", FlatStyle = FlatStyle.Flat, BackColor = Soft, ForeColor = Primary, TabStop = false };
            cancelWorkButton.FlatAppearance.BorderSize = 0; cancelWorkButton.Click += delegate { CancelCurrentWork(); };
            workPanel.Controls.Add(workProgress); workPanel.Controls.Add(cancelWorkButton); statusPanel.Controls.Add(statusLabel); statusPanel.Controls.Add(workPanel); workPanel.BringToFront();
            rootLayout.Controls.Add(statusPanel, 0, 5);

            tips.SetToolTip(swapButton, "结果移到输入并切换方向");
            tips.SetToolTip(helpButton, "工具说明");
            tips.SetToolTip(rulesButton, "字母表与文本规则");
            categoryPicker.SelectedIndex = 0;
            Shown += delegate { NativeMethods.SetCueBanner(inputBox, "输入"); NativeMethods.SetCueBanner(outputBox, "输出"); inputBox.Focus(); };
            SizeChanged += delegate { AdjustParameterArea(); };
            FormClosed += delegate { liveTimer.Dispose(); tips.Dispose(); };
            AllowDrop = true;
            DragEnter += FilesDragEnter;
            DragDrop += FilesDragDrop;
        }

        private void PopulateTools()
        {
            SaveParameters();
            string category = categoryPicker.SelectedItem as string;
            if (tagPicker.Items.Count == 0 || tagPicker.Tag as string != category)
            {
                string previous = tagPicker.SelectedItem as string;
                tagPicker.BeginUpdate(); tagPicker.Items.Clear();
                foreach (string tag in ToolTags.AllForCategory(allTools, category)) tagPicker.Items.Add(tag);
                tagPicker.EndUpdate(); tagPicker.Tag = category; UpdatePickerDropDownWidth(tagPicker);
                tagPicker.SelectedItem = previous != null && tagPicker.Items.Contains(previous) ? previous : ToolTags.Any;
                if (tagPicker.SelectedIndex < 0 && tagPicker.Items.Count > 0) tagPicker.SelectedIndex = 0;
            }
            string selectedTag = tagPicker.SelectedItem as string;
            toolPicker.BeginUpdate();
            toolPicker.Items.Clear();
            foreach (ICryptoTool tool in allTools) if (tool.Category == category && ToolTags.Matches(tool, selectedTag)) toolPicker.Items.Add(tool);
            toolPicker.EndUpdate();
            UpdatePickerDropDownWidth(toolPicker);
            if (toolPicker.Items.Count > 0) toolPicker.SelectedIndex = 0;
        }
        private void ToolPickerSelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            SaveParameters();
            currentTool = toolPicker.SelectedItem as ICryptoTool;
            if (currentTool == null) { UpdateContextActions(); return; }
            colorPickButton.Visible = currentTool.Name == "取色器与调色盘";
            colorPalettePanel.Visible = false;
            BuildModeButtons();
            activeMode = currentTool.Modes[0];
            ApplyModeStyles();
            BuildParameterBoxes();
            UpdateContextActions();
            SetStatus(currentTool.Category + " · " + currentTool.Name, false);
            tips.SetToolTip(toolPicker, currentTool.Name + "\r\n" + ToolTags.Display(currentTool));
            ScheduleLiveUpdate();
        }
        private void SwapText()
        {
            if (outputBox.TextLength == 0) return;
            if (currentTool != null && currentTool.Name == "进制换算")
            {
                string source = parameterPickers["from"].Text;
                parameterPickers["from"].SelectedItem = parameterPickers["to"].Text;
                parameterPickers["to"].SelectedItem = source;
            }
            inputBox.Text = outputBox.Text;
            outputBox.Clear();
            if (activeMode == ToolMode.Encrypt && currentTool.Modes.Contains(ToolMode.Decrypt)) SetMode(ToolMode.Decrypt);
            else if (activeMode == ToolMode.Decrypt && currentTool.Modes.Contains(ToolMode.Encrypt)) SetMode(ToolMode.Encrypt);
            else if (activeMode == ToolMode.Encode && currentTool.Modes.Contains(ToolMode.Decode)) SetMode(ToolMode.Decode);
            else if (activeMode == ToolMode.Decode && currentTool.Modes.Contains(ToolMode.Encode)) SetMode(ToolMode.Encode);
            inputBox.Focus();
        }
        private void ClearText() { liveTimer.Stop(); executionVersion++; rerunPending = false; SetWorkProgress(false, 0, string.Empty); batchDocuments.Clear(); inputBox.Clear(); outputBox.Clear(); candidateGrid.Rows.Clear(); candidateGrid.Visible = false; colorPalettePanel.Controls.Clear(); colorPalettePanel.Visible = false; SetStatus(string.Empty, false); inputBox.Focus(); }
        private void PickColor()
        {
            using (ColorDialog dialog = new ColorDialog { FullOpen = true, AnyColor = true }) if (dialog.ShowDialog(this) == DialogResult.OK) inputBox.Text = "#" + dialog.Color.R.ToString("X2") + dialog.Color.G.ToString("X2") + dialog.Color.B.ToString("X2");
        }
        private void UpdateColorPalette(string output)
        {
            colorPalettePanel.Controls.Clear(); colorPalettePanel.Visible = currentTool != null && currentTool.Name == "取色器与调色盘" && !string.IsNullOrEmpty(output); if (!colorPalettePanel.Visible) return; int at = output.IndexOf("调色盘：", StringComparison.Ordinal); if (at < 0) { colorPalettePanel.Visible = false; return; } string line = output.Substring(at + 4).Split(new[] { '\r', '\n' })[0]; foreach (string token in line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) { try { Color color = ColorTranslator.FromHtml(token); Button swatch = new Button { Width = 82, Height = 42, Text = token, BackColor = color, ForeColor = ReadableColor(color), FlatStyle = FlatStyle.Flat, Margin = new Padding(3), TabStop = false, UseVisualStyleBackColor = false }; swatch.FlatAppearance.BorderColor = Color.FromArgb(90, 34, 41, 54); tips.SetToolTip(swatch, token + " · 点击设为当前颜色"); swatch.Click += delegate { inputBox.Text = token; }; colorPalettePanel.Controls.Add(swatch); } catch { } }
        }
        private static Color ReadableColor(Color background)
        {
            double luminance = (background.R * 299.0 + background.G * 587.0 + background.B * 114.0) / 1000.0; return luminance >= 150.0 ? Color.FromArgb(24, 28, 36) : Color.White;
        }
        private void CopyOutput()
        {
            try { if (outputBox.TextLength > 0) { Clipboard.SetText(outputBox.Text); SetStatus("已复制", false); } }
            catch (ExternalException) { SetStatus("剪贴板暂不可用", true); }
        }
        private void PasteInput()
        {
            try { if (Clipboard.ContainsText()) { inputBox.SelectedText = Clipboard.GetText(); inputBox.Focus(); } }
            catch (ExternalException) { SetStatus("剪贴板暂不可用", true); }
        }
        private void InputTextChanged(object sender, EventArgs eventArgs)
        {
            if (!loadingBatch) batchDocuments.Clear();
            ScheduleLiveUpdate();
        }
        private void ShowHelp() { using (HelpForm help = new HelpForm(currentTool == null ? null : currentTool.Name)) help.ShowDialog(this); }
        private void SetStatus(string text, bool isError) { statusLabel.Text = text; statusLabel.ForeColor = isError ? Error : Muted; tips.SetToolTip(statusLabel, text); }
    }
}
