using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox
{
    internal sealed class HelpForm : Form
    {
        private static readonly Color Primary = Color.FromArgb(34, 41, 54);
        private static readonly Color Muted = Color.FromArgb(102, 111, 126);
        private readonly TreeView index;
        private readonly RichTextBox details;

        internal HelpForm()
        {
            Text = "密码箱 1.1.6 · 文档"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(900, 650); MinimumSize = new Size(650, 460); BackColor = Color.White; Font = new Font("Microsoft YaHei UI", 9F); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96F, 96F);

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18, 14, 18, 14), ColumnCount = 1, RowCount = 2, BackColor = Color.White };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); Controls.Add(root);
            Label intro = new Label { Dock = DockStyle.Fill, Text = "选择工具查看说明；主窗口可用标签缩小范围。", ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = false };
            root.Controls.Add(intro, 0, 0);

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 225, FixedPanel = FixedPanel.Panel1, BackColor = Color.FromArgb(232, 236, 243) };
            root.Controls.Add(split, 0, 1);
            index = new TreeView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, HideSelection = false, FullRowSelect = true, ShowLines = false, ShowPlusMinus = true, ItemHeight = 25, BackColor = Color.FromArgb(247, 248, 250), ForeColor = Primary };
            details = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ReadOnly = true, BackColor = Color.White, ForeColor = Primary, Font = new Font("Microsoft YaHei UI", 10F), DetectUrls = true, WordWrap = true, ScrollBars = RichTextBoxScrollBars.Vertical, Padding = new Padding(18) };
            split.Panel1.Controls.Add(index); split.Panel2.Padding = new Padding(22, 12, 12, 12); split.Panel2.Controls.Add(details);

            Dictionary<string, TreeNode> groups = new Dictionary<string, TreeNode>();
            foreach (ICryptoTool tool in ToolRegistry.CreateAll())
            {
                TreeNode group; if (!groups.TryGetValue(tool.Category, out group)) { group = new TreeNode(tool.Category); group.NodeFont = new Font(Font, FontStyle.Bold); groups[tool.Category] = group; index.Nodes.Add(group); }
                TreeNode node = new TreeNode(tool.Name) { Tag = tool }; group.Nodes.Add(node);
            }
            foreach (TreeNode group in index.Nodes) group.Expand();
            index.AfterSelect += delegate(object sender, TreeViewEventArgs e) { ICryptoTool tool = e.Node.Tag as ICryptoTool; if (tool != null) Render(tool); };
            if (index.Nodes.Count > 0 && index.Nodes[0].Nodes.Count > 0)
            {
                index.SelectedNode = index.Nodes[0].Nodes[0];
                Render((ICryptoTool)index.SelectedNode.Tag);
            }
        }

        private void Render(ICryptoTool tool)
        {
            details.Clear(); Append(tool.Name, 19F, FontStyle.Bold, Primary); AppendLine();
            StringBuilder modes = new StringBuilder(); foreach (ToolMode mode in tool.Modes) { if (modes.Length > 0) modes.Append(" / "); modes.Append(ToolModeInfo.Label(mode)); }
            Append(tool.Category + "  ·  " + modes, 9F, FontStyle.Regular, Muted); AppendLine();
            Append("标签  " + ToolTags.Display(tool), 9F, FontStyle.Regular, Muted); AppendLine(); AppendLine();
            Section("用途", ToolDocumentation.GetSummary(tool.Name));
            Section("原理", ToolDocumentation.GetPrinciple(tool.Name));
            Heading("参数");
            if (tool.Parameters.Count == 0) Body("无需额外参数。\r\n");
            else foreach (ToolParameter parameter in tool.Parameters)
            {
                StringBuilder applicable = new StringBuilder(); foreach (ToolMode mode in tool.Modes) if (parameter.AppliesTo(mode)) { if (applicable.Length > 0) applicable.Append('/'); applicable.Append(ToolModeInfo.Label(mode)); }
                Body("• " + parameter.Hint + "  [" + (parameter.Required ? "必填" : "可选") + "；" + applicable + "]\r\n");
            }
            AppendLine();
            Section("操作", ToolDocumentation.GetUsage(tool));
            Section("结果解读", ToolDocumentation.GetInterpretation(tool));
            Section("示例", ToolDocumentation.GetExample(tool.Name));
            Section("注意事项", ToolDocumentation.GetNotes(tool.Name));
            Section("排错", ToolDocumentation.GetTroubleshooting(tool));
            details.SelectionStart = 0; details.ScrollToCaret();
        }

        private void Section(string title, string content) { Heading(title); Body((string.IsNullOrWhiteSpace(content) ? "暂无补充说明。" : content.Trim()) + "\r\n\r\n"); }
        private void Heading(string text) { Append(text + "\r\n", 12F, FontStyle.Bold, Primary); }
        private void Body(string text) { Append(text, 10F, FontStyle.Regular, Primary); }
        private void AppendLine() { details.AppendText("\r\n"); }
        private void Append(string text, float size, FontStyle style, Color color)
        {
            details.SelectionStart = details.TextLength; details.SelectionLength = 0; details.SelectionColor = color; details.SelectionFont = new Font("Microsoft YaHei UI", size, style); details.AppendText(text);
        }
    }
}
