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
    }
}
