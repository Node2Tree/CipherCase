using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox
{
    internal sealed class CipherForm : Form
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
        private ICryptoTool currentTool;
        private ToolMode activeMode;
        private bool loadingBatch;
        private int executionVersion;

        internal CipherForm()
        {
            Text = "密码箱 1.1.6";
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
            categoryPicker.Items.AddRange(new object[] { ToolCategories.General, ToolCategories.Encoding, ToolCategories.Substitution, ToolCategories.Polyalphabetic, ToolCategories.Transposition, ToolCategories.Grid });
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
            colorPalettePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, Visible = false, BackColor = Surface, Padding = new Padding(4), WrapContents = false };
            candidateGrid.SelectionChanged += CandidateSelectionChanged;
            candidateGrid.CellDoubleClick += CandidateGridDoubleClick;
            outputPanel.Controls.Add(outputBox);
            outputPanel.Controls.Add(colorPalettePanel);
            outputPanel.Controls.Add(candidateGrid);
            colorPalettePanel.BringToFront();
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
        private void BuildModeButtons()
        {
            modePanel.Controls.Clear();
            modeButtons.Clear();
            foreach (ToolMode mode in currentTool.Modes)
            {
                ToolMode captured = mode;
                Button button = CreateButton(ToolModeInfo.Label(mode), 58, false);
                button.Margin = new Padding(0, 0, 4, 0);
                button.Click += delegate { SetMode(captured); };
                modeButtons[mode] = button;
                modePanel.Controls.Add(button);
            }
        }
        private void SetMode(ToolMode mode)
        {
            SaveParameters(); activeMode = mode; ApplyModeStyles(); BuildParameterBoxes(); UpdateContextActions();
            outputBox.Clear(); candidateGrid.Rows.Clear(); candidateGrid.Visible = false; SetStatus(string.Empty, false); ScheduleLiveUpdate();
        }
        private void ApplyModeStyles()
        {
            foreach (KeyValuePair<ToolMode, Button> item in modeButtons)
            {
                bool selected = item.Key == activeMode;
                item.Value.BackColor = selected ? Primary : Soft;
                item.Value.ForeColor = selected ? Color.White : Primary;
            }
        }
        private void BuildParameterBoxes()
        {
            parameterPanel.Controls.Clear();
            parameterBoxes.Clear();
            parameterPickers.Clear();
            if (currentTool == null) return;
            foreach (ToolParameter parameter in currentTool.Parameters)
            {
                if (!parameter.AppliesTo(activeMode)) continue;
                int measured = TextRenderer.MeasureText(parameter.Hint, Font).Width + 26;
                int cardWidth = Math.Max(190, Math.Min(520, measured));
                Control box;
                if (parameter.Id == "crib" || parameter.Id == "clue") cardWidth = Math.Max(cardWidth, 300);
                if (parameter.Id == "method") { ComboBox picker = CreatePicker(cardWidth); picker.Items.AddRange(new object[] { "AUTO", "COSINE", "LLR", "CHI", "NGRAM" }); picker.SelectedIndex = 0; picker.SelectedIndexChanged += delegate { ScheduleLiveUpdate(); }; parameterPickers[parameter.Id] = picker; box = picker; }
                else if (parameter.Id == "effort") { ComboBox picker = CreatePicker(cardWidth); picker.Items.AddRange(new object[] { "快速", "标准", "深入" }); picker.SelectedIndex = 1; picker.SelectedIndexChanged += delegate { ScheduleLiveUpdate(); }; parameterPickers[parameter.Id] = picker; box = picker; }
                else if (parameter.Id == "heuristic") { ComboBox picker = CreatePicker(cardWidth); picker.Items.AddRange(new object[] { "自动", "模拟退火", "爬山", "延迟接受", "再加热退火" }); picker.SelectedIndex = 0; picker.SelectedIndexChanged += delegate { ScheduleLiveUpdate(); }; parameterPickers[parameter.Id] = picker; box = picker; }
                else { TextBox textBox = CreateSingleLineBox(cardWidth); textBox.TextChanged += delegate { ScheduleLiveUpdate(); }; parameterBoxes[parameter.Id] = textBox; box = textBox; }
                box.Margin = Padding.Empty;
                string storageKey = ParameterStorageKey(currentTool, parameter.Id);
                if (parameterValues.ContainsKey(storageKey)) { ComboBox picker = box as ComboBox; if (picker != null && picker.Items.Contains(parameterValues[storageKey])) picker.SelectedItem = parameterValues[storageKey]; else box.Text = parameterValues[storageKey]; }
                Panel card = CreateParameterCard(parameter.Hint, box, cardWidth);
                parameterPanel.Controls.Add(card);
                tips.SetToolTip(box, parameter.Hint);
                tips.SetToolTip(card, parameter.Hint);
                TextBox cueBox = box as TextBox; if (cueBox != null && parameter.Id == "crib") NativeMethods.SetCueBanner(cueBox, "点击“明文”打开编辑器"); else if (cueBox != null && parameter.Id == "clue") NativeMethods.SetCueBanner(cueBox, "点击“线索”分别填写算法和明文");
            }
            AdjustParameterArea();
        }
        private void SaveParameters()
        {
            if (currentTool == null) return;
            foreach (KeyValuePair<string, TextBox> item in parameterBoxes) parameterValues[ParameterStorageKey(currentTool, item.Key)] = item.Value.Text;
            foreach (KeyValuePair<string, ComboBox> item in parameterPickers) parameterValues[ParameterStorageKey(currentTool, item.Key)] = item.Value.Text;
        }
        private static string ParameterStorageKey(ICryptoTool tool, string id) { return tool.Name + "|" + id; }
        private void BeginExecution()
        {
            if (currentTool == null) return;
            if (inputBox.TextLength == 0) { outputBox.Clear(); SetWorkProgress(false, 0, string.Empty); SetStatus(string.Empty, false); return; }
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (KeyValuePair<string, TextBox> item in parameterBoxes) values[item.Key] = item.Value.Text;
            foreach (KeyValuePair<string, ComboBox> item in parameterPickers) values[item.Key] = item.Value.Text;
            ICryptoTool tool = currentTool;
            ToolMode mode = activeMode;
            string input = inputBox.Text;
            TextRuleOptions rules = textRules.Copy();
            List<BatchDocument> documents = new List<BatchDocument>(batchDocuments);
            int version = ++executionVersion;
            SetWorkProgress(mode == ToolMode.Crack && SupportsProgress(tool.Name), 0, string.Empty);
            SetStatus(mode == ToolMode.Crack ? "破解中…" : mode == ToolMode.Analyze ? "分析中…" : "处理中…", false);
            Action<int, string> progress = delegate(int percent, string stage)
            {
                if (IsDisposed || !IsHandleCreated) return;
                try { BeginInvoke((MethodInvoker)delegate { if (version == executionVersion && !IsDisposed) SetWorkProgress(true, percent, stage); }); } catch (InvalidOperationException) { }
            };
            Func<bool> cancellation = delegate { return IsDisposed || version != executionVersion; };
            Action<string> partial = delegate(string partialOutput)
            {
                if (IsDisposed || !IsHandleCreated) return;
                try { BeginInvoke((MethodInvoker)delegate { if (version == executionVersion && !IsDisposed) { outputBox.Text = partialOutput; UpdateCandidatePanel(partialOutput, mode); } }); } catch (InvalidOperationException) { }
            };
            Task.Factory.StartNew(delegate
            {
                try
                {
                    StringBuilder result = new StringBuilder();
                    if (documents.Count == 0) result.Append(ExecuteWithRules(tool, mode, input, values, rules, progress, cancellation, partial));
                    else foreach (BatchDocument document in documents)
                    {
                        if (documents.Count > 1) result.Append("===== ").Append(document.Name).Append(" =====\r\n");
                        result.Append(ExecuteWithRules(tool, mode, document.Content, values, rules, progress, cancellation, null));
                        if (documents.Count > 1) result.Append("\r\n\r\n");
                    }
                    return new ExecutionResult(result.ToString().TrimEnd(), null);
                }
                catch (Exception exception)
                {
                    return new ExecutionResult(string.Empty, exception.Message);
                }
            }).ContinueWith(delegate(Task<ExecutionResult> task)
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    if (version != executionVersion || IsDisposed) return;
                    ExecutionResult result = task.Result;
                    SetWorkProgress(false, 0, string.Empty);
                    outputBox.Text = result.Output;
                    UpdateColorPalette(result.Output);
                    UpdateCandidatePanel(result.Output, mode);
                    if (result.Error == null)
                    {
                        SaveParameters();
                        SetStatus(documents.Count > 0 ? "实时 · " + documents.Count + " 个文件" : "实时", false);
                    }
                    else SetStatus(result.Error, true);
                });
            });
        }
        private void ScheduleLiveUpdate()
        {
            if (liveTimer == null || outputBox == null) return;
            liveTimer.Stop();
            executionVersion++;
            if (inputBox == null || inputBox.TextLength == 0) { outputBox.Clear(); SetStatus(string.Empty, false); return; }
            if (currentTool != null && currentTool.Name == "通用破解") { string effort = parameterPickers.ContainsKey("effort") ? parameterPickers["effort"].Text : "标准"; liveTimer.Interval = effort == "快速" ? 450 : effort == "深入" ? 1200 : 800; }
            else liveTimer.Interval = activeMode == ToolMode.Crack || activeMode == ToolMode.Analyze ? 350 : 180;
            liveTimer.Start();
        }
        private void SwapText()
        {
            if (outputBox.TextLength == 0) return;
            inputBox.Text = outputBox.Text;
            outputBox.Clear();
            if (activeMode == ToolMode.Encrypt && currentTool.Modes.Contains(ToolMode.Decrypt)) SetMode(ToolMode.Decrypt);
            else if (activeMode == ToolMode.Decrypt && currentTool.Modes.Contains(ToolMode.Encrypt)) SetMode(ToolMode.Encrypt);
            else if (activeMode == ToolMode.Encode && currentTool.Modes.Contains(ToolMode.Decode)) SetMode(ToolMode.Decode);
            else if (activeMode == ToolMode.Decode && currentTool.Modes.Contains(ToolMode.Encode)) SetMode(ToolMode.Encode);
            inputBox.Focus();
        }
        private void ClearText() { liveTimer.Stop(); executionVersion++; SetWorkProgress(false, 0, string.Empty); batchDocuments.Clear(); inputBox.Clear(); outputBox.Clear(); candidateGrid.Rows.Clear(); candidateGrid.Visible = false; colorPalettePanel.Controls.Clear(); colorPalettePanel.Visible = false; SetStatus(string.Empty, false); inputBox.Focus(); }
        private void PickColor()
        {
            using (ColorDialog dialog = new ColorDialog { FullOpen = true, AnyColor = true }) if (dialog.ShowDialog(this) == DialogResult.OK) inputBox.Text = "#" + dialog.Color.R.ToString("X2") + dialog.Color.G.ToString("X2") + dialog.Color.B.ToString("X2");
        }
        private void UpdateColorPalette(string output)
        {
            colorPalettePanel.Controls.Clear(); colorPalettePanel.Visible = currentTool != null && currentTool.Name == "取色器与调色盘" && !string.IsNullOrEmpty(output); if (!colorPalettePanel.Visible) return; int at = output.IndexOf("调色盘：", StringComparison.Ordinal); if (at < 0) return; string line = output.Substring(at + 4).Split(new[] { '\r', '\n' })[0]; foreach (string token in line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) { try { Color color = ColorTranslator.FromHtml(token); Panel swatch = new Panel { Width = 72, Height = 42, BackColor = color, Margin = new Padding(3) }; tips.SetToolTip(swatch, token); swatch.Click += delegate { inputBox.Text = token; }; colorPalettePanel.Controls.Add(swatch); } catch { } }
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
        private void OpenFiles()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Filter = "文本文件|*.txt;*.log;*.csv;*.md;*.json;*.xml|所有文件|*.*";
                if (dialog.ShowDialog(this) == DialogResult.OK) LoadFiles(dialog.FileNames);
            }
        }
        private void FilesDragEnter(object sender, DragEventArgs eventArgs)
        {
            if (eventArgs.Data.GetDataPresent(DataFormats.FileDrop)) eventArgs.Effect = DragDropEffects.Copy;
        }
        private void FilesDragDrop(object sender, DragEventArgs eventArgs)
        {
            string[] files = eventArgs.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null) LoadFiles(files);
        }
        private void LoadFiles(string[] files)
        {
            batchDocuments.Clear();
            try
            {
                foreach (string file in files)
                    if (File.Exists(file)) batchDocuments.Add(new BatchDocument(Path.GetFileName(file), File.ReadAllText(file)));
                if (batchDocuments.Count == 0) throw new CipherException("未找到可读取的文件");
                StringBuilder preview = new StringBuilder();
                foreach (BatchDocument document in batchDocuments)
                {
                    if (batchDocuments.Count > 1) preview.Append("===== ").Append(document.Name).Append(" =====\r\n");
                    preview.Append(document.Content);
                    if (batchDocuments.Count > 1) preview.Append("\r\n\r\n");
                }
                loadingBatch = true;
                inputBox.Text = preview.ToString().TrimEnd();
                loadingBatch = false;
                SetStatus("已载入 " + batchDocuments.Count + " 个文件", false);
                ScheduleLiveUpdate();
            }
            catch (Exception exception)
            {
                loadingBatch = false; batchDocuments.Clear(); SetStatus("读取失败：" + exception.Message, true);
            }
        }
        private static string ExecuteWithRules(ICryptoTool tool, ToolMode mode, string input, IDictionary<string, string> values, TextRuleOptions rules, Action<int, string> progress, Func<bool> cancellation, Action<string> partial)
        {
            string working = mode == ToolMode.Encode || mode == ToolMode.Decode || tool.Category == ToolCategories.Encoding ? input ?? string.Empty : TextRules.ToWorking(input, rules);
            string output = tool.Execute(new ToolRequest(mode, working, values, progress, cancellation, partial));
            return mode == ToolMode.Encrypt || mode == ToolMode.Decrypt ? TextRules.FromWorking(output, rules) : output;
        }
        private void ShowTextRules()
        {
            using (TextRulesForm dialog = new TextRulesForm(textRules))
                if (dialog.ShowDialog(this) == DialogResult.OK) { textRules = dialog.Options; SetStatus("文本规则已更新", false); ScheduleLiveUpdate(); }
        }
        private void UpdateContextActions()
        {
            if (identifyButton == null) return;
            bool valid = currentTool != null;
            identifyButton.Visible = valid && currentTool.Name != "密码识别器";
            universalButton.Visible = valid && currentTool.Name != "通用破解";
            bool hasCrib = false, hasClue = false;
            if (valid) foreach (ToolParameter parameter in currentTool.Parameters) if (parameter.AppliesTo(activeMode)) { if (parameter.Id == "crib") hasCrib = true; if (parameter.Id == "clue") hasClue = true; }
            clueButton.Visible = hasCrib || hasClue;
            clueButton.Text = hasCrib ? "明文" : "线索";
            tips.SetToolTip(identifyButton, "保留输入并进入密码识别器");
            tips.SetToolTip(universalButton, "保留输入并进入通用破解");
            tips.SetToolTip(clueButton, hasCrib ? "编辑已知明文片段" : "分别编辑算法提示和已知明文");
        }
        private void NavigateToTool(string name, ToolMode preferredMode)
        {
            ICryptoTool target = null; foreach (ICryptoTool tool in allTools) if (string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase)) { target = tool; break; }
            if (target == null) return;
            if (tagPicker.Items.Contains(ToolTags.Any)) tagPicker.SelectedItem = ToolTags.Any;
            categoryPicker.SelectedItem = target.Category;
            if (tagPicker.Items.Contains(ToolTags.Any)) tagPicker.SelectedItem = ToolTags.Any;
            for (int i = 0; i < toolPicker.Items.Count; i++) if (ReferenceEquals(toolPicker.Items[i], target) || string.Equals(toolPicker.Items[i].ToString(), target.Name, StringComparison.OrdinalIgnoreCase)) { toolPicker.SelectedIndex = i; break; }
            if (target.Modes.Contains(preferredMode)) SetMode(preferredMode);
            inputBox.Focus();
        }
        private void ShowKnownPlaintextEditor()
        {
            if (currentTool == null) return; string id = parameterBoxes.ContainsKey("crib") ? "crib" : parameterBoxes.ContainsKey("clue") ? "clue" : string.Empty; if (id.Length == 0) return;
            TextBox field = parameterBoxes[id]; bool structured = id == "clue"; string selected = outputBox.SelectedText;
            using (KnownPlaintextDialog dialog = new KnownPlaintextDialog(currentTool.Name, field.Text, selected, structured))
                if (dialog.ShowDialog(this) == DialogResult.OK) { field.Text = dialog.Value; field.Focus(); field.SelectionStart = field.TextLength; ScheduleLiveUpdate(); }
        }
        private void UpdateCandidatePanel(string output, ToolMode mode)
        {
            candidateGrid.Rows.Clear();
            if (mode != ToolMode.Crack && (currentTool == null || currentTool.Name != "密码识别器")) { candidateGrid.Visible = false; return; }
            List<string> blocks = CandidateBlocks(output);
            foreach (string block in blocks)
            {
                string[] lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None); string header = lines.Length > 0 ? lines[0] : string.Empty;
                string rank = Between(header, "#", "  "), key = Field(header, "密钥 "); if (key.Length == 0) key = Field(header, "类型 "); if (key.Length == 0) key = Field(header, "位置 ");
                string score = Field(header, "语言分 "); if (score.Length == 0) score = Field(header, "自然 "); if (score.Length == 0) score = Field(header, "评分 "); if (score.Length == 0) score = Field(header, "匹配 "); if (score.Length == 0) score = Field(header, "置信 "); string preview = string.Empty;
                for (int i = 1; i < lines.Length; i++) if (!string.IsNullOrWhiteSpace(lines[i]) && lines[i].IndexOf("表：", StringComparison.Ordinal) < 0) { preview = lines[i].Trim(); break; }
                int row = candidateGrid.Rows.Add(rank, key, score, preview); candidateGrid.Rows[row].Tag = block.Trim();
            }
            candidateGrid.Visible = candidateGrid.Rows.Count > 0;
            if (candidateGrid.Visible && candidateGrid.Rows.Count > 0) candidateGrid.Rows[0].Selected = true;
        }
        private void CandidateSelectionChanged(object sender, EventArgs eventArgs)
        {
            if (!candidateGrid.Visible || candidateGrid.SelectedRows.Count == 0) return; string block = candidateGrid.SelectedRows[0].Tag as string; if (!string.IsNullOrEmpty(block)) outputBox.Text = CandidateText(block);
        }
        private void CandidateGridDoubleClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (candidateGrid.SelectedRows.Count == 0) return; string block = candidateGrid.SelectedRows[0].Tag as string; if (string.IsNullOrEmpty(block)) return; string header = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0], name = ResolveCandidateToolName(Field(header, "类型 ")); ICryptoTool target = null; foreach (ICryptoTool tool in allTools) if (string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase) && (tool.Modes.Contains(ToolMode.Crack) || tool.Modes.Contains(ToolMode.Decode))) { target = tool; break; } if (target == null) return;
            NavigateToTool(target.Name, target.Modes.Contains(ToolMode.Crack) ? ToolMode.Crack : ToolMode.Decode); if (parameterBoxes.ContainsKey("language")) parameterBoxes["language"].Text = header.IndexOf("·ZH", StringComparison.OrdinalIgnoreCase) >= 0 || header.IndexOf("中文编码", StringComparison.Ordinal) >= 0 ? "ZH" : "AUTO";
        }
        private static string ResolveCandidateToolName(string value)
        {
            string name = value ?? string.Empty; int separator = name.IndexOf('·'); if (separator >= 0) name = name.Substring(0, separator);
            if (name.IndexOf("中文编码单表", StringComparison.Ordinal) >= 0 || name.IndexOf("单表替换", StringComparison.Ordinal) >= 0) return "单表替换";
            if (name.IndexOf("Gronsfeld", StringComparison.OrdinalIgnoreCase) >= 0) return "Gronsfeld"; if (name.IndexOf("维吉尼亚", StringComparison.Ordinal) >= 0) return "维吉尼亚";
            if (name.IndexOf("Fractionated Morse", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("分数化", StringComparison.Ordinal) >= 0) return "Fractionated Morse";
            if (name.IndexOf("Playfair", StringComparison.OrdinalIgnoreCase) >= 0) return "Playfair"; if (name.IndexOf("Bifid", StringComparison.OrdinalIgnoreCase) >= 0) return "Bifid";
            if (name.IndexOf("Polybius", StringComparison.OrdinalIgnoreCase) >= 0) return "Polybius"; if (name.IndexOf("Morbit", StringComparison.OrdinalIgnoreCase) >= 0) return "Morbit";
            if (name.IndexOf("Autokey", StringComparison.OrdinalIgnoreCase) >= 0) return "Autokey"; if (name.IndexOf("Scytale", StringComparison.OrdinalIgnoreCase) >= 0) return "Scytale";
            if (name.IndexOf("同音替换", StringComparison.Ordinal) >= 0) return "同音替换"; if (name.IndexOf("栅栏", StringComparison.Ordinal) >= 0) return "栅栏"; if (name.IndexOf("列换位", StringComparison.Ordinal) >= 0 || name == "换位密码") return "列换位";
            int slash = name.IndexOf(" / ", StringComparison.Ordinal); return slash > 0 ? name.Substring(0, slash).Trim() : name.Trim();
        }
        private static List<string> CandidateBlocks(string output)
        {
            List<string> blocks = new List<string>(); StringBuilder current = null; string[] lines = (output ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines) { if (line.StartsWith("#", StringComparison.Ordinal) && line.Length > 1 && char.IsDigit(line[1])) { if (current != null) blocks.Add(current.ToString()); current = new StringBuilder(); } if (current != null) current.AppendLine(line); }
            if (current != null) blocks.Add(current.ToString()); return blocks;
        }
        private static string CandidateText(string block)
        {
            string[] lines = (block ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None); StringBuilder result = new StringBuilder();
            for (int i = 1; i < lines.Length; i++) { if (lines[i].StartsWith("密文表：", StringComparison.Ordinal) || lines[i].StartsWith("明文表：", StringComparison.Ordinal)) continue; if (result.Length > 0) result.AppendLine(); result.Append(lines[i]); }
            return result.ToString().Trim();
        }
        private static string Between(string text, string start, string end) { int a = text.IndexOf(start, StringComparison.Ordinal); if (a < 0) return string.Empty; a += start.Length; int b = text.IndexOf(end, a, StringComparison.Ordinal); return (b < 0 ? text.Substring(a) : text.Substring(a, b - a)).Trim(); }
        private static string Field(string header, string label) { int start = header.IndexOf(label, StringComparison.Ordinal); if (start < 0) return string.Empty; start += label.Length; int end = header.IndexOf("  ", start, StringComparison.Ordinal); return (end < 0 ? header.Substring(start) : header.Substring(start, end - start)).Trim(); }
        private void ShowHelp() { using (HelpForm help = new HelpForm()) help.ShowDialog(this); }
        private void SetStatus(string text, bool isError) { statusLabel.Text = text; statusLabel.ForeColor = isError ? Error : Muted; tips.SetToolTip(statusLabel, text); }
        private void SetWorkProgress(bool visible, int percent, string stage)
        {
            Control panel = workProgress.Parent; panel.Visible = visible; if (!visible) return; workProgress.Value = Math.Max(0, Math.Min(100, percent)); if (!string.IsNullOrEmpty(stage)) SetStatus(stage + "  " + percent + "%", false);
        }
        private void CancelCurrentWork() { liveTimer.Stop(); executionVersion++; SetWorkProgress(false, 0, string.Empty); SetStatus("已取消", false); }
        private static bool SupportsProgress(string name)
        {
            switch (name)
            {
                case "通用破解": case "单表替换": case "Keyword Cipher": case "列换位": case "Hill 2×2": case "Morbit": case "Myszkowski": case "AMSCO": case "Autokey": case "Playfair": case "ADFGX": case "ADFGVX": case "Fractionated Morse": case "Nihilist": case "跨行棋盘": case "Polybius": case "Bifid": case "同音替换": case "Turning Grille": case "Two-square": case "Four-square": case "Trifid": case "双重列换位": case "Ubchi": return true;
                default: return false;
            }
        }
        private void AdjustParameterArea()
        {
            if (rootLayout == null || parameterPanel == null) return; int available = parameterPanel.ClientSize.Width; if (available < 200) available = Math.Max(200, ClientSize.Width - 48); int rows = 1, used = 0;
            foreach (Control control in parameterPanel.Controls) { int width = control.Width + control.Margin.Horizontal; if (used > 0 && used + width > available) { rows++; used = 0; } used += width; }
            float height = parameterPanel.Controls.Count == 0 ? 46F : Math.Min(220F, 10F + rows * 66F); if (Math.Abs(rootLayout.RowStyles[1].Height - height) > 0.5F) rootLayout.RowStyles[1].Height = height;
        }
        private static void UpdatePickerDropDownWidth(ComboBox picker)
        {
            int width = picker.Width; foreach (object item in picker.Items) width = Math.Max(width, TextRenderer.MeasureText(item == null ? string.Empty : item.ToString(), picker.Font).Width + 34); picker.DropDownWidth = Math.Min(520, width);
        }
        private static FlowLayoutPanel CreateBar(FlowDirection direction, int verticalPadding)
        {
            FlowLayoutPanel bar = new FlowLayoutPanel(); bar.Dock = DockStyle.Fill; bar.FlowDirection = direction; bar.WrapContents = false;
            bar.Padding = new Padding(0, verticalPadding, 0, verticalPadding); bar.Margin = Padding.Empty; bar.BackColor = Background; return bar;
        }
        private static ComboBox CreatePicker(int width)
        {
            ComboBox picker = new ComboBox(); picker.DropDownStyle = ComboBoxStyle.DropDownList; picker.FlatStyle = FlatStyle.Flat;
            picker.BackColor = Surface; picker.ForeColor = Primary; picker.Font = new Font("Microsoft YaHei UI", 10F); picker.Width = width; return picker;
        }
        private static TextBox CreateSingleLineBox(int width)
        {
            TextBox box = new TextBox(); box.Width = width; box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Surface; box.ForeColor = Primary; box.Font = new Font("Consolas", 11F); return box;
        }
        private static Panel CreateParameterCard(string hint, Control box, int width)
        {
            Panel card = new Panel(); card.Width = width; card.Height = 60; card.Margin = new Padding(0, 0, 10, 4); card.BackColor = Background;
            Label label = new Label(); label.Text = hint; label.Dock = DockStyle.Top; label.Height = 24; label.TextAlign = ContentAlignment.BottomLeft; label.AutoEllipsis = false; label.ForeColor = Muted; label.Font = new Font("Microsoft YaHei UI", 8.5F); label.Padding = new Padding(1, 0, 0, 2);
            box.Dock = DockStyle.Bottom; card.Controls.Add(box); card.Controls.Add(label); return card;
        }
        private static TextBox CreateTextArea(bool readOnly)
        {
            TextBox box = new TextBox(); box.Dock = DockStyle.Fill; box.Multiline = true; box.AcceptsReturn = true; box.AcceptsTab = true;
            box.ScrollBars = ScrollBars.Vertical; box.BorderStyle = BorderStyle.FixedSingle; box.BackColor = Surface; box.ForeColor = Primary;
            box.Font = new Font("Consolas", 12F); box.ReadOnly = readOnly; return box;
        }
        private static Button CreateButton(string text, int width, bool accent)
        {
            Button button = new Button(); button.Text = text; button.Width = width; button.Height = 34; button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0; button.Cursor = Cursors.Hand; button.BackColor = accent ? Accent : Soft;
            button.ForeColor = accent ? Color.White : Primary; button.UseVisualStyleBackColor = false; return button;
        }
        private static DataGridView CreateCandidateGrid()
        {
            DataGridView grid = new DataGridView(); grid.Dock = DockStyle.Left; grid.Width = 430; grid.Visible = false; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false; grid.RowHeadersVisible = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.BackgroundColor = Surface; grid.BorderStyle = BorderStyle.FixedSingle; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; grid.ShowCellToolTips = true;
            grid.Columns.Add("rank", "#"); grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None; grid.Columns[0].Width = 38; grid.Columns.Add("key", "密钥 / 类型"); grid.Columns[1].FillWeight = 42; grid.Columns.Add("score", "评分"); grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None; grid.Columns[2].Width = 70; grid.Columns.Add("preview", "预览"); grid.Columns[3].FillWeight = 58; return grid;
        }
        private sealed class BatchDocument
        {
            internal BatchDocument(string name, string content) { Name = name; Content = content; }
            internal string Name { get; private set; }
            internal string Content { get; private set; }
        }
        private sealed class ExecutionResult
        {
            internal ExecutionResult(string output, string error) { Output = output; Error = error; }
            internal string Output { get; private set; }
            internal string Error { get; private set; }
        }
        private sealed class KnownPlaintextDialog : Form
        {
            private readonly TextBox algorithmBox;
            private readonly TextBox plaintextBox;
            private readonly bool structured;

            internal KnownPlaintextDialog(string toolName, string current, string selectedOutput, bool structuredValue)
            {
                structured = structuredValue; Text = (structured ? "线索" : "已知明文") + " · " + toolName; StartPosition = FormStartPosition.CenterParent; MinimumSize = new Size(520, 330); ClientSize = new Size(680, 430); BackColor = Background; Font = new Font("Microsoft YaHei UI", 9F); AutoScaleMode = AutoScaleMode.Dpi;
                TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = structured ? 5 : 3, BackColor = Background };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); Controls.Add(layout);
                algorithmBox = CreateSingleLineBox(400); algorithmBox.Dock = DockStyle.Fill;
                plaintextBox = CreateTextArea(false); plaintextBox.Font = new Font("Consolas", 11F);
                string algorithm, plain; SplitValue(current, out algorithm, out plain); algorithmBox.Text = algorithm; plaintextBox.Text = plain;
                int row = 0;
                if (structured)
                {
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F)); layout.Controls.Add(DialogLabel("算法（可留空）"), 0, row++);
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F)); layout.Controls.Add(algorithmBox, 0, row++);
                }
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); layout.Controls.Add(DialogLabel(structured ? "已知明文（可留空）" : PlainHint(toolName)), 0, row++);
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); layout.Controls.Add(plaintextBox, 0, row++);
                FlowLayoutPanel actions = CreateBar(FlowDirection.RightToLeft, 8); actions.Padding = new Padding(0, 8, 0, 0);
                Button ok = CreateButton("确定", 72, true); ok.DialogResult = DialogResult.OK; actions.Controls.Add(ok);
                Button cancel = CreateButton("取消", 72, false); cancel.DialogResult = DialogResult.Cancel; cancel.Margin = new Padding(6, 0, 0, 0); actions.Controls.Add(cancel);
                if (!string.IsNullOrWhiteSpace(selectedOutput)) { Button use = CreateButton("使用选中", 92, false); use.Margin = new Padding(6, 0, 0, 0); use.Click += delegate { plaintextBox.Text = selectedOutput; plaintextBox.Focus(); }; actions.Controls.Add(use); }
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); layout.Controls.Add(actions, 0, row);
                AcceptButton = ok; CancelButton = cancel; Shown += delegate { plaintextBox.Focus(); plaintextBox.SelectionStart = plaintextBox.TextLength; };
            }

            internal string Value
            {
                get
                {
                    string plain = plaintextBox.Text.Trim(); if (!structured) return plain; StringBuilder value = new StringBuilder(); string algorithm = algorithmBox.Text.Trim(); if (algorithm.Length > 0) value.Append("算法：").Append(algorithm); if (plain.Length > 0) { if (value.Length > 0) value.AppendLine(); value.Append("明文：").Append(plain); } return value.ToString();
                }
            }
            private static Label DialogLabel(string text) { return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Muted, Padding = new Padding(0, 0, 0, 4) }; }
            private static string PlainHint(string toolName)
            {
                if (toolName == "Hill 3×3") return "从密文开头对齐的明文（至少 9 个字母）";
                if (toolName == "Running Key") return "完整明文，或从开头对齐的片段";
                if (toolName == "Enigma") return "会在密文各位置搜索的连续明文片段";
                return "连续明文片段";
            }
            private void SplitValue(string current, out string algorithm, out string plain)
            {
                algorithm = string.Empty; plain = string.Empty; if (!structured) { plain = current ?? string.Empty; return; }
                StringBuilder rest = new StringBuilder(); foreach (string raw in (current ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)) { string line = raw.Trim(); if (line.StartsWith("算法：", StringComparison.Ordinal) || line.StartsWith("算法:", StringComparison.Ordinal)) algorithm = line.Substring(3).Trim(); else if (line.StartsWith("明文：", StringComparison.Ordinal) || line.StartsWith("明文:", StringComparison.Ordinal)) { if (rest.Length > 0) rest.AppendLine(); rest.Append(line.Substring(3).Trim()); } else { if (rest.Length > 0) rest.AppendLine(); rest.Append(line); } } plain = rest.ToString();
            }
        }
    }
}
