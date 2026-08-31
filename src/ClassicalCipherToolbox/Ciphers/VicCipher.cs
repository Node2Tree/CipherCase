using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class VicCipher
    {
        private sealed class Keys
        {
            internal int[] First; internal int[] Second; internal int[] Header; internal string Indicator; internal int InsertDistance;
        }

        internal static string Encrypt(string input, string common, string phrase, string date, string personalText, string indicator)
        {
            return Encrypt(input, common, phrase, date, personalText, indicator, string.Empty);
        }

        internal static string Encrypt(string input, string common, string phrase, string date, string personalText, string indicator, string cutText)
        {
            Keys keys = Derive(common, phrase, date, personalText, indicator);
            string digits = CheckerboardEncode(Bifurcate(input, cutText), common, keys.Header);
            digits = ColumnarEncrypt(digits, keys.First);
            digits = DisruptedEncrypt(digits, keys.Second);
            return InsertIndicator(digits, keys.Indicator, keys.InsertDistance);
        }

        internal static string Decrypt(string input, string common, string phrase, string date, string personalText, string indicator)
        {
            return Decrypt(input, common, phrase, date, personalText, indicator, string.Empty);
        }

        internal static string Decrypt(string input, string common, string phrase, string date, string personalText, string indicator, string cutText)
        {
            string digits = Digits(input); string dateDigits = Digits(date);
            if (string.IsNullOrWhiteSpace(indicator))
            {
                if (dateDigits.Length < 6) throw new CipherException("VIC 日期至少需要 6 位数字");
                List<string> groups = Groups(digits); int index = groups.Count - (dateDigits[5] - '0');
                if (index < 0 || index >= groups.Count || groups[index].Length != 5) throw new CipherException("无法从密文提取消息组");
                indicator = groups[index];
            }
            Keys keys = Derive(common, phrase, date, personalText, indicator);
            digits = RemoveIndicator(digits, keys);
            digits = DisruptedDecrypt(digits, keys.Second);
            digits = ColumnarDecrypt(digits, keys.First);
            string plain = CheckerboardDecode(digits, common, keys.Header);
            if (!string.IsNullOrWhiteSpace(cutText)) { int marker = plain.IndexOf("..", StringComparison.Ordinal); if (marker >= 0) plain = plain.Substring(marker + 2) + plain.Substring(0, marker); }
            return plain;
        }

        internal static string DescribeKeys(string common, string phrase, string date, string personalText, string indicator)
        {
            Keys keys = Derive(common, phrase, date, personalText, indicator);
            return "消息组：" + keys.Indicator + "\r\n第一换位宽度：" + keys.First.Length + "\r\n第二换位宽度：" + keys.Second.Length + "\r\n棋盘表头：" + Digits(keys.Header);
        }

        private static Keys Derive(string commonText, string phraseText, string dateText, string personalText, string indicatorText)
        {
            string common = UniqueLetters(commonText); if (common.Length != 8) throw new CipherException("VIC 常用字母须为 8 个不重复字母，例如 ATONESIR");
            string phrase = CipherUtilities.Letters(phraseText); if (phrase.Length < 20) throw new CipherException("VIC 记忆短语至少需要 20 个字母"); phrase = phrase.Substring(0, 20);
            string date = Digits(dateText); if (date.Length < 6) throw new CipherException("VIC 日期至少需要 6 位数字");
            int personal; if (!int.TryParse(personalText, out personal) || personal < 1 || personal > 20) throw new CipherException("个人编号须为 1–20");
            string indicator = Digits(indicatorText); if (indicator.Length == 0) indicator = DeterministicIndicator(phrase, date, personal); if (indicator.Length != 5) throw new CipherException("消息组须为 5 位数字");
            int[] s1 = Sequentialize(phrase.Substring(0, 10)); int[] s2 = Sequentialize(phrase.Substring(10, 10));
            List<int> seed = new List<int>(); for (int i = 0; i < 5; i++) seed.Add(Alphabet.Mod((indicator[i] - '0') - (date[i] - '0'), 10)); Expand(seed, 10);
            int[] g = new int[10]; for (int i = 0; i < 10; i++) g[i] = (seed[i] + RankDigit(s1[i])) % 10;
            List<int> t = new List<int>(); string digitOrder = "1234567890"; for (int i = 0; i < 10; i++) t.Add(RankDigit(s2[digitOrder.IndexOf((char)('0' + g[i]))]));
            int[] seqT = Sequentialize(Digits(t.ToArray())); List<int> chain = new List<int>(t); Expand(chain, 60); int[] block = chain.GetRange(10, 50).ToArray();
            int last = block[49], previous = block[48]; for (int i = 48; i >= 0 && previous == last; i--) previous = block[i];
            int width1 = personal + previous, width2 = personal + last; if (width1 < 2) width1 = 2; if (width2 < 2) width2 = 2;
            StringBuilder transposed = new StringBuilder(); for (int rank = 1; rank <= 10; rank++) { int column = Array.IndexOf(seqT, rank); for (int row = 0; row < 5; row++) transposed.Append(block[row * 10 + column]); }
            int[] first = Sequentialize(transposed.ToString().Substring(0, width1)); int[] second = Sequentialize(transposed.ToString().Substring(width1, width2)); int[] lastRow = new int[10]; Array.Copy(block, 40, lastRow, 0, 10);
            return new Keys { First = first, Second = second, Header = Sequentialize(Digits(lastRow)), Indicator = indicator, InsertDistance = date[5] - '0' };
        }

        private static string CheckerboardEncode(string input, string commonText, int[] header)
        {
            string common = UniqueLetters(commonText), rest = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.#"; List<char> symbols = new List<char>(); foreach (char c in common) symbols.Add(c); foreach (char c in rest) if (!symbols.Contains(c)) symbols.Add(c);
            Dictionary<char, string> map = new Dictionary<char, string>(); for (int i = 0; i < 8; i++) map[symbols[i]] = RankDigit(header[i]).ToString(); int p = 8; for (int row = 8; row < 10; row++) for (int col = 0; col < 10; col++) if (p < symbols.Count) map[symbols[p++]] = RankDigit(header[row]).ToString() + RankDigit(header[col]).ToString();
            string hashCode = map['#']; StringBuilder result = new StringBuilder(); string source = input ?? string.Empty; for (int i = 0; i < source.Length; i++) { char c = char.ToUpperInvariant(source[i]); if (c >= 'A' && c <= 'Z') result.Append(map[c]); else if (c == '.') result.Append(map['.']); else if (char.IsDigit(c)) { int start = i; while (i < source.Length && char.IsDigit(source[i])) i++; int count = i - start; if (count > 999) throw new CipherException("VIC 连续数字不能超过 999 位"); result.Append(hashCode).Append(count.ToString("000")); for (int digitIndex = start; digitIndex < i; digitIndex++) result.Append(source[digitIndex]).Append(source[digitIndex]); result.Append(hashCode); i--; } } return result.ToString();
        }

        private static string CheckerboardDecode(string digits, string commonText, int[] header)
        {
            string common = UniqueLetters(commonText), rest = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.#"; List<char> symbols = new List<char>(); foreach (char c in common) symbols.Add(c); foreach (char c in rest) if (!symbols.Contains(c)) symbols.Add(c);
            Dictionary<string, char> map = new Dictionary<string, char>(); for (int i = 0; i < 8; i++) map[RankDigit(header[i]).ToString()] = symbols[i]; int p = 8; for (int row = 8; row < 10; row++) for (int col = 0; col < 10; col++) if (p < symbols.Count) map[RankDigit(header[row]).ToString() + RankDigit(header[col]).ToString()] = symbols[p++]; string hashCode = string.Empty; foreach (KeyValuePair<string, char> pair in map) if (pair.Value == '#') hashCode = pair.Key;
            HashSet<char> prefixes = new HashSet<char>(); prefixes.Add((char)('0' + RankDigit(header[8]))); prefixes.Add((char)('0' + RankDigit(header[9]))); StringBuilder result = new StringBuilder();
            for (int i = 0; i < digits.Length;) { string code = digits[i].ToString(); if (prefixes.Contains(digits[i])) { if (i + 1 >= digits.Length) break; code += digits[i + 1]; i += 2; } else i++; char value; if (!map.TryGetValue(code, out value)) continue; if (value != '#') { result.Append(value); continue; } if (i + 3 > digits.Length) break; int count; if (!int.TryParse(digits.Substring(i, 3), out count)) break; i += 3; for (int n = 0; n < count && i + 1 < digits.Length; n++) { if (digits[i] != digits[i + 1]) throw new CipherException("VIC 数字重复校验失败"); result.Append(digits[i]); i += 2; } if (i + hashCode.Length <= digits.Length && string.Compare(digits, i, hashCode, 0, hashCode.Length, StringComparison.Ordinal) == 0) i += hashCode.Length; }
            return result.ToString();
        }

        private static string ColumnarEncrypt(string source, int[] key) { StringBuilder r = new StringBuilder(); for (int rank = 1; rank <= key.Length; rank++) { int col = Array.IndexOf(key, rank); for (int i = col; i < source.Length; i += key.Length) r.Append(source[i]); } return r.ToString(); }
        private static string ColumnarDecrypt(string source, int[] key) { int rows = (source.Length + key.Length - 1) / key.Length, shortCols = key.Length * rows - source.Length; char[] result = new char[source.Length]; int p = 0; for (int rank = 1; rank <= key.Length; rank++) { int col = Array.IndexOf(key, rank); int length = rows - (col >= key.Length - shortCols ? 1 : 0); for (int row = 0; row < length; row++) result[row * key.Length + col] = source[p++]; } return new string(result); }

        private static string DisruptedEncrypt(string source, int[] key) { bool[,] blanks; bool[,] valid; BuildMask(source.Length, key, out blanks, out valid); char[,] grid = FillDisrupted(source, blanks, valid); StringBuilder r = new StringBuilder(); for (int rank = 1; rank <= key.Length; rank++) { int col = Array.IndexOf(key, rank); for (int row = 0; row < grid.GetLength(0); row++) if (valid[row, col]) r.Append(grid[row, col]); } return r.ToString(); }
        private static string DisruptedDecrypt(string source, int[] key) { bool[,] blanks; bool[,] valid; BuildMask(source.Length, key, out blanks, out valid); char[,] grid = new char[valid.GetLength(0), key.Length]; int p = 0; for (int rank = 1; rank <= key.Length; rank++) { int col = Array.IndexOf(key, rank); for (int row = 0; row < grid.GetLength(0); row++) if (valid[row, col]) grid[row, col] = source[p++]; } StringBuilder r = new StringBuilder(); for (int pass = 0; pass < 2; pass++) for (int row = 0; row < grid.GetLength(0); row++) for (int col = 0; col < key.Length; col++) if (valid[row, col] && blanks[row, col] == (pass == 1)) r.Append(grid[row, col]); return r.ToString(); }
        private static char[,] FillDisrupted(string source, bool[,] blanks, bool[,] valid) { char[,] grid = new char[valid.GetLength(0), valid.GetLength(1)]; int p = 0; for (int pass = 0; pass < 2; pass++) for (int row = 0; row < grid.GetLength(0); row++) for (int col = 0; col < grid.GetLength(1); col++) if (valid[row, col] && blanks[row, col] == (pass == 1) && p < source.Length) grid[row, col] = source[p++]; return grid; }
        private static void BuildMask(int length, int[] key, out bool[,] blanks, out bool[,] valid) { int rows = (length + key.Length - 1) / key.Length; blanks = new bool[rows, key.Length]; valid = new bool[rows, key.Length]; for (int i = 0; i < length; i++) valid[i / key.Length, i % key.Length] = true; int row = 0, rank = 1; while (row < rows) { int start = Array.IndexOf(key, rank); for (int step = 0; step < key.Length - start && row < rows; step++, row++) for (int col = start + step; col < key.Length; col++) if (valid[row, col]) blanks[row, col] = true; rank++; if (rank > key.Length) rank = 1; } }

        private static string InsertIndicator(string digits, string indicator, int distance) { List<string> groups = Groups(digits); int index = Math.Max(0, Math.Min(groups.Count, groups.Count - distance + 1)); groups.Insert(index, indicator); return string.Join(" ", groups.ToArray()); }
        private static string RemoveIndicator(string digits, Keys keys) { List<string> groups = Groups(digits); int index = Math.Max(0, Math.Min(groups.Count - 1, groups.Count - keys.InsertDistance)); if (index < 0 || index >= groups.Count || groups[index] != keys.Indicator) throw new CipherException("未在日期指定位置找到消息组"); groups.RemoveAt(index); return string.Concat(groups.ToArray()); }
        private static List<string> Groups(string digits) { List<string> r = new List<string>(); for (int i = 0; i < digits.Length; i += 5) r.Add(digits.Substring(i, Math.Min(5, digits.Length - i))); return r; }
        private static string DeterministicIndicator(string phrase, string date, int personal) { int seed = personal; foreach (char c in phrase + date) seed = unchecked(seed * 31 + c); Random random = new Random(seed); return random.Next(100000).ToString("00000"); }
        private static string Bifurcate(string input, string cutText) { if (string.IsNullOrWhiteSpace(cutText)) return input ?? string.Empty; string source = new StringBuilder(input ?? string.Empty).ToString(); int cut; if (!int.TryParse(cutText, out cut) || cut < 1 || cut >= source.Length) throw new CipherException("VIC 切分位置须位于消息内部"); return source.Substring(cut) + ".." + source.Substring(0, cut); }
        private static int[] Sequentialize(string source) { List<int> indices = new List<int>(); for (int i = 0; i < source.Length; i++) indices.Add(i); indices.Sort(delegate(int a, int b) { int c = source[a].CompareTo(source[b]); return c != 0 ? c : a.CompareTo(b); }); int[] result = new int[source.Length]; for (int i = 0; i < indices.Count; i++) result[indices[i]] = i + 1; return result; }
        private static void Expand(List<int> values, int length) { int i = 0; while (values.Count < length) { values.Add((values[i] + values[i + 1]) % 10); i++; } }
        private static int RankDigit(int rank) { return rank == 10 ? 0 : rank; }
        private static string UniqueLetters(string input) { StringBuilder r = new StringBuilder(); foreach (char c in CipherUtilities.Letters(input)) if (r.ToString().IndexOf(c) < 0) r.Append(c); return r.ToString(); }
        private static string Digits(string input) { StringBuilder r = new StringBuilder(); foreach (char c in input ?? string.Empty) if (char.IsDigit(c)) r.Append(c); return r.ToString(); }
        private static string Digits(int[] input) { StringBuilder r = new StringBuilder(); foreach (int value in input) r.Append(RankDigit(value)); return r.ToString(); }
    }
}
