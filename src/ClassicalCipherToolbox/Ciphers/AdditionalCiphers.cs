using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class RagbabyCipher
    {
        private const string Basis = "ABCDEFGHIKLMNOPQRSTUVWYZ";
        internal static string Transform(string input, string key, string firstText, string stepText, bool decrypt)
        {
            int first, step; if (!int.TryParse(firstText, out first)) first = 1; if (!int.TryParse(stepText, out step)) step = 1; string alphabet = Keyed(key); StringBuilder result = new StringBuilder(); int word = first, letter = 0; bool inWord = false;
            foreach (char raw in (input ?? string.Empty).ToUpperInvariant()) { char c = raw == 'J' ? 'I' : raw == 'X' ? 'W' : raw; int index = alphabet.IndexOf(c); if (index < 0) { result.Append(raw); if (char.IsWhiteSpace(raw) && inWord) { word++; letter = 0; inWord = false; } continue; } result.Append(alphabet[Alphabet.Mod(index + (decrypt ? -1 : 1) * (word + letter * step), alphabet.Length)]); letter++; inWord = true; } return result.ToString();
        }
        private static string Keyed(string key) { StringBuilder r = new StringBuilder(); foreach (char raw in CipherUtilities.Letters(key) + Basis) { char c = raw == 'J' ? 'I' : raw == 'X' ? 'W' : raw; if (Basis.IndexOf(c) >= 0 && r.ToString().IndexOf(c) < 0) r.Append(c); } return r.ToString(); }
    }

    internal static class UbchiCipher
    {
        internal static string Encrypt(string input, string key, string nulls) { string first = ColumnarTranspositionCipher.EncryptText(input ?? string.Empty, key); return ColumnarTranspositionCipher.EncryptText(first + (nulls ?? string.Empty), key); }
        internal static string Decrypt(string input, string key, string nullCountText) { int count; if (!int.TryParse(nullCountText, out count) || count < 0) count = 0; string first = ColumnarTranspositionCipher.DecryptText(input ?? string.Empty, key); if (count > first.Length) throw new CipherException("空字母数量超过文本长度"); first = first.Substring(0, first.Length - count); return ColumnarTranspositionCipher.DecryptText(first, key); }
    }

    internal sealed class ProgressiveCaesarCipher : ICipher
    {
        public string Name { get { return "渐进凯撒"; } } public string KeyHint { get { return "起始位移，例如 0"; } } public bool RequiresKey { get { return false; } }
        public string Encrypt(string input, string key) { return Transform(input, Parse(key), false); }
        public string Decrypt(string input, string key) { return Transform(input, Parse(key), true); }
        private static int Parse(string key) { int value; if (string.IsNullOrWhiteSpace(key)) return 0; if (!int.TryParse(key, out value)) throw new CipherException("起始位移须为整数"); return value; }
        private static string Transform(string input, int start, bool decrypt) { StringBuilder r = new StringBuilder(); int p = 0; foreach (char c in input ?? string.Empty) { if (!Alphabet.IsAsciiLetter(c)) r.Append(c); else { r.Append(Alphabet.Shift(c, (decrypt ? -1 : 1) * (start + p))); p++; } } return r.ToString(); }
    }

    internal sealed class TrithemiusCipher : ICipher
    {
        public string Name { get { return "Trithemius"; } } public string KeyHint { get { return "无需密钥"; } } public bool RequiresKey { get { return false; } }
        public string Encrypt(string input, string key) { return new ProgressiveCaesarCipher().Encrypt(input, "0"); }
        public string Decrypt(string input, string key) { return new ProgressiveCaesarCipher().Decrypt(input, "0"); }
    }

    internal sealed class VariantBeaufortCipher : ICipher
    {
        public string Name { get { return "Variant Beaufort"; } } public string KeyHint { get { return "关键词"; } } public bool RequiresKey { get { return true; } }
        public string Encrypt(string input, string key) { return Transform(input, key, false); }
        public string Decrypt(string input, string key) { return Transform(input, key, true); }
        private static string Transform(string input, string key, bool decrypt) { string k = CipherUtilities.Letters(key); if (k.Length == 0) throw new CipherException("请输入字母密钥"); StringBuilder r = new StringBuilder(); int p = 0; foreach (char c in input ?? string.Empty) { if (!Alphabet.IsAsciiLetter(c)) r.Append(c); else { int shift = k[p++ % k.Length] - 'A'; r.Append(Alphabet.Shift(c, decrypt ? shift : -shift)); } } return r.ToString(); }
    }

    internal sealed class ScytaleCipher : ICipher
    {
        public string Name { get { return "Scytale"; } } public string KeyHint { get { return "列数，例如 5"; } } public bool RequiresKey { get { return true; } }
        public string Encrypt(string input, string key) { int width = Width(key); string s = input ?? string.Empty; StringBuilder r = new StringBuilder(s.Length); for (int c = 0; c < width; c++) for (int i = c; i < s.Length; i += width) r.Append(s[i]); return r.ToString(); }
        public string Decrypt(string input, string key) { int width = Width(key); string s = input ?? string.Empty; int rows = (s.Length + width - 1) / width, shortColumns = width * rows - s.Length; string[] columns = new string[width]; int p = 0; for (int c = 0; c < width; c++) { int length = rows - (c >= width - shortColumns ? 1 : 0); columns[c] = s.Substring(p, length); p += length; } StringBuilder r = new StringBuilder(s.Length); for (int row = 0; row < rows; row++) for (int c = 0; c < width; c++) if (row < columns[c].Length) r.Append(columns[c][row]); return r.ToString(); }
        private static int Width(string value) { int width; if (!int.TryParse(value, out width) || width < 2 || width > 100) throw new CipherException("列数须为 2–100"); return width; }
    }

    internal sealed class CaesarBoxCipher : ICipher
    {
        public string Name { get { return "Caesar Box"; } } public string KeyHint { get { return "宽度，例如 5"; } } public bool RequiresKey { get { return true; } }
        public string Encrypt(string input, string key) { return new ScytaleCipher().Encrypt(input, key); }
        public string Decrypt(string input, string key) { return new ScytaleCipher().Decrypt(input, key); }
    }

    internal static class RedefenceCipher
    {
        internal static string Encrypt(string input, string railsText, string offsetText) { return Transform(input, railsText, offsetText, false); }
        internal static string Decrypt(string input, string railsText, string offsetText) { return Transform(input, railsText, offsetText, true); }
        private static string Transform(string input, string railsText, string offsetText, bool decrypt)
        {
            int rails, offset; if (!int.TryParse(railsText, out rails) || rails < 2 || rails > 50) throw new CipherException("栏数须为 2–50"); if (!int.TryParse(offsetText, out offset)) offset = 0;
            string s = input ?? string.Empty; int[] path = new int[s.Length]; int cycle = 2 * rails - 2;
            for (int i = 0; i < s.Length; i++) { int x = Alphabet.Mod(i + offset, cycle); path[i] = x < rails ? x : cycle - x; }
            if (!decrypt) { StringBuilder r = new StringBuilder(); for (int rail = 0; rail < rails; rail++) for (int i = 0; i < s.Length; i++) if (path[i] == rail) r.Append(s[i]); return r.ToString(); }
            char[] output = new char[s.Length]; int p = 0; for (int rail = 0; rail < rails; rail++) for (int i = 0; i < s.Length; i++) if (path[i] == rail) output[i] = s[p++]; return new string(output);
        }
    }

    internal static class A1Z26Cipher
    {
        internal static string Encrypt(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') { if (r.Length > 0 && char.IsDigit(r[r.Length - 1])) r.Append('-'); r.Append(c - 'A' + 1); } else r.Append(raw); } return r.ToString(); }
        internal static string Decrypt(string input) { string[] parts = (input ?? string.Empty).Split(new[] {' ', '-', ',', ';', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); StringBuilder r = new StringBuilder(); foreach (string part in parts) { int n; if (!int.TryParse(part, out n) || n < 1 || n > 26) throw new CipherException("A1Z26 数字须为 1–26"); r.Append((char)('A' + n - 1)); } return r.ToString(); }
    }

    internal static class TapCodeCipher
    {
        private const string Square = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        internal static string Encrypt(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in CipherUtilities.Letters(input)) { char c = raw == 'J' ? 'I' : raw; int p = Square.IndexOf(c); if (r.Length > 0) r.Append(' '); r.Append(p / 5 + 1).Append(p % 5 + 1); } return r.ToString(); }
        internal static string Decrypt(string input) { string digits = Digits(input); if (digits.Length % 2 != 0) throw new CipherException("Tap Code 须为成对数字"); StringBuilder r = new StringBuilder(); for (int i = 0; i < digits.Length; i += 2) { int row = digits[i] - '1', col = digits[i + 1] - '1'; if (row < 0 || row > 4 || col < 0 || col > 4) throw new CipherException("坐标须为 1–5"); r.Append(Square[row * 5 + col]); } return r.ToString(); }
        private static string Digits(string input) { StringBuilder r = new StringBuilder(); foreach (char c in input ?? string.Empty) if (char.IsDigit(c)) r.Append(c); return r.ToString(); }
    }

    internal static class MorseCipher
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly string[] Codes = {".-","-...","-.-.","-..",".","..-.","--.","....","..",".---","-.-",".-..","--","-.","---",".--.","--.-",".-.","...","-","..-","...-",".--","-..-","-.--","--..","-----",".----","..---","...--","....-",".....","-....","--...","---..","----."};
        internal static string Encrypt(string input) { StringBuilder r = new StringBuilder(); bool previousSpace = false; foreach (char raw in input ?? string.Empty) { if (char.IsWhiteSpace(raw)) { if (!previousSpace && r.Length > 0) r.Append(" / "); previousSpace = true; continue; } int i = Alphabet.IndexOf(char.ToUpperInvariant(raw)); if (i < 0) continue; if (r.Length > 0 && !previousSpace) r.Append(' '); r.Append(Codes[i]); previousSpace = false; } return r.ToString().Trim(); }
        internal static string Decrypt(string input) { string[] words = (input ?? string.Empty).Split(new[] {'/'}, StringSplitOptions.None); StringBuilder r = new StringBuilder(); for (int w = 0; w < words.Length; w++) { if (w > 0) r.Append(' '); string[] codes = words[w].Split(new[] {' ', '\t', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries); foreach (string code in codes) { int i = Array.IndexOf(Codes, code); r.Append(i >= 0 ? Alphabet[i] : '?'); } } return r.ToString(); }
    }

    internal static class MorbitCipher
    {
        private static readonly string[] Pairs = {"..",".-",".x","-.","--","-x","x.","x-","xx"};
        internal static string Encrypt(string input, string key) { string stream = MorseStream(input); int[] order = KeywordOrder(key, 9); StringBuilder r = new StringBuilder(); if (stream.Length % 2 != 0) stream += "x"; for (int i = 0; i < stream.Length; i += 2) r.Append(order[Array.IndexOf(Pairs, stream.Substring(i, 2))]); return r.ToString(); }
        internal static string Decrypt(string input, string key) { int[] order = KeywordOrder(key, 9); StringBuilder stream = new StringBuilder(); foreach (char c in input ?? string.Empty) if (c >= '1' && c <= '9') { int index = Array.IndexOf(order, c - '0'); if (index >= 0) stream.Append(Pairs[index]); } return DecodeMorseStream(stream.ToString()); }
        internal static string MorseStream(string input) { string encoded = MorseCipher.Encrypt(input); return encoded.Replace(" / ", "xx").Replace(" ", "x"); }
        internal static string DecodeMorseStream(string stream) { return MorseCipher.Decrypt((stream ?? string.Empty).TrimEnd('x').Replace("xx", " / ").Replace("x", " ")); }
        internal static int[] KeywordOrder(string key, int size) { string source = string.IsNullOrWhiteSpace(key) ? "KEYWORD" : key.ToUpperInvariant(); List<int> indices = new List<int>(); for (int i = 0; i < size; i++) indices.Add(i); indices.Sort(delegate(int a, int b) { char ca = source[a % source.Length], cb = source[b % source.Length]; int c = ca.CompareTo(cb); return c != 0 ? c : a.CompareTo(b); }); int[] order = new int[size]; for (int i = 0; i < size; i++) order[indices[i]] = i + 1; return order; }
    }

    internal static class PolluxCipher
    {
        internal static string Encrypt(string input, string key) { int seed = Seed(key); Random random = new Random(seed); string stream = MorbitCipher.MorseStream(input); int[][] groups = {new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8, 9}}; StringBuilder r = new StringBuilder(); foreach (char c in stream) { int group = c == '.' ? 0 : c == '-' ? 1 : 2; int[] values = groups[group]; r.Append(values[random.Next(values.Length)]); } return r.ToString(); }
        internal static string Decrypt(string input, string key) { StringBuilder stream = new StringBuilder(); foreach (char c in input ?? string.Empty) if (char.IsDigit(c)) { int d = c - '0'; stream.Append(d == 0 || d == 3 || d == 6 ? '.' : d == 1 || d == 4 || d == 7 ? '-' : 'x'); } return MorbitCipher.DecodeMorseStream(stream.ToString()); }
        private static int Seed(string key) { int value = 17; foreach (char c in key ?? string.Empty) value = unchecked(value * 31 + c); return value; }
    }

    internal static class TrifidCipher
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.";
        internal static string Encrypt(string input, string key, string periodText) { return Transform(input, key, periodText, false); }
        internal static string Decrypt(string input, string key, string periodText) { return Transform(input, key, periodText, true); }
        private static string Transform(string input, string key, string periodText, bool decrypt)
        {
            int period; if (!int.TryParse(periodText, out period) || period < 1) period = 5; string alphabet = Keyed27(key); StringBuilder text = new StringBuilder(); foreach (char raw in (input ?? string.Empty).ToUpperInvariant()) if (Alphabet.IndexOf(raw) >= 0) text.Append(raw);
            StringBuilder result = new StringBuilder(); for (int start = 0; start < text.Length; start += period) { int length = Math.Min(period, text.Length - start); List<int> digits = new List<int>(); if (!decrypt) { for (int axis = 0; axis < 3; axis++) for (int i = 0; i < length; i++) { int p = alphabet.IndexOf(text[start + i]); digits.Add(axis == 0 ? p / 9 : axis == 1 ? (p / 3) % 3 : p % 3); } for (int i = 0; i < length; i++) result.Append(alphabet[digits[i * 3] * 9 + digits[i * 3 + 1] * 3 + digits[i * 3 + 2]]); } else { for (int i = 0; i < length; i++) { int p = alphabet.IndexOf(text[start + i]); digits.Add(p / 9); digits.Add((p / 3) % 3); digits.Add(p % 3); } for (int i = 0; i < length; i++) result.Append(alphabet[digits[i] * 9 + digits[i + length] * 3 + digits[i + 2 * length]]); } } return result.ToString();
        }
        private static string Keyed27(string key) { StringBuilder r = new StringBuilder(); foreach (char c in (CipherUtilities.Letters(key) + Alphabet)) if (r.ToString().IndexOf(c) < 0) r.Append(c); return r.ToString(); }
    }

    internal static class AlbertiCipher
    {
        internal static string Transform(string input, string key, string periodText, bool decrypt)
        {
            string mixed = CipherUtilities.KeyedAlphabet(key, true); int period; if (!int.TryParse(periodText, out period) || period < 1) period = 5; StringBuilder r = new StringBuilder(); int p = 0;
            foreach (char raw in input ?? string.Empty) { if (!Alphabet.IsAsciiLetter(raw)) { r.Append(raw); continue; } int rotation = (p / period) % 26; int index = mixed.IndexOf(char.ToUpperInvariant(raw)); char output = mixed[Alphabet.Mod(index + (decrypt ? -rotation : rotation), 26)]; r.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output); p++; }
            return r.ToString();
        }
    }

    internal static class BellasoCipher
    {
        internal static string Transform(string input, string key, string alphabetKey, bool decrypt)
        {
            string keyword = CipherUtilities.Letters(key); if (keyword.Length == 0) throw new CipherException("请输入关键词"); string mixed = CipherUtilities.KeyedAlphabet(alphabetKey, true); StringBuilder r = new StringBuilder(); int p = 0;
            foreach (char raw in input ?? string.Empty) { if (!Alphabet.IsAsciiLetter(raw)) { r.Append(raw); continue; } int source = mixed.IndexOf(char.ToUpperInvariant(raw)), shift = mixed.IndexOf(keyword[p++ % keyword.Length]); int value = Alphabet.Mod(source + (decrypt ? -shift : shift), 26); char output = mixed[value]; r.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output); } return r.ToString();
        }
    }

    internal static class JeffersonWheelCipher
    {
        internal static string Transform(string input, string seedText, string offsetText, bool decrypt)
        {
            int seed, offset; if (!int.TryParse(seedText, out seed)) seed = 1776; if (!int.TryParse(offsetText, out offset)) offset = 3; string letters = CipherUtilities.Letters(input); StringBuilder r = new StringBuilder();
            for (int i = 0; i < letters.Length; i++) { string wheel = Wheel(seed, i); int index = wheel.IndexOf(letters[i]); r.Append(wheel[Alphabet.Mod(index + (decrypt ? -offset : offset), 26)]); } return r.ToString();
        }
        private static string Wheel(int seed, int position) { List<char> chars = new List<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()); Random random = new Random(unchecked(seed * 397 + position)); for (int i = chars.Count - 1; i > 0; i--) { int j = random.Next(i + 1); char c = chars[i]; chars[i] = chars[j]; chars[j] = c; } return new string(chars.ToArray()); }
    }

    internal static class AmscoCipher
    {
        internal static string Encrypt(string input, string key) { return Transform(input, key, false); }
        internal static string Decrypt(string input, string key) { return Transform(input, key, true); }
        private static string Transform(string input, string key, bool decrypt)
        {
            string source = input ?? string.Empty; int width = CipherUtilities.Letters(key).Length; if (width < 2) throw new CipherException("关键词至少需要 2 个字母"); List<int> lengths = CellLengths(source.Length, width); int[] order = ColumnOrder(key); int cells = lengths.Count, rows = (cells + width - 1) / width;
            if (!decrypt) { List<string> values = new List<string>(); int p = 0; foreach (int len in lengths) { values.Add(source.Substring(p, len)); p += len; } StringBuilder r = new StringBuilder(); for (int rank = 0; rank < width; rank++) { int col = Array.IndexOf(order, rank); for (int row = 0; row < rows; row++) { int cell = row * width + col; if (cell < values.Count) r.Append(values[cell]); } } return r.ToString(); }
            string[] cellValues = new string[cells]; int pos = 0; for (int rank = 0; rank < width; rank++) { int col = Array.IndexOf(order, rank); for (int row = 0; row < rows; row++) { int cell = row * width + col; if (cell < cells) { cellValues[cell] = source.Substring(pos, lengths[cell]); pos += lengths[cell]; } } } return string.Concat(cellValues);
        }
        private static List<int> CellLengths(int total, int width) { List<int> r = new List<int>(); int used = 0, cell = 0; while (used < total) { int len = Math.Min((cell++ % 2) + 1, total - used); r.Add(len); used += len; } return r; }
        private static int[] ColumnOrder(string key) { string k = CipherUtilities.Letters(key); List<int> indices = new List<int>(); for (int i = 0; i < k.Length; i++) indices.Add(i); indices.Sort(delegate(int a, int b) { int c = k[a].CompareTo(k[b]); return c != 0 ? c : a.CompareTo(b); }); int[] order = new int[k.Length]; for (int i = 0; i < indices.Count; i++) order[indices[i]] = i; return order; }
    }

    internal static class TurningGrilleCipher
    {
        internal static string Encrypt(string input, string sizeText, string holesText) { int size = Size(sizeText); List<int> holes = Holes(holesText, size); string source = input ?? string.Empty; int block = size * size; StringBuilder output = new StringBuilder(); for (int start = 0; start < source.Length; start += block) { char[] grid = new string('X', block).ToCharArray(); int p = start; List<int> current = new List<int>(holes); for (int turn = 0; turn < 4; turn++) { foreach (int h in current) if (p < Math.Min(source.Length, start + block)) grid[h] = source[p++]; current = Rotate(current, size); } output.Append(grid); } return output.ToString(); }
        internal static string Decrypt(string input, string sizeText, string holesText) { int size = Size(sizeText); List<int> holes = Holes(holesText, size); string source = input ?? string.Empty; int block = size * size; if (source.Length % block != 0) throw new CipherException("密文长度须为方阵大小的倍数"); StringBuilder output = new StringBuilder(); for (int start = 0; start < source.Length; start += block) { List<int> current = new List<int>(holes); for (int turn = 0; turn < 4; turn++) { foreach (int h in current) output.Append(source[start + h]); current = Rotate(current, size); } } return output.ToString().TrimEnd('X'); }
        private static int Size(string text) { int n; if (!int.TryParse(text, out n) || n < 2 || n > 10 || n % 2 != 0) throw new CipherException("方阵边长须为 2–10 的偶数"); return n; }
        private static List<int> Holes(string text, int n) { string[] parts = (text ?? string.Empty).Split(new[] {',', ' ', ';'}, StringSplitOptions.RemoveEmptyEntries); List<int> holes = new List<int>(); foreach (string part in parts) { int v; if (!int.TryParse(part, out v) || v < 1 || v > n * n) throw new CipherException("孔位使用 1 起始的位置编号"); holes.Add(v - 1); } holes.Sort(); if (holes.Count != n * n / 4) throw new CipherException("孔位数量须为方阵格数的四分之一"); HashSet<int> all = new HashSet<int>(); List<int> current = new List<int>(holes); for (int t = 0; t < 4; t++) { foreach (int h in current) if (!all.Add(h)) throw new CipherException("孔位旋转后发生重叠"); current = Rotate(current, n); } return holes; }
        private static List<int> Rotate(List<int> values, int n) { List<int> r = new List<int>(); foreach (int v in values) { int row = v / n, col = v % n; r.Add(col * n + (n - 1 - row)); } r.Sort(); return r; }
    }
}
