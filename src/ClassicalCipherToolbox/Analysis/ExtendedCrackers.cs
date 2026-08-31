using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class ExtendedCrackers
    {
        private sealed class Candidate
        {
            internal string Key;
            internal string Text;
            internal double Score;
        }

        internal static string CrackHill2(ToolRequest request)
        {
            string cipher = Letters(request.Input); if (cipher.Length < 40 || cipher.Length % 2 != 0) throw new CipherException("Hill 2×2 破解需要至少 40 个偶数字母");
            string language = LanguageModels.Normalize(request.Get("language")); int sampleLength = Math.Min(360, cipher.Length); if (sampleLength % 2 != 0) sampleLength--;
            List<Candidate> shortlist = new List<Candidate>();
            for (int a = 0; a < 26; a++)
            {
                request.ThrowIfCancellationRequested(); request.ReportProgress(a * 82 / 26, "Hill 2×2 · 枚举矩阵");
                for (int b = 0; b < 26; b++) for (int c = 0; c < 26; c++) for (int d = 0; d < 26; d++)
                {
                    int determinant = Mod(a * d - b * c, 26), inverse = Inverse(determinant); if (inverse < 0) continue;
                    int m0 = Mod(d * inverse, 26), m1 = Mod(-b * inverse, 26), m2 = Mod(-c * inverse, 26), m3 = Mod(a * inverse, 26);
                    string sample = HillTransform(cipher, sampleLength, m0, m1, m2, m3);
                    AddBest(shortlist, new Candidate { Key = a + "," + b + "," + c + "," + d, Score = LanguageModels.TextScore(sample, language) }, 80);
                }
            }
            request.ReportProgress(85, "Hill 2×2 · 完整评分");
            List<Candidate> final = new List<Candidate>(); int position = 0;
            foreach (Candidate item in shortlist)
            {
                request.ThrowIfCancellationRequested(); string[] p = item.Key.Split(','); int a = int.Parse(p[0]), b = int.Parse(p[1]), c = int.Parse(p[2]), d = int.Parse(p[3]); int inverse = Inverse(Mod(a * d - b * c, 26));
                item.Text = HillTransform(cipher, cipher.Length, Mod(d * inverse, 26), Mod(-b * inverse, 26), Mod(-c * inverse, 26), Mod(a * inverse, 26)); item.Score = LanguageModels.TextScore(item.Text, language); AddBest(final, item, 15);
                request.ReportProgress(85 + (++position * 15 / Math.Max(1, shortlist.Count)), "Hill 2×2 · 完整评分");
            }
            request.ReportProgress(100, "Hill 2×2 · 完成"); return Format(final, 15);
        }

        internal static string CrackColumnar(ToolRequest request)
        {
            string source = request.Input ?? string.Empty; if (Letters(source).Length < 30) throw new CipherException("列换位破解至少需要 30 个字母");
            int minimum = ReadRange(request.Get("min"), 2, 2, 9), maximum = ReadRange(request.Get("max"), 8, minimum, 9); string language = LanguageModels.Normalize(request.Get("language"));
            long total = 0; for (int width = minimum; width <= maximum; width++) total += Factorial(width); long completed = 0; List<Candidate> best = new List<Candidate>();
            for (int width = minimum; width <= maximum; width++)
            {
                int[] order = new int[width]; for (int i = 0; i < width; i++) order[i] = i;
                do
                {
                    if ((completed & 511) == 0) { request.ThrowIfCancellationRequested(); request.ReportProgress((int)(completed * 100 / Math.Max(1L, total)), "列换位 · 宽度 " + width); }
                    string plain = DecryptColumnar(source, order); double score = LanguageModels.TextScore(Letters(plain), language); AddBest(best, new Candidate { Key = EquivalentKeyword(order), Text = plain, Score = score }, 20); completed++;
                }
                while (NextPermutation(order));
            }
            request.ReportProgress(100, "列换位 · 完成"); return Format(best, 15);
        }

        internal static string CrackMorbit(ToolRequest request)
        {
            string digits = Digits(request.Input, '1', '9'); if (digits.Length < 30) throw new CipherException("Morbit 破解至少需要 30 位数字");
            string language = LanguageModels.Normalize(request.Get("language")); int[] mapping = new int[9]; for (int i = 0; i < 9; i++) mapping[i] = i; long total = Factorial(9), completed = 0; int sampleLength = Math.Min(220, digits.Length); List<Candidate> shortlist = new List<Candidate>();
            do
            {
                if ((completed & 1023) == 0) { request.ThrowIfCancellationRequested(); request.ReportProgress((int)(completed * 85 / total), "Morbit · 搜索映射"); }
                string plain = DecodeMorbit(digits, sampleLength, mapping); double score = MorseScore(plain, sampleLength, language); AddBest(shortlist, new Candidate { Key = MorbitMapping(mapping), Score = score }, 100); completed++;
            }
            while (NextPermutation(mapping));
            List<Candidate> final = new List<Candidate>(); int position = 0;
            foreach (Candidate candidate in shortlist)
            {
                request.ThrowIfCancellationRequested(); int[] parsed = ParseMorbitMapping(candidate.Key); candidate.Text = DecodeMorbit(digits, digits.Length, parsed); candidate.Score = MorseScore(candidate.Text, digits.Length, language); AddBest(final, candidate, 15);
                request.ReportProgress(85 + (++position * 15 / Math.Max(1, shortlist.Count)), "Morbit · 完整评分");
            }
            request.ReportProgress(100, "Morbit · 完成"); return Format(final, 15);
        }

        internal static string CrackPollux(ToolRequest request)
        {
            request.ReportProgress(20, "Pollux · 还原摩尔斯流"); string plain = PolluxCipher.Decrypt(request.Input, string.Empty); request.ReportProgress(100, "Pollux · 完成");
            return "#1  密钥 固定数字组  评分 " + LanguageModels.TextScore(Letters(plain), LanguageModels.Normalize(request.Get("language"))).ToString("0.00", CultureInfo.InvariantCulture) + "\r\n" + plain;
        }

        private static string HillTransform(string cipher, int length, int m0, int m1, int m2, int m3)
        {
            char[] result = new char[length]; for (int i = 0; i < length; i += 2) { int x = cipher[i] - 'A', y = cipher[i + 1] - 'A'; result[i] = (char)('A' + Mod(m0 * x + m1 * y, 26)); result[i + 1] = (char)('A' + Mod(m2 * x + m3 * y, 26)); } return new string(result);
        }

        private static string DecryptColumnar(string input, int[] order)
        {
            int columns = order.Length, shortLength = input.Length / columns, longColumns = input.Length % columns, position = 0; char[][] data = new char[columns][];
            foreach (int column in order) { int length = shortLength + (column < longColumns ? 1 : 0); data[column] = input.Substring(position, length).ToCharArray(); position += length; }
            StringBuilder result = new StringBuilder(input.Length); int rows = (input.Length + columns - 1) / columns;
            for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++) if (row < data[column].Length) result.Append(data[column][row]); return result.ToString();
        }

        private static string EquivalentKeyword(int[] order)
        {
            char[] key = new char[order.Length]; for (int rank = 0; rank < order.Length; rank++) key[order[rank]] = (char)('A' + rank); return new string(key);
        }

        private static string DecodeMorbit(string digits, int length, int[] mapping)
        {
            string[] pairs = { "..", ".-", ".x", "-.", "--", "-x", "x.", "x-", "xx" }; StringBuilder stream = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++) stream.Append(pairs[mapping[digits[i] - '1']]); return MorbitCipher.DecodeMorseStream(stream.ToString());
        }

        private static double MorseScore(string plain, int cipherLength, string language)
        {
            string letters = Letters(plain); int unknown = 0; foreach (char c in plain) if (c == '?') unknown++; if (letters.Length < Math.Max(4, cipherLength / 8)) return -1000000 - unknown * 100;
            return LanguageModels.TextScore(letters, language) - unknown * 30.0;
        }

        private static string MorbitMapping(int[] mapping)
        {
            StringBuilder result = new StringBuilder(); for (int i = 0; i < mapping.Length; i++) { if (i > 0) result.Append(','); result.Append(mapping[i]); } return result.ToString();
        }

        private static int[] ParseMorbitMapping(string text)
        {
            string[] parts = text.Split(','); int[] result = new int[parts.Length]; for (int i = 0; i < parts.Length; i++) result[i] = int.Parse(parts[i], CultureInfo.InvariantCulture); return result;
        }

        private static void AddBest(List<Candidate> values, Candidate candidate, int limit)
        {
            values.Add(candidate); values.Sort(delegate(Candidate left, Candidate right) { return right.Score.CompareTo(left.Score); }); if (values.Count > limit) values.RemoveAt(values.Count - 1);
        }

        private static string Format(List<Candidate> candidates, int limit)
        {
            candidates.Sort(delegate(Candidate left, Candidate right) { return right.Score.CompareTo(left.Score); }); StringBuilder result = new StringBuilder();
            for (int i = 0; i < Math.Min(limit, candidates.Count); i++) result.AppendFormat(CultureInfo.InvariantCulture, "#{0}  密钥 {1}  评分 {2:0.00}\r\n{3}\r\n\r\n", i + 1, candidates[i].Key, candidates[i].Score, candidates[i].Text);
            return result.ToString().TrimEnd();
        }

        private static bool NextPermutation(int[] values)
        {
            int i = values.Length - 2; while (i >= 0 && values[i] >= values[i + 1]) i--; if (i < 0) return false; int j = values.Length - 1; while (values[j] <= values[i]) j--; int value = values[i]; values[i] = values[j]; values[j] = value; Array.Reverse(values, i + 1, values.Length - i - 1); return true;
        }

        private static long Factorial(int value) { long result = 1; for (int i = 2; i <= value; i++) result *= i; return result; }
        private static int ReadRange(string text, int fallback, int minimum, int maximum) { int value; if (!int.TryParse(text, out value)) value = fallback; return Math.Max(minimum, Math.Min(maximum, value)); }
        private static int Mod(int value, int modulus) { int result = value % modulus; return result < 0 ? result + modulus : result; }
        private static int Inverse(int value) { for (int i = 1; i < 26; i++) if (Mod(value * i, 26) == 1) return i; return -1; }
        private static string Letters(string input) { StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') result.Append(c); } return result.ToString(); }
        private static string Digits(string input, char low, char high) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) if (c >= low && c <= high) result.Append(c); return result.ToString(); }
    }
}
