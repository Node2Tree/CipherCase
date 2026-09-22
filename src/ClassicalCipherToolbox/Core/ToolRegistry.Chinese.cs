using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private static void AddChinese(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("中文编码工作台", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ChineseWorkbench.Workbench(r.Input); }));
            tools.Add(new DelegateCryptoTool("字符详情卡", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ChineseWorkbench.CharacterCard(r.Input); }));
            tools.Add(new DelegateCryptoTool("中文语言评分", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new[] { new ToolParameter("model", "中文模型", false, ToolParameterEditor.Choice, "自动", ChineseLanguageScoring.ModelChoices) }, delegate(ToolRequest r) { return ChineseLanguageScoring.Analyze(r.Input, r.Get("model")); }));
            tools.Add(new DelegateCryptoTool("中文输入法码", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("scheme", "输入法或检字方案", false, ToolParameterEditor.Choice, ChineseInputCode.SchemeChoices[0], ChineseInputCode.SchemeChoices) }, delegate(ToolRequest r) { return ChineseInputCode.Transform(r.Input, r.Get("scheme"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("中文码表工作台", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new[] { new ToolParameter("scheme", "内置码表", false, ToolParameterEditor.Choice, ChineseCodeTables.SchemeChoices[0], ChineseCodeTables.SchemeChoices), new ToolParameter("table", "可选自定义码表文件；每行“字 码”或“码 字”", false, ToolParameterEditor.LongTextFile, string.Empty, null) }, delegate(ToolRequest r) { return ChineseCodeTableWorkbench.Query(r.Input, r.Get("scheme"), r.Get("table")); }));
            tools.Add(new DelegateCryptoTool("中文语音与罗马化", ToolCategories.Chinese, new[] { ToolMode.Encode }, new[] { new ToolParameter("target", "目标方案", false, ToolParameterEditor.Choice, ChineseRomanization.TargetChoices[0], ChineseRomanization.TargetChoices) }, delegate(ToolRequest r) { return ChineseRomanization.Transform(r.Input, r.Get("target")); }));
            tools.Add(new DelegateCryptoTool("拼音格式转换", ToolCategories.Chinese, new[] { ToolMode.Encode }, new[] { new ToolParameter("target", "目标格式", false, ToolParameterEditor.Choice, ChineseRomanization.PinyinFormatChoices[0], ChineseRomanization.PinyinFormatChoices) }, delegate(ToolRequest r) { return ChineseRomanization.FormatPinyin(r.Input, r.Get("target")); }));
            tools.Add(new DelegateCryptoTool("中文编码识别", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ChineseWorkbench.Identify(r.Input); }));
            tools.Add(new DelegateCryptoTool("中文字符集对照", ToolCategories.Chinese, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ChineseWorkbench.CharsetComparison(r.Input); }));
            tools.Add(new DelegateCryptoTool("Unicode 兼容格式", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("format", "格式", false, ToolParameterEditor.Choice, UnicodeCompatibilityEncoding.Choices[0], UnicodeCompatibilityEncoding.Choices) }, delegate(ToolRequest r) { return UnicodeCompatibilityEncoding.Transform(r.Input, r.Get("format"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("中文传输格式", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("format", "格式", false, ToolParameterEditor.Choice, ChineseTransferFormats.Choices[0], ChineseTransferFormats.Choices) }, delegate(ToolRequest r) { return ChineseTransferFormats.Transform(r.Input, r.Get("format"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("历史中文字符集", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("charset", "历史代码页", false, ToolParameterEditor.Choice, ChineseHistoricalEncoding.Choices[0], ChineseHistoricalEncoding.Choices) }, delegate(ToolRequest r) { return ChineseHistoricalEncoding.Transform(r.Input, r.Get("charset"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("中文电报码", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return ChineseTelegraphCode.Transform(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("中文电码加密", ToolCategories.Chinese, new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack }, new[] { new ToolParameter("key", "密钥数 0–9999", true, ToolMode.Encrypt, ToolMode.Decrypt), new ToolParameter("model", "中文模型", false, ToolParameterEditor.Choice, "自动", ChineseLanguageScoring.ModelChoices, ToolMode.Crack) }, delegate(ToolRequest r) { return r.Mode == ToolMode.Crack ? ChineseTelegraphCipher.Crack(r) : ChineseTelegraphCipher.Transform(r.Input, r.Get("key"), r.Mode == ToolMode.Decrypt); }));
            tools.Add(new DelegateCryptoTool("反切码", ToolCategories.Chinese, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return FanqieWorkbench.Transform(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("中文代码本", ToolCategories.Chinese, new[] { ToolMode.Encrypt, ToolMode.Decrypt }, new[] { new ToolParameter("map", "代码本；每行“词组=代码”", true, ToolParameterEditor.LongTextFile, string.Empty, null) }, delegate(ToolRequest r) { return ChineseCodebookCipher.Transform(r.Input, r.Get("map"), r.Mode == ToolMode.Decrypt); }));
            tools.Add(new DelegateCryptoTool("中文隐写分析", ToolCategories.Chinese, new[] { ToolMode.Crack }, new[] { new ToolParameter("path", "读取方式", false, ToolParameterEditor.Choice, "自动", ChineseSteganalysis.PathChoices), new ToolParameter("width", "已知矩阵宽度；自动可留空", false), new ToolParameter("model", "中文模型", false, ToolParameterEditor.Choice, "自动", ChineseLanguageScoring.ModelChoices, ToolMode.Crack) }, delegate(ToolRequest r) { return ChineseSteganalysis.Analyze(r); }));
        }
    }
}
