using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private static void AddGeneral(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("通用破解", ToolCategories.General, new[] { ToolMode.Crack }, new[] { LanguageParameter(), new ToolParameter("effort", "强度", false, ToolParameterEditor.Choice, "标准", new[] { "快速", "标准", "深入" }, ToolMode.Crack), new ToolParameter("clue", "算法:名称 或 明文:片段；可留空", false, ToolMode.Crack) }, delegate(ToolRequest r) { return UniversalCracker.Crack(r); }));
            tools.Add(new DelegateCryptoTool("密码识别器", ToolCategories.General, new[] { ToolMode.Analyze }, new[] { new ToolParameter("clue", "线索或关键词（可选）", false), MatchMethodParameter() }, delegate(ToolRequest r) { return CipherIdentifier.Identify(r.Input, r.Get("clue"), r.Get("method")); }));
            tools.Add(new DelegateCryptoTool("Crib 工具", ToolCategories.General, new[] { ToolMode.Analyze }, new[] { new ToolParameter("crib", "已知明文片段", true), new ToolParameter("algorithm", "算法提示（可选）", false) }, delegate(ToolRequest r) { return CribAnalysis.Analyze(r.Input, r.Get("crib"), r.Get("algorithm")); }));
        }

        private static void AddAnalysis(List<ICryptoTool> tools)
        {
            tools.Add(AnalysisTool("分析工作台", new[] { LanguageParameter(ToolMode.Analyze), MatchMethodParameter(), new ToolParameter("n", "N-gram 阶数，默认 3", false) }, delegate(ToolRequest request) { return AnalysisWorkbench.Analyze(request.Input, request.Get("language"), request.Get("n"), request.Get("method")); }));
            tools.Add(AnalysisTool("频率", new ToolParameter[0], delegate(ToolRequest request) { return ClassicalAnalysis.Frequency(request.Input); }));
            tools.Add(AnalysisTool("N-gram", new[] { new ToolParameter("n", "N，例如 2", true) }, delegate(ToolRequest request) { return ClassicalAnalysis.Ngrams(request.Input, request.Get("n")); }));
            tools.Add(AnalysisTool("重合指数", new ToolParameter[0], delegate(ToolRequest request) { return ClassicalAnalysis.IndexOfCoincidence(request.Input); }));
            tools.Add(AnalysisTool("Kasiski", new[] { new ToolParameter("length", "序列长度，例如 3", true) }, delegate(ToolRequest request) { return ClassicalAnalysis.Kasiski(request.Input, request.Get("length")); }));
        }

        private static ICryptoTool AnalysisTool(string name, IEnumerable<ToolParameter> parameters, Func<ToolRequest, string> executor)
        {
            return new DelegateCryptoTool(name, ToolCategories.General, new[] { ToolMode.Analyze }, parameters, executor);
        }
    }
}
