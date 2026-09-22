using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private static void SortByCommonness(List<ICryptoTool> tools)
        {
            tools.Sort(delegate(ICryptoTool a, ICryptoTool b)
            {
                int category = CategoryRank(a.Category).CompareTo(CategoryRank(b.Category)); if (category != 0) return category;
                int priority = Commonness(a).CompareTo(Commonness(b)); return priority != 0 ? priority : string.CompareOrdinal(a.Name, b.Name);
            });
        }

        private static int CategoryRank(string category)
        {
            if (category == ToolCategories.General) return 0; if (category == ToolCategories.Chinese) return 1; if (category == ToolCategories.Encoding) return 2; if (category == ToolCategories.Substitution) return 3; if (category == ToolCategories.Polyalphabetic) return 4; if (category == ToolCategories.Transposition) return 5; if (category == ToolCategories.Grid) return 6; return 7;
        }

        private static int Commonness(ICryptoTool tool)
        {
            string[] order;
            if (tool.Category == ToolCategories.General) order = new[] { "通用破解", "密码识别器", "分析工作台", "频率", "重合指数", "N-gram", "Kasiski", "Crib 工具" };
            else if (tool.Category == ToolCategories.Chinese) order = new[] { "中文编码工作台", "字符详情卡", "中文语言评分", "中文输入法码", "中文电报码", "中文电码加密", "反切码", "中文代码本", "中文隐写分析", "拼音格式转换", "中文语音与罗马化", "中文码表工作台", "中文编码识别", "中文字符集对照", "Unicode 兼容格式", "中文传输格式", "历史中文字符集" };
            else if (tool.Category == ToolCategories.Encoding) order = new[] { "自动解码", "Base64", "十六进制", "URL 编码", "Unicode 转义", "二进制", "进制换算", "数据类型编码", "格雷码", "碱基转氨基酸", "Base32", "字符集字节", "中文输入法码", "HTML 实体", "QR Code", "Morse", "条形码", "Base64URL", "Quoted-Printable", "盲文（英语一级）", "中文电报码", "博多码 ITA2", "颜色编码", "取色器与调色盘", "Base58", "ASCII85", "Punycode", "A1Z26", "Tap Code", "北约音标字母", "旗语", "猪圈密码符号" };
            else if (tool.Category == ToolCategories.Substitution) order = new[] { "凯撒", "ROT13", "Atbash", "仿射", "ROT-N", "单表替换", "培根", "Keyword Cipher", "Multiplicative", "同音替换", "Book Cipher", "Nomenclator", "Grandpré", "Vatsyayana" };
            else if (tool.Category == ToolCategories.Polyalphabetic) order = new[] { "维吉尼亚", "Beaufort", "Autokey", "Gronsfeld", "Porta", "Running Key", "Enigma", "Variant Beaufort", "Trithemius", "渐进凯撒", "Alberti", "Bellaso", "Ragbaby", "Jefferson Wheel", "Quagmire I", "Quagmire II", "Quagmire III", "Quagmire IV", "Gromark", "Periodic Gromark", "Chaocipher", "Solitaire", "Nicodemus" };
            else if (tool.Category == ToolCategories.Transposition) order = new[] { "栅栏", "列换位", "路线换位", "Scytale", "Reverse", "Redefence", "Caesar Box", "Myszkowski", "AMSCO", "双重列换位", "Turning Grille", "Ubchi", "扰乱式换位", "Swagman", "Cadenus" };
            else order = new[] { "Polybius", "Playfair", "Hill 2×2", "Hill 3×3", "ADFGX", "ADFGVX", "Bifid", "Trifid", "Four-square", "Two-square", "Nihilist", "Fractionated Morse", "跨行棋盘", "Bazeries", "Morbit", "Pollux", "Three-square", "Digrafid", "Phillips", "VIC" };
            for (int i = 0; i < order.Length; i++) if (tool.Name == order[i]) return i; return 1000;
        }
    }
}
