using System;
using System.Windows.Forms;

namespace ClassicalCipherToolbox
{
    internal sealed partial class CipherForm
    {
        private SemaphorePreview semaphorePreview;

        private void InitializeSemaphorePreview()
        {
            semaphorePreview = new SemaphorePreview { Dock = DockStyle.Fill, Visible = false, Margin = Padding.Empty };
            colorPaletteLayout.RowCount = 3;
            colorPaletteLayout.RowStyles.Insert(1, new RowStyle(SizeType.Absolute, 0));
            colorPaletteLayout.SetRow(outputBox, 2);
            colorPaletteLayout.Controls.Add(semaphorePreview, 0, 1);
        }

        private void UpdateSemaphorePreview(string output)
        {
            if (semaphorePreview == null) return;
            bool show = currentTool != null && currentTool.Name == "旗语" && !string.IsNullOrEmpty(output) && batchDocuments.Count <= 1;
            semaphorePreview.Visible = show;
            colorPaletteLayout.RowStyles[1].Height = show ? Math.Min(184, Math.Max(100, colorPaletteLayout.ClientSize.Height - 60)) : 0;
            semaphorePreview.SetOutput(show ? output : string.Empty, activeMode == Core.ToolMode.Encode);
        }
    }
}
