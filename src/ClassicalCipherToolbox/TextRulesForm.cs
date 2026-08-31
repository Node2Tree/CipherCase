using System;
using System.Drawing;
using System.Windows.Forms;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox
{
    internal sealed class TextRulesForm : Form
    {
        private readonly TextBox alphabet;
        private readonly CheckBox preserveCase, preserveSpaces, preservePunctuation, mergeIJ, removeDiacritics;
        internal TextRuleOptions Options { get; private set; }

        internal TextRulesForm(TextRuleOptions source)
        {
            Text = "字母表与文本规则"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(460, 290); Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96F, 96F);
            Options = source.Copy(); TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 8 }; Controls.Add(root);
            root.Controls.Add(new Label { Text = "26 字符字母表", AutoSize = true }); alphabet = new TextBox { Dock = DockStyle.Top, Text = Options.Alphabet, Font = new Font("Consolas", 11F) }; root.Controls.Add(alphabet);
            preserveCase = AddCheck(root, "保留大小写", Options.PreserveCase); preserveSpaces = AddCheck(root, "保留空白", Options.PreserveSpaces); preservePunctuation = AddCheck(root, "保留标点与其他字符", Options.PreservePunctuation); mergeIJ = AddCheck(root, "合并 I/J", Options.MergeIJ); removeDiacritics = AddCheck(root, "移除变音符号", Options.RemoveDiacritics);
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; Button ok = new Button { Text = "确定", DialogResult = DialogResult.None, Width = 72 }; Button cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 72 }; ok.Click += Save; buttons.Controls.Add(ok); buttons.Controls.Add(cancel); root.Controls.Add(buttons); AcceptButton = ok; CancelButton = cancel;
        }

        private static CheckBox AddCheck(Control parent, string text, bool value) { CheckBox box = new CheckBox { Text = text, Checked = value, AutoSize = true }; parent.Controls.Add(box); return box; }
        private void Save(object sender, EventArgs e) { try { string value = TextRules.ValidateAlphabet(alphabet.Text); Options = new TextRuleOptions { Alphabet = value, PreserveCase = preserveCase.Checked, PreserveSpaces = preserveSpaces.Checked, PreservePunctuation = preservePunctuation.Checked, MergeIJ = mergeIJ.Checked, RemoveDiacritics = removeDiacritics.Checked }; DialogResult = DialogResult.OK; Close(); } catch (CipherException exception) { MessageBox.Show(this, exception.Message, "密码箱", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
    }
}
