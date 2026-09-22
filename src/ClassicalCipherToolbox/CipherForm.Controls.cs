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
    }
}
