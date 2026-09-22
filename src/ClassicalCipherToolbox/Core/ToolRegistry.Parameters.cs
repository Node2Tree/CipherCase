using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private static ToolParameter LanguageParameter()
        {
            return LanguageParameter(ToolMode.Crack);
        }

        private static ToolParameter LanguageParameter(ToolMode mode)
        {
            return new ToolParameter("language", "语言", false, ToolParameterEditor.Choice, "AUTO",
                new[] { "AUTO", "ZH", "EN", "FR", "DE", "ES", "IT", "PT", "NL", "SV", "PL", "TR" }, mode);
        }

        private static ToolParameter SearchIterations()
        {
            return new ToolParameter("iterations", "搜索次数", false, ToolMode.Crack);
        }

        private static ToolParameter SearchHeuristic()
        {
            return new ToolParameter("heuristic", "搜索策略", false, ToolParameterEditor.Choice, "自动",
                new[] { "自动", "模拟退火", "爬山", "延迟接受", "再加热退火", "阈值接受", "大洪水", "记录到记录", "自适应退火" }, ToolMode.Crack);
        }

        private static ToolParameter SearchRestarts()
        {
            return new ToolParameter("restarts", "随机重启次数", false, ToolMode.Crack);
        }

        private static ToolParameter MatchMethodParameter()
        {
            return MatchMethodParameter(ToolMode.Analyze);
        }

        private static ToolParameter MatchMethodParameter(ToolMode mode)
        {
            return new ToolParameter("method", "语言匹配", false, ToolParameterEditor.Choice, "AUTO",
                new[] { "AUTO", "COSINE", "LLR", "CHI", "NGRAM" }, mode);
        }
    }
}
