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
        private void BeginExecution()
        {
            if (currentTool == null) return;
            if (inputBox.TextLength == 0) { outputBox.Clear(); SetWorkProgress(false, 0, string.Empty); SetStatus(string.Empty, false); return; }
            if (workRunning) { rerunPending = true; SetStatus("正在停止旧任务…", false); return; }
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (KeyValuePair<string, TextBox> item in parameterBoxes) values[item.Key] = item.Value.Text;
            foreach (KeyValuePair<string, ComboBox> item in parameterPickers) values[item.Key] = item.Value.Text;
            ICryptoTool tool = currentTool;
            ToolMode mode = activeMode;
            string input = inputBox.Text;
            TextRuleOptions rules = textRules.Copy();
            List<BatchDocument> documents = new List<BatchDocument>(batchDocuments);
            int version = ++executionVersion;
            workRunning = true; rerunPending = false;
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
                    if (IsDisposed) return;
                    bool stale = version != executionVersion; workRunning = false;
                    ExecutionResult result = task.Result;
                    if (!stale)
                    {
                        SetWorkProgress(false, 0, string.Empty);
                        outputBox.Text = result.Output;
                        UpdateColorPalette(result.Output);
                        UpdateSemaphorePreview(result.Output);
                        UpdateCandidatePanel(result.Output, mode);
                        if (result.Error == null)
                        {
                            SaveParameters();
                            SetStatus(documents.Count > 0 ? "实时 · " + documents.Count + " 个文件" : "实时", false);
                        }
                        else SetStatus(result.Error, true);
                    }
                    if (rerunPending) { rerunPending = false; ScheduleLiveUpdate(); }
                });
            });
        }
        private void ScheduleLiveUpdate()
        {
            if (liveTimer == null || outputBox == null) return;
            liveTimer.Stop();
            UpdateSemaphorePreview(string.Empty);
            executionVersion++;
            if (inputBox == null || inputBox.TextLength == 0) { rerunPending = false; outputBox.Clear(); SetStatus(string.Empty, false); return; }
            if (workRunning) { rerunPending = true; SetStatus("正在停止旧任务…", false); return; }
            if (currentTool != null && currentTool.Name == "通用破解") { string effort = parameterPickers.ContainsKey("effort") ? parameterPickers["effort"].Text : "标准"; liveTimer.Interval = effort == "快速" ? 600 : effort == "深入" ? 1400 : 900; }
            else if (activeMode == ToolMode.Crack && currentTool != null && SupportsProgress(currentTool.Name)) liveTimer.Interval = 700;
            else liveTimer.Interval = activeMode == ToolMode.Crack || activeMode == ToolMode.Analyze ? 400 : 180;
            liveTimer.Start();
        }
        private static string ExecuteWithRules(ICryptoTool tool, ToolMode mode, string input, IDictionary<string, string> values, TextRuleOptions rules, Action<int, string> progress, Func<bool> cancellation, Action<string> partial)
        {
            string working = mode == ToolMode.Encode || mode == ToolMode.Decode || tool.Category == ToolCategories.Encoding || tool.Category == ToolCategories.Chinese ? input ?? string.Empty : TextRules.ToWorking(input, rules);
            string output = tool.Execute(new ToolRequest(mode, working, values, progress, cancellation, partial));
            return mode == ToolMode.Encrypt || mode == ToolMode.Decrypt ? TextRules.FromWorking(output, rules) : output;
        }
        private void SetWorkProgress(bool visible, int percent, string stage)
        {
            Control panel = workProgress.Parent; panel.Visible = visible; if (!visible) return; workProgress.Value = Math.Max(0, Math.Min(100, percent)); if (!string.IsNullOrEmpty(stage)) SetStatus(stage + "  " + percent + "%", false);
        }
        private void CancelCurrentWork() { liveTimer.Stop(); executionVersion++; rerunPending = false; SetWorkProgress(false, 0, string.Empty); SetStatus("已取消", false); }
        private static bool SupportsProgress(string name)
        {
            switch (name)
            {
                case "通用破解": case "单表替换": case "Keyword Cipher": case "中文电码加密": case "列换位": case "Hill 2×2": case "Morbit": case "Myszkowski": case "AMSCO": case "Autokey": case "Playfair": case "ADFGX": case "ADFGVX": case "Fractionated Morse": case "Nihilist": case "跨行棋盘": case "Polybius": case "Bifid": case "同音替换": case "Turning Grille": case "Two-square": case "Four-square": case "Trifid": case "双重列换位": case "Ubchi": return true;
                default: return false;
            }
        }
        private sealed class ExecutionResult
        {
            internal ExecutionResult(string output, string error) { Output = output; Error = error; }
            internal string Output { get; private set; }
            internal string Error { get; private set; }
        }
    }
}
