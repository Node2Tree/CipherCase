using System;
using System.Collections.Generic;

namespace ClassicalCipherToolbox.Core
{
    internal static class ToolTags
    {
        internal const string Any = "不限标签";
        private static readonly string[] Order = { "常用", "可破解", "自动", "已知明文", "无密钥", "快速", "搜索型", "长搜索", "中文", "Unicode", "图形" };
        private static readonly HashSet<string> Popular = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "通用破解", "密码识别器", "分析工作台", "自动解码", "Base64", "十六进制", "URL 编码", "Morse", "中文输入法码",
            "凯撒", "ROT13", "Atbash", "单表替换", "维吉尼亚", "Autokey", "栅栏", "列换位", "Polybius", "Playfair"
        };
        private static readonly HashSet<string> Automatic = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "通用破解", "密码识别器", "自动解码", "分析工作台", "频率", "重合指数", "N-gram", "Kasiski"
        };
        private static readonly HashSet<string> ShortText = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "自动解码", "Base64", "Base64URL", "Base32", "Base58", "十六进制", "二进制", "URL 编码", "Unicode 转义",
            "HTML 实体", "Quoted-Printable", "ASCII85", "Punycode", "Morse", "A1Z26", "中文输入法码", "凯撒", "ROT13", "ROT-N", "Atbash", "仿射", "Reverse"
        };
        private static readonly HashSet<string> Chinese = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "通用破解", "密码识别器", "分析工作台", "频率", "N-gram", "单表替换", "Keyword Cipher", "字符集字节", "Unicode 转义", "中文电报码", "中文输入法码", "颜色编码"
        };
        private static readonly HashSet<string> Unicode = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "通用破解", "密码识别器", "分析工作台", "频率", "N-gram", "单表替换", "字符集字节", "Unicode 转义", "中文输入法码", "HTML 实体", "盲文（英语一级）", "旗语", "猪圈密码符号"
        };
        private static readonly HashSet<string> Visual = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "QR Code", "条形码", "盲文（英语一级）", "颜色编码", "取色器与调色盘", "旗语", "猪圈密码符号"
        };

        internal static IList<string> AllForCategory(IEnumerable<ICryptoTool> tools, string category)
        {
            HashSet<string> found = new HashSet<string>();
            foreach (ICryptoTool tool in tools) if (tool.Category == category) foreach (string tag in Get(tool)) found.Add(tag);
            List<string> result = new List<string> { Any };
            foreach (string tag in Order) if (found.Contains(tag)) result.Add(tag);
            return result;
        }

        internal static IList<string> Get(ICryptoTool tool)
        {
            List<string> result = new List<string>();
            if (Popular.Contains(tool.Name)) result.Add("常用");
            if (tool.Modes.Contains(ToolMode.Crack)) result.Add("可破解");
            if (Automatic.Contains(tool.Name)) result.Add("自动");
            bool hasCrib = false, search = false, longSearch = false, required = false;
            foreach (ToolParameter parameter in tool.Parameters)
            {
                if (parameter.Id == "crib") hasCrib = true;
                if (parameter.Id == "iterations" || parameter.Id == "restarts") search = true;
                if (parameter.Id == "restarts") longSearch = true;
                if (parameter.Required && (parameter.AppliesTo(ToolMode.Encrypt) || parameter.AppliesTo(ToolMode.Decrypt) || parameter.AppliesTo(ToolMode.Encode) || parameter.AppliesTo(ToolMode.Decode))) required = true;
            }
            if (hasCrib) result.Add("已知明文");
            if (!required) result.Add("无密钥");
            if (ShortText.Contains(tool.Name)) result.Add("快速");
            if (search) result.Add("搜索型");
            if (longSearch) result.Add("长搜索");
            if (Chinese.Contains(tool.Name)) result.Add("中文");
            if (Unicode.Contains(tool.Name)) result.Add("Unicode");
            if (Visual.Contains(tool.Name)) result.Add("图形");
            return result;
        }

        internal static bool Matches(ICryptoTool tool, string tag)
        {
            if (string.IsNullOrEmpty(tag) || tag == Any) return true;
            foreach (string value in Get(tool)) if (value == tag) return true;
            return false;
        }

        internal static string Display(ICryptoTool tool) { return string.Join(" · ", new List<string>(Get(tool)).ToArray()); }
    }
}
