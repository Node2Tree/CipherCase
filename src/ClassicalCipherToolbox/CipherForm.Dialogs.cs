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
        private sealed class AlphabetParameterDialog : Form
        {
            private readonly TextBox quick;
            private readonly TextBox[] letters;
            private bool synchronizing;
            private bool quickDirty;
            private string value;

            internal AlphabetParameterDialog(string current)
            {
                Text = "单表替换 · 字母表"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false; ShowInTaskbar = false;
                MinimumSize = new Size(720, 330); ClientSize = new Size(860, 390); BackColor = Background; Font = new Font("Microsoft YaHei UI", 9F); AutoScaleMode = AutoScaleMode.Dpi;
                TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5, BackColor = Background };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); Controls.Add(root);
                root.Controls.Add(new Label { Text = "整段输入（A 对应第 1 位，Z 对应第 26 位）", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Muted }, 0, 0);
                Panel quickHost = new Panel { Dock = DockStyle.Fill, BackColor = Background };
                quick = CreateSingleLineBox(600); quick.Dock = DockStyle.Fill; quick.MaxLength = 26; quick.CharacterCasing = CharacterCasing.Upper; quick.TextChanged += delegate { if (!synchronizing) quickDirty = true; };
                Button apply = CreateButton("填入", 64, false); apply.Dock = DockStyle.Right; apply.Height = 25; apply.Click += delegate { ApplyQuick(); };
                quickHost.Controls.Add(quick); quickHost.Controls.Add(apply); root.Controls.Add(quickHost, 0, 1);
                root.Controls.Add(new Label { Text = "逐个填写", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = Muted }, 0, 2);
                TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 13, RowCount = 4, BackColor = Background, Padding = new Padding(0, 4, 0, 0) };
                for (int column = 0; column < 13; column++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 13F));
                for (int row = 0; row < 4; row++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                letters = new TextBox[26];
                for (int i = 0; i < 26; i++)
                {
                    int row = i < 13 ? 0 : 2, column = i % 13; Label label = new Label { Text = ((char)('A' + i)).ToString(), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomCenter, ForeColor = Muted };
                    TextBox box = CreateSingleLineBox(40); box.Dock = DockStyle.Fill; box.Margin = new Padding(3); box.MaxLength = 1; box.CharacterCasing = CharacterCasing.Upper; box.TextAlign = HorizontalAlignment.Center; box.Tag = i; box.TextChanged += LetterChanged;
                    letters[i] = box; grid.Controls.Add(label, column, row); grid.Controls.Add(box, column, row + 1);
                }
                root.Controls.Add(grid, 0, 3);
                FlowLayoutPanel actions = CreateBar(FlowDirection.RightToLeft, 8); actions.Padding = new Padding(0, 8, 0, 0);
                Button ok = CreateButton("确定", 72, true); ok.Click += delegate { AcceptAlphabet(); }; actions.Controls.Add(ok);
                Button cancel = CreateButton("取消", 72, false); cancel.DialogResult = DialogResult.Cancel; cancel.Margin = new Padding(6, 0, 0, 0); actions.Controls.Add(cancel); root.Controls.Add(actions, 0, 4);
                CancelButton = cancel; quick.Text = (current ?? string.Empty).Trim().ToUpperInvariant(); string initialError; if (ValidateAlphabet(quick.Text, out initialError)) ApplyQuick();
                Shown += delegate { quick.Focus(); quick.SelectAll(); };
            }

            internal string Value { get { return value ?? string.Empty; } }

            private void ApplyQuick()
            {
                string candidate = quick.Text.Trim().ToUpperInvariant(); string error; if (!ValidateAlphabet(candidate, out error)) { MessageBox.Show(this, error, "密码箱", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                synchronizing = true; for (int i = 0; i < 26; i++) letters[i].Text = candidate[i].ToString(); synchronizing = false; quickDirty = false;
            }
            private void LetterChanged(object sender, EventArgs eventArgs)
            {
                if (synchronizing) return; TextBox box = sender as TextBox; if (box == null) return; string candidate = GridAlphabet(); if (candidate.Length == 26) { synchronizing = true; quick.Text = candidate; synchronizing = false; quickDirty = false; }
                int index = (int)box.Tag; if (box.TextLength == 1 && index < 25) letters[index + 1].Focus();
            }
            private string GridAlphabet()
            {
                StringBuilder result = new StringBuilder(26); foreach (TextBox box in letters) { if (box.TextLength != 1) return string.Empty; result.Append(char.ToUpperInvariant(box.Text[0])); } return result.ToString();
            }
            private void AcceptAlphabet()
            {
                string candidate = quickDirty ? quick.Text.Trim().ToUpperInvariant() : GridAlphabet(); if (candidate.Length != 26) candidate = quick.Text.Trim().ToUpperInvariant(); string error; if (!ValidateAlphabet(candidate, out error)) { MessageBox.Show(this, error, "密码箱", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                value = candidate; DialogResult = DialogResult.OK;
            }
            private static bool ValidateAlphabet(string candidate, out string error)
            {
                if (candidate.Length != 26) { error = "请输入 26 个字母。"; return false; } HashSet<char> used = new HashSet<char>(); foreach (char c in candidate) if (c < 'A' || c > 'Z' || !used.Add(c)) { error = "字母表须由 A–Z 的 26 个不重复字母组成。"; return false; } error = string.Empty; return true;
            }
        }
        private sealed class LongTextParameterDialog : Form
        {
            private readonly TextBox editor;
            private readonly ComboBox encodingPicker;
            private readonly Label stats;
            private string loadedFile;

            internal LongTextParameterDialog(string title, string current)
            {
                Text = title; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false; ShowInTaskbar = false;
                MinimumSize = new Size(560, 360); ClientSize = new Size(760, 520); BackColor = Background; Font = new Font("Microsoft YaHei UI", 9F); AutoScaleMode = AutoScaleMode.Dpi; AllowDrop = true;
                TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 3, BackColor = Background };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); Controls.Add(layout);
                stats = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted, AutoEllipsis = true };
                editor = CreateTextArea(false); editor.Font = new Font("Consolas", 11F); editor.Text = current ?? string.Empty; editor.TextChanged += delegate { UpdateStats(); };
                FlowLayoutPanel actions = CreateBar(FlowDirection.RightToLeft, 8); actions.Padding = new Padding(0, 8, 0, 0);
                Button ok = CreateButton("确定", 72, true); ok.DialogResult = DialogResult.OK; actions.Controls.Add(ok);
                Button cancel = CreateButton("取消", 72, false); cancel.DialogResult = DialogResult.Cancel; cancel.Margin = new Padding(6, 0, 0, 0); actions.Controls.Add(cancel);
                Button clear = CreateButton("清空", 64, false); clear.Margin = new Padding(6, 0, 0, 0); clear.Click += delegate { loadedFile = string.Empty; editor.Clear(); }; actions.Controls.Add(clear);
                Button paste = CreateButton("粘贴", 64, false); paste.Margin = new Padding(6, 0, 0, 0); paste.Click += delegate { try { if (Clipboard.ContainsText()) editor.SelectedText = Clipboard.GetText(); } catch (ExternalException) { } }; actions.Controls.Add(paste);
                encodingPicker = CreatePicker(190); encodingPicker.Items.Add("自动"); encodingPicker.Items.AddRange(TransferEncoding.CharsetChoices); encodingPicker.SelectedIndex = 0; encodingPicker.Margin = new Padding(6, 0, 0, 0); actions.Controls.Add(encodingPicker);
                Button open = CreateButton("打开", 64, false); open.Margin = new Padding(6, 0, 0, 0); open.Click += delegate { OpenTextFile(); }; actions.Controls.Add(open);
                layout.Controls.Add(stats, 0, 0); layout.Controls.Add(editor, 0, 1); layout.Controls.Add(actions, 0, 2);
                DragEnter += FileDragEnter; DragDrop += FileDragDrop; AcceptButton = ok; CancelButton = cancel;
                Shown += delegate { editor.Focus(); editor.SelectionStart = editor.TextLength; UpdateStats(); };
            }

            internal string Value { get { return editor.Text; } }

            private void OpenTextFile()
            {
                using (OpenFileDialog dialog = new OpenFileDialog { Filter = "文本文件|*.txt;*.log;*.csv;*.md;*.json;*.xml|所有文件|*.*" })
                    if (dialog.ShowDialog(this) == DialogResult.OK) LoadTextFile(dialog.FileName);
            }
            private void LoadTextFile(string path)
            {
                try { editor.Text = ReadTextFile(path, encodingPicker.Text); loadedFile = Path.GetFileName(path); UpdateStats(); }
                catch (Exception exception) { MessageBox.Show(this, "读取失败：" + exception.Message, "密码箱", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            private static string ReadTextFile(string path, string encodingName)
            {
                string name = string.IsNullOrEmpty(encodingName) ? "自动" : encodingName;
                if (name == "自动") using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true)) return reader.ReadToEnd();
                return TransferEncoding.CharsetText(File.ReadAllBytes(path), name);
            }
            private void FileDragEnter(object sender, DragEventArgs eventArgs) { if (eventArgs.Data.GetDataPresent(DataFormats.FileDrop)) eventArgs.Effect = DragDropEffects.Copy; }
            private void FileDragDrop(object sender, DragEventArgs eventArgs) { string[] files = eventArgs.Data.GetData(DataFormats.FileDrop) as string[]; if (files != null && files.Length > 0) LoadTextFile(files[0]); }
            private void UpdateStats()
            {
                int lines = editor.TextLength == 0 ? 0 : editor.Lines.Length; string prefix = string.IsNullOrEmpty(loadedFile) ? string.Empty : loadedFile + " · "; stats.Text = prefix + editor.TextLength + " 字符 · " + lines + " 行";
            }
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
