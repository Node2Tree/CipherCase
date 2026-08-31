using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class DigrafidCipher
    {
        private const string Basis = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.";
        internal static string Encrypt(string input, string key1, string key2, string periodText) { return Transform(input, key1, key2, periodText, false); }
        internal static string Decrypt(string input, string key1, string key2, string periodText) { return Transform(input, key1, key2, periodText, true); }
        private static string Transform(string input, string key1, string key2, string periodText, bool decrypt)
        {
            int period; if (!int.TryParse(periodText, out period) || period < 1) period = 5; string a = Keyed(key1), b = Keyed(key2), text = Clean(input); if (text.Length % 2 != 0) { if (decrypt) throw new CipherException("Digrafid 密文须为偶数长度"); text += "X"; } StringBuilder result = new StringBuilder();
            for (int start = 0; start < text.Length; start += period * 2) { int pairs = Math.Min(period, (text.Length - start) / 2); List<int[]> triples = new List<int[]>(); for (int i = 0; i < pairs; i++) triples.Add(Coordinates(text[start + i * 2], text[start + i * 2 + 1], a, b)); if (!decrypt) { List<int> stream = new List<int>(); for (int axis = 0; axis < 3; axis++) foreach (int[] triple in triples) stream.Add(triple[axis]); for (int i = 0; i < pairs; i++) result.Append(Decode(stream[i * 3], stream[i * 3 + 1], stream[i * 3 + 2], a, b)); } else { List<int> stream = new List<int>(); foreach (int[] triple in triples) stream.AddRange(triple); for (int i = 0; i < pairs; i++) result.Append(Decode(stream[i], stream[pairs + i], stream[pairs * 2 + i], a, b)); } } return result.ToString();
        }
        private static int[] Coordinates(char first, char second, string a, string b) { int x = a.IndexOf(first), y = b.IndexOf(second); return new[] {x % 9 + 1, (x / 9) * 3 + y % 3 + 1, y / 3 + 1}; }
        private static string Decode(int x, int middle, int z, string a, string b) { int firstRow = (middle - 1) / 3, secondCol = (middle - 1) % 3; return new string(new[] {a[firstRow * 9 + x - 1], b[(z - 1) * 3 + secondCol]}); }
        private static string Keyed(string key) { StringBuilder r = new StringBuilder(); foreach (char c in Clean(key) + Basis) if (r.ToString().IndexOf(c) < 0) r.Append(c); return r.ToString(); }
        private static string Clean(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in (input ?? string.Empty).ToUpperInvariant()) if (Basis.IndexOf(raw) >= 0) r.Append(raw); return r.ToString(); }
    }

    internal static class ThreeSquareCipher
    {
        private const string Normal = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        internal static string Encrypt(string input, string key1, string key2)
        {
            string left = CipherUtilities.KeyedAlphabet(key1, false), right = CipherUtilities.KeyedAlphabet(key2, false), text = FourSquareCipher.Normalize(input); if (text.Length % 2 != 0) text += "X"; StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i += 2) { int a = Normal.IndexOf(text[i]), b = Normal.IndexOf(text[i + 1]); int ar = a / 5, ac = a % 5, br = b / 5, bc = b % 5; result.Append(left[ar * 5 + bc]); result.Append(Normal[((ar + br) % 5) * 5 + ((ac + bc) % 5)]); result.Append(right[br * 5 + ac]); } return result.ToString();
        }
        internal static string Decrypt(string input, string key1, string key2)
        {
            string left = CipherUtilities.KeyedAlphabet(key1, false), right = CipherUtilities.KeyedAlphabet(key2, false), text = FourSquareCipher.Normalize(input); if (text.Length % 3 != 0) throw new CipherException("Three-square 密文长度须为 3 的倍数"); StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i += 3) { int a = left.IndexOf(text[i]), b = right.IndexOf(text[i + 2]); result.Append(Normal[(a / 5) * 5 + b % 5]); result.Append(Normal[(b / 5) * 5 + a % 5]); } return result.ToString();
        }
    }

    internal static class GrandpreCipher
    {
        internal static string Encrypt(string input, string key)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true); List<string>[] codes = Build(alphabet); int[] used = new int[26]; StringBuilder result = new StringBuilder(); foreach (char c in CipherUtilities.Letters(input)) { List<string> choices = codes[c - 'A']; if (result.Length > 0) result.Append(' '); result.Append(choices[used[c - 'A']++ % choices.Count]); } return result.ToString();
        }
        internal static string Decrypt(string input, string key)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true); string[] parts = (input ?? string.Empty).Split(new[] {' ', ',', ';', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); StringBuilder result = new StringBuilder(); foreach (string part in parts) { int value; if (part.Length != 2 || !int.TryParse(part, out value) || value < 0 || value > 99) throw new CipherException("Grandpré 坐标须为 00–99"); result.Append(alphabet[value % 26]); } return result.ToString();
        }
        private static List<string>[] Build(string alphabet) { List<string>[] result = new List<string>[26]; for (int i = 0; i < 26; i++) result[i] = new List<string>(); for (int p = 0; p < 100; p++) result[alphabet[p % 26] - 'A'].Add(p.ToString("00")); return result; }
    }

    internal static class NomenclatorCipher
    {
        internal static string Encrypt(string input, string mappingsText) { Dictionary<string, string> map = Parse(mappingsText); List<string> keys = new List<string>(map.Keys); keys.Sort(delegate(string a, string b) { return b.Length.CompareTo(a.Length); }); string source = (input ?? string.Empty).ToUpperInvariant(); StringBuilder r = new StringBuilder(); for (int i = 0; i < source.Length;) { string found = null; foreach (string key in keys) if (i + key.Length <= source.Length && string.Compare(source, i, key, 0, key.Length, StringComparison.Ordinal) == 0) { found = key; break; } if (found != null) { if (r.Length > 0 && !char.IsWhiteSpace(r[r.Length - 1])) r.Append(' '); r.Append(map[found]).Append(' '); i += found.Length; } else r.Append(source[i++]); } return r.ToString().Trim(); }
        internal static string Decrypt(string input, string mappingsText) { Dictionary<string, string> map = Parse(mappingsText), reverse = new Dictionary<string, string>(); foreach (KeyValuePair<string, string> p in map) reverse[p.Value] = p.Key; string[] tokens = (input ?? string.Empty).Split(new[] {' ', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); StringBuilder r = new StringBuilder(); foreach (string token in tokens) { string value; if (reverse.TryGetValue(token, out value)) r.Append(value); else r.Append(token); r.Append(' '); } return r.ToString().TrimEnd(); }
        private static Dictionary<string, string> Parse(string text) { Dictionary<string, string> r = new Dictionary<string, string>(); string[] parts = (text ?? string.Empty).Split(new[] {';', ',', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries); foreach (string part in parts) { string[] pair = part.Split('='); if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]) || string.IsNullOrWhiteSpace(pair[1])) throw new CipherException("命名码格式示例：KING=42;ARMY=731"); r[pair[0].Trim().ToUpperInvariant()] = pair[1].Trim(); } if (r.Count == 0) throw new CipherException("请输入命名码表"); return r; }
    }

    internal static class BookCipher
    {
        internal static string Encrypt(string input, string book)
        {
            string[] words = Words(book); StringBuilder result = new StringBuilder(); foreach (char c in CipherUtilities.Letters(input)) { string code = null; for (int w = 0; w < words.Length && code == null; w++) { int index = words[w].IndexOf(c); if (index >= 0) code = (w + 1) + "." + (index + 1); } if (code == null) throw new CipherException("书本密钥中缺少字母 " + c); if (result.Length > 0) result.Append(' '); result.Append(code); } return result.ToString();
        }
        internal static string Decrypt(string input, string book)
        {
            string[] words = Words(book), tokens = (input ?? string.Empty).Split(new[] {' ', ',', ';', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); StringBuilder result = new StringBuilder(); foreach (string token in tokens) { string[] pair = token.Split('.'); int word, letter; if (pair.Length != 2 || !int.TryParse(pair[0], out word) || !int.TryParse(pair[1], out letter) || word < 1 || word > words.Length || letter < 1 || letter > words[word - 1].Length) throw new CipherException("书本坐标格式示例：3.2"); result.Append(words[word - 1][letter - 1]); } return result.ToString();
        }
        private static string[] Words(string book) { string[] raw = (book ?? string.Empty).ToUpperInvariant().Split(new[] {' ', ',', '.', ';', ':', '-', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); List<string> words = new List<string>(); foreach (string item in raw) { string clean = CipherUtilities.Letters(item); if (clean.Length > 0) words.Add(clean); } if (words.Count == 0) throw new CipherException("请输入书本密钥文本"); return words.ToArray(); }
    }
}
