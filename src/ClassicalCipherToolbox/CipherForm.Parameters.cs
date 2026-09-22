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
                if (!parameter.AppliesTo(activeMode) || !ParameterIsVisible(parameter)) continue;
                int measured = TextRenderer.MeasureText(parameter.Hint, Font).Width + 34;
                int cardWidth = Math.Max(240, Math.Min(640, measured));
                if (parameter.Editor == ToolParameterEditor.LongTextFile) cardWidth = Math.Max(cardWidth, 420);
                if (parameter.Editor == ToolParameterEditor.Alphabet || parameter.Id == "key1" || parameter.Id == "key2" || parameter.Id == "alphabet" || parameter.Id == "locks" || parameter.Id == "partial" || parameter.Id == "plugboard" || parameter.Id == "rotors") cardWidth = Math.Max(cardWidth, 340);
                Control box, editor;
                if (parameter.Id == "crib" || parameter.Id == "clue") cardWidth = Math.Max(cardWidth, 300);
                if (parameter.Editor == ToolParameterEditor.Choice)
                {
                    ComboBox picker = CreatePicker(cardWidth); picker.Items.AddRange(parameter.Choices); if (picker.Items.Count > 0) picker.SelectedIndex = 0; UpdatePickerDropDownWidth(picker);
                    if (!string.IsNullOrEmpty(parameter.DefaultValue) && picker.Items.Contains(parameter.DefaultValue)) picker.SelectedItem = parameter.DefaultValue;
                    parameterPickers[parameter.Id] = picker; box = picker; editor = picker;
                }
                else
                {
                    TextBox textBox = CreateSingleLineBox(cardWidth); textBox.TextChanged += delegate { ScheduleLiveUpdate(); }; parameterBoxes[parameter.Id] = textBox; box = textBox; editor = textBox;
                    if (!string.IsNullOrEmpty(parameter.DefaultValue)) textBox.Text = parameter.DefaultValue;
                    if (parameter.Editor == ToolParameterEditor.LongTextFile || parameter.Editor == ToolParameterEditor.Alphabet)
                    {
                        string parameterId = parameter.Id, parameterHint = parameter.Hint; bool alphabetEditor = parameter.Editor == ToolParameterEditor.Alphabet; textBox.ReadOnly = !alphabetEditor; textBox.Cursor = Cursors.Hand;
                        Panel host = new Panel { Height = 25, BackColor = Background }; textBox.Dock = DockStyle.Fill;
                        Button edit = CreateButton("…", 34, false); edit.Dock = DockStyle.Right; edit.Height = 25; edit.Margin = Padding.Empty;
                        EventHandler openEditor = delegate { if (alphabetEditor) ShowAlphabetEditor(textBox); else ShowLongTextEditor(parameterId, parameterHint, textBox); };
                        edit.Click += openEditor; textBox.DoubleClick += openEditor; host.Controls.Add(textBox); host.Controls.Add(edit); editor = host;
                        tips.SetToolTip(edit, alphabetEditor ? "逐个填写字母表" : "编辑或打开文件"); NativeMethods.SetCueBanner(textBox, alphabetEditor ? "粘贴 26 字母或点击 …" : "双击编辑或打开文件");
                    }
                }
                box.Margin = Padding.Empty;
                string storageKey = ParameterStorageKey(currentTool, parameter.Id);
                if (parameterValues.ContainsKey(storageKey)) { ComboBox picker = box as ComboBox; if (picker != null && picker.Items.Contains(parameterValues[storageKey])) picker.SelectedItem = parameterValues[storageKey]; else box.Text = parameterValues[storageKey]; }
                ComboBox choice = box as ComboBox;
                if (choice != null)
                {
                    ToolParameter capturedParameter = parameter;
                    choice.SelectedIndexChanged += delegate
                    {
                        if (ParameterControlsVisibility(capturedParameter.Id)) { SaveParameters(); BuildParameterBoxes(); }
                        ScheduleLiveUpdate();
                    };
                }
                Panel card = CreateParameterCard(parameter.Hint + (parameter.Required ? " *" : string.Empty), editor, cardWidth);
                parameterPanel.Controls.Add(card);
                tips.SetToolTip(box, parameter.Hint);
                tips.SetToolTip(card, parameter.Hint);
                TextBox cueBox = box as TextBox; if (cueBox != null && parameter.Id == "crib") NativeMethods.SetCueBanner(cueBox, "点击“明文”打开编辑器"); else if (cueBox != null && parameter.Id == "clue") NativeMethods.SetCueBanner(cueBox, "点击“线索”分别填写算法和明文");
            }
            AdjustParameterArea();
        }
        private bool ParameterIsVisible(ToolParameter parameter)
        {
            if (string.IsNullOrEmpty(parameter.DependencyId)) return true;
            string key = ParameterStorageKey(currentTool, parameter.DependencyId), value;
            if (parameterValues.TryGetValue(key, out value)) return parameter.IsVisible(value);
            foreach (ToolParameter candidate in currentTool.Parameters)
                if (candidate.Id == parameter.DependencyId) return parameter.IsVisible(candidate.DefaultValue);
            return false;
        }
        private bool ParameterControlsVisibility(string id)
        {
            foreach (ToolParameter parameter in currentTool.Parameters) if (parameter.DependencyId == id) return true;
            return false;
        }
        private void SaveParameters()
        {
            if (currentTool == null) return;
            foreach (KeyValuePair<string, TextBox> item in parameterBoxes) parameterValues[ParameterStorageKey(currentTool, item.Key)] = item.Value.Text;
            foreach (KeyValuePair<string, ComboBox> item in parameterPickers) parameterValues[ParameterStorageKey(currentTool, item.Key)] = item.Value.Text;
        }
        private void ShowLongTextEditor(string parameterId, string hint, TextBox target)
        {
            using (LongTextParameterDialog dialog = new LongTextParameterDialog(currentTool == null ? hint : currentTool.Name + " · " + hint, target.Text))
                if (dialog.ShowDialog(this) == DialogResult.OK) { target.Text = dialog.Value; parameterValues[ParameterStorageKey(currentTool, parameterId)] = target.Text; }
        }
        private void ShowAlphabetEditor(TextBox target)
        {
            using (AlphabetParameterDialog dialog = new AlphabetParameterDialog(target.Text))
                if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.Value;
        }
        private static string ParameterStorageKey(ICryptoTool tool, string id) { return tool.Name + "|" + id; }
        private void ShowTextRules()
        {
            using (TextRulesForm dialog = new TextRulesForm(textRules))
                if (dialog.ShowDialog(this) == DialogResult.OK) { textRules = dialog.Options; SetStatus("文本规则已更新", false); ScheduleLiveUpdate(); }
        }
        private void ShowKnownPlaintextEditor()
        {
            if (currentTool == null) return; string id = parameterBoxes.ContainsKey("crib") ? "crib" : parameterBoxes.ContainsKey("clue") ? "clue" : string.Empty; if (id.Length == 0) return;
            TextBox field = parameterBoxes[id]; bool structured = id == "clue"; string selected = outputBox.SelectedText;
            using (KnownPlaintextDialog dialog = new KnownPlaintextDialog(currentTool.Name, field.Text, selected, structured))
                if (dialog.ShowDialog(this) == DialogResult.OK) { field.Text = dialog.Value; field.Focus(); field.SelectionStart = field.TextLength; ScheduleLiveUpdate(); }
        }
    }
}
