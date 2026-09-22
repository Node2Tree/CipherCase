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
        private sealed class BatchDocument
        {
            internal BatchDocument(string name, string content) { Name = name; Content = content; }
            internal string Name { get; private set; }
            internal string Content { get; private set; }
        }
    }
}
