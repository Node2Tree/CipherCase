using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class UnicodeAnalysis
    {
        internal static List<string> Units(string input)
        {
            List<string> preferred = new List<string>(), fallback = new List<string>(); TextElementEnumerator items = StringInfo.GetTextElementEnumerator((input ?? string.Empty).Normalize(NormalizationForm.FormC));
            while (items.MoveNext())
            {
                string unit = items.GetTextElement(); if (string.IsNullOrEmpty(unit)) continue; UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(unit, 0);
                if (category == UnicodeCategory.Control || category == UnicodeCategory.Format || category == UnicodeCategory.LineSeparator || category == UnicodeCategory.ParagraphSeparator || category == UnicodeCategory.SpaceSeparator || char.IsWhiteSpace(unit, 0)) continue;
                string normalized = unit.ToUpperInvariant(); fallback.Add(normalized); if (IsLetterOrDigit(category)) preferred.Add(normalized);
            }
            if (fallback.Count > 0 && Different(fallback) <= 26 && IsPredominantlyNonAscii(fallback)) return fallback;
            return preferred.Count > 0 ? preferred : fallback;
        }

        internal static string LatinLetters(string input)
        {
            string decomposed = (input ?? string.Empty).Normalize(NormalizationForm.FormD); StringBuilder result = new StringBuilder();
            foreach (char raw in decomposed) { if (CharUnicodeInfo.GetUnicodeCategory(raw) == UnicodeCategory.NonSpacingMark) continue; char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') result.Append(c); }
            return result.ToString();
        }

        internal static string Frequency(string input)
        {
            List<string> units = Units(input); if (units.Count == 0) throw new CipherException("没有可分析的字符"); Dictionary<string, int> counts = Counts(units); List<KeyValuePair<string, int>> rows = Sorted(counts); StringBuilder result = new StringBuilder("字符   次数   比例\r\n");
            foreach (KeyValuePair<string, int> row in rows) result.AppendFormat(CultureInfo.InvariantCulture, "{0,-6} {1,-6} {2,6:0.00}%\r\n", Display(row.Key), row.Value, row.Value * 100.0 / units.Count); return result.ToString();
        }

        internal static string Ngrams(string input, int n)
        {
            if (n < 1 || n > 8) throw new CipherException("N 须为 1–8"); List<string> units = Units(input); if (units.Count < n) throw new CipherException("文本长度小于 N"); Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int i = 0; i <= units.Count - n; i++) { string key = GramKey(units, i, n); counts[key] = counts.ContainsKey(key) ? counts[key] + 1 : 1; }
            List<KeyValuePair<string, int>> rows = Sorted(counts); StringBuilder result = new StringBuilder("组合       次数\r\n"); for (int i = 0; i < Math.Min(100, rows.Count); i++) result.AppendFormat("{0,-10} {1}\r\n", DisplayGram(rows[i].Key), rows[i].Value); return result.ToString();
        }

        internal static double Coincidence(IList<string> units)
        {
            if (units == null || units.Count < 2) return 0; Dictionary<string, int> counts = Counts(units); double numerator = 0; foreach (int count in counts.Values) numerator += count * (count - 1); return numerator / (units.Count * (units.Count - 1.0));
        }

        internal static double Entropy(IList<string> units)
        {
            if (units == null || units.Count == 0) return 0; Dictionary<string, int> counts = Counts(units); double value = 0; foreach (int count in counts.Values) { double p = count / (double)units.Count; value -= p * Math.Log(p, 2); } return value;
        }

        internal static double ColumnIc(IList<string> units, int start, int step)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(); int total = 0; for (int i = start; i < units.Count; i += step) { string value = units[i]; counts[value] = counts.ContainsKey(value) ? counts[value] + 1 : 1; total++; } if (total < 2) return 0; double numerator = 0; foreach (int count in counts.Values) numerator += count * (count - 1); return numerator / (total * (total - 1.0));
        }

        internal static string ScriptSummary(string input)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(); TextElementEnumerator items = StringInfo.GetTextElementEnumerator(input ?? string.Empty); int total = 0;
            while (items.MoveNext()) { string unit = items.GetTextElement(); UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(unit, 0); if (!IsLetterOrDigit(category)) continue; string script = ScriptOf(char.ConvertToUtf32(unit, 0)); counts[script] = counts.ContainsKey(script) ? counts[script] + 1 : 1; total++; }
            if (total == 0) return "符号"; List<KeyValuePair<string, int>> rows = Sorted(counts); StringBuilder result = new StringBuilder(); for (int i = 0; i < rows.Count; i++) { if (i > 0) result.Append("、"); result.Append(rows[i].Key).Append(' ').Append((rows[i].Value * 100.0 / total).ToString("0.#", CultureInfo.InvariantCulture)).Append('%'); } return result.ToString();
        }

        internal static int Different(IList<string> units) { return Counts(units).Count; }
        internal static bool IsPredominantlyNonAscii(IList<string> units) { int count = 0; foreach (string unit in units) if (!(unit.Length == 1 && unit[0] < 128)) count++; return units.Count > 0 && count >= units.Count * .8; }

        internal static string Kasiski(string input, int length)
        {
            List<string> units = Units(input); Dictionary<string, List<int>> positions = new Dictionary<string, List<int>>(); for (int i = 0; i <= units.Count - length; i++) { string value = GramKey(units, i, length); if (!positions.ContainsKey(value)) positions[value] = new List<int>(); positions[value].Add(i); }
            Dictionary<int, int> factors = new Dictionary<int, int>(); StringBuilder details = new StringBuilder("重复序列   位置 / 间距\r\n"); int repeats = 0;
            foreach (KeyValuePair<string, List<int>> row in positions) { if (row.Value.Count < 2) continue; repeats++; details.Append(DisplayGram(row.Key)).Append("   "); for (int i = 1; i < row.Value.Count; i++) { int distance = row.Value[i] - row.Value[i - 1]; details.Append(row.Value[i - 1]).Append('→').Append(row.Value[i]).Append(" / ").Append(distance).Append("  "); for (int factor = 2; factor <= Math.Min(30, distance); factor++) if (distance % factor == 0) factors[factor] = factors.ContainsKey(factor) ? factors[factor] + 1 : 1; } details.AppendLine(); }
            if (repeats == 0) return "未发现重复序列；请增加文本长度或降低序列长度。"; List<KeyValuePair<int, int>> ranked = new List<KeyValuePair<int, int>>(factors); ranked.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b) { int c = b.Value.CompareTo(a.Value); return c != 0 ? c : a.Key.CompareTo(b.Key); }); StringBuilder result = new StringBuilder("可能的密钥长度："); for (int i = 0; i < Math.Min(10, ranked.Count); i++) result.Append(i == 0 ? " " : ", ").Append(ranked[i].Key).Append('(').Append(ranked[i].Value).Append(')'); return result.Append("\r\n\r\n").Append(details).ToString();
        }

        private static bool IsLetterOrDigit(UnicodeCategory c) { return c == UnicodeCategory.UppercaseLetter || c == UnicodeCategory.LowercaseLetter || c == UnicodeCategory.TitlecaseLetter || c == UnicodeCategory.ModifierLetter || c == UnicodeCategory.OtherLetter || c == UnicodeCategory.DecimalDigitNumber || c == UnicodeCategory.LetterNumber || c == UnicodeCategory.OtherNumber; }
        private static Dictionary<string, int> Counts(IList<string> units) { Dictionary<string, int> result = new Dictionary<string, int>(); if (units != null) foreach (string unit in units) result[unit] = result.ContainsKey(unit) ? result[unit] + 1 : 1; return result; }
        private static List<KeyValuePair<string, int>> Sorted(Dictionary<string, int> counts) { List<KeyValuePair<string, int>> rows = new List<KeyValuePair<string, int>>(counts); rows.Sort(delegate(KeyValuePair<string, int> a, KeyValuePair<string, int> b) { int c = b.Value.CompareTo(a.Value); return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key); }); return rows; }
        private static string GramKey(IList<string> units, int start, int length) { StringBuilder r = new StringBuilder(); for (int i = 0; i < length; i++) { if (i > 0) r.Append('\u0001'); r.Append(units[start + i]); } return r.ToString(); }
        private static string DisplayGram(string key) { return key.Replace("\u0001", string.Empty); }
        private static string Display(string value) { return value == "\t" ? "\\t" : value == "\r" ? "\\r" : value == "\n" ? "\\n" : value; }
        private static string ScriptOf(int code) { if ((code >= 0x4E00 && code <= 0x9FFF) || (code >= 0x3400 && code <= 0x4DBF) || (code >= 0x20000 && code <= 0x2FA1F)) return "汉字"; if (code >= 0x3040 && code <= 0x309F) return "平假名"; if (code >= 0x30A0 && code <= 0x30FF) return "片假名"; if (code >= 0xAC00 && code <= 0xD7AF) return "韩文"; if (code >= 0x0400 && code <= 0x052F) return "西里尔"; if (code >= 0x0370 && code <= 0x03FF) return "希腊"; if (code >= 0x0590 && code <= 0x05FF) return "希伯来"; if (code >= 0x0600 && code <= 0x06FF) return "阿拉伯"; if (code >= 0x0900 && code <= 0x097F) return "天城文"; if ((code >= 0x0041 && code <= 0x024F) || (code >= 0x1E00 && code <= 0x1EFF)) return "拉丁"; if (code >= 0x30 && code <= 0x39) return "数字"; return "其他"; }
    }
}
