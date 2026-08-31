using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class QuagmireCipher
    {
        internal static string Transform(string input, string variant, string key1, string key2, string indicator, bool decrypt)
        {
            int kind; if (!int.TryParse(variant, out kind) || kind < 1 || kind > 4) kind = 1;
            string keyed1 = CipherUtilities.KeyedAlphabet(key1, true), keyed2 = CipherUtilities.KeyedAlphabet(key2, true);
            string plain = kind == 1 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : keyed1;
            string cipher = kind == 2 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : kind == 4 ? keyed2 : keyed1;
            string shifts = CipherUtilities.Letters(indicator); if (shifts.Length == 0) throw new CipherException("请输入指示词");
            StringBuilder result = new StringBuilder(); int position = 0;
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                int shift = shifts[position++ % shifts.Length] - 'A', index;
                if (!decrypt) { index = plain.IndexOf(char.ToUpperInvariant(raw)); index = Alphabet.Mod(index + shift, 26); result.Append(MatchCase(cipher[index], raw)); }
                else { index = cipher.IndexOf(char.ToUpperInvariant(raw)); index = Alphabet.Mod(index - shift, 26); result.Append(MatchCase(plain[index], raw)); }
            }
            return result.ToString();
        }
        private static char MatchCase(char value, char source) { return char.IsLower(source) ? char.ToLowerInvariant(value) : value; }
    }

    internal static class GromarkCipher
    {
        internal static string Transform(string input, string key, string primer, string periodText, bool periodic, bool decrypt)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true), digits = Digits(primer); if (digits.Length < 2) throw new CipherException("数字引子至少需要 2 位");
            int period; if (!int.TryParse(periodText, out period) || period < 1) period = 10;
            StringBuilder result = new StringBuilder(); List<int> stream = new List<int>(); int letters = 0;
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                int local = periodic ? letters % period : letters; if (periodic && local == 0) stream.Clear();
                while (stream.Count <= local)
                {
                    if (stream.Count < digits.Length) stream.Add(digits[stream.Count] - '0');
                    else stream.Add((stream[stream.Count - digits.Length] + stream[stream.Count - digits.Length + 1]) % 10);
                }
                int source = alphabet.IndexOf(char.ToUpperInvariant(raw)), value = Alphabet.Mod(source + (decrypt ? -stream[local] : stream[local]), 26);
                char output = alphabet[value]; result.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output); letters++;
            }
            return result.ToString();
        }
        private static string Digits(string value) { StringBuilder r = new StringBuilder(); foreach (char c in value ?? string.Empty) if (char.IsDigit(c)) r.Append(c); return r.ToString(); }
    }

    internal static class ChaocipherCipher
    {
        internal static string Transform(string input, string leftKey, string rightKey, bool decrypt)
        {
            string left = CipherUtilities.KeyedAlphabet(leftKey, true), right = CipherUtilities.KeyedAlphabet(rightKey, true); StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                char value = char.ToUpperInvariant(raw); int index = decrypt ? left.IndexOf(value) : right.IndexOf(value); char output = decrypt ? right[index] : left[index]; result.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output);
                left = PermuteLeft(left, index); right = PermuteRight(right, index);
            }
            return result.ToString();
        }
        private static string Rotate(string value, int amount) { amount = Alphabet.Mod(amount, value.Length); return value.Substring(amount) + value.Substring(0, amount); }
        private static string Move(string value, int from, int to) { List<char> chars = new List<char>(value.ToCharArray()); char c = chars[from]; chars.RemoveAt(from); chars.Insert(to, c); return new string(chars.ToArray()); }
        private static string PermuteLeft(string value, int index) { value = Rotate(value, index); return Move(value, 1, 13); }
        private static string PermuteRight(string value, int index) { value = Rotate(value, index + 1); return Move(value, 2, 13); }
    }

    internal static class SolitaireCipher
    {
        internal static string Transform(string input, string passphrase, bool decrypt)
        {
            List<int> deck = new List<int>(); for (int i = 1; i <= 54; i++) deck.Add(i); KeyDeck(deck, CipherUtilities.Letters(passphrase)); StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                int key = Next(deck), value = Alphabet.Mod(char.ToUpperInvariant(raw) - 'A' + (decrypt ? -key : key), 26); char c = (char)('A' + value); result.Append(char.IsLower(raw) ? char.ToLowerInvariant(c) : c);
            }
            return result.ToString();
        }
        private static void KeyDeck(List<int> deck, string key) { foreach (char c in key) { Step(deck); CountCut(deck, c - 'A' + 1); } }
        private static int Next(List<int> deck) { while (true) { Step(deck); int top = Math.Min(53, deck[0]), value = deck[top]; if (value < 53) return (value - 1) % 26 + 1; } }
        private static void Step(List<int> deck) { MoveDown(deck, 53, 1); MoveDown(deck, 54, 2); int a = deck.IndexOf(53), b = deck.IndexOf(54), first = Math.Min(a, b), last = Math.Max(a, b); List<int> next = new List<int>(); next.AddRange(deck.GetRange(last + 1, deck.Count - last - 1)); next.AddRange(deck.GetRange(first, last - first + 1)); next.AddRange(deck.GetRange(0, first)); deck.Clear(); deck.AddRange(next); CountCut(deck, Math.Min(53, deck[deck.Count - 1])); }
        private static void MoveDown(List<int> deck, int card, int count) { for (int n = 0; n < count; n++) { int p = deck.IndexOf(card); deck.RemoveAt(p); deck.Insert(p == deck.Count ? 1 : p + 1, card); } }
        private static void CountCut(List<int> deck, int count) { if (count <= 0 || count >= deck.Count) return; int bottom = deck[deck.Count - 1]; List<int> top = deck.GetRange(0, count); deck.RemoveRange(0, count); deck.RemoveAt(deck.Count - 1); deck.AddRange(top); deck.Add(bottom); }
    }

    internal static class PhillipsCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string baseSquare = PolybiusCipher.BuildSquare(key); StringBuilder result = new StringBuilder(); int p = 0;
            foreach (char raw in input ?? string.Empty)
            {
                char normalized = char.ToUpperInvariant(raw); if (normalized == 'J') normalized = 'I'; if (normalized < 'A' || normalized > 'Z') { result.Append(raw); continue; }
                string square = RotateRows(baseSquare, p / 5); int index = square.IndexOf(normalized), row = index / 5, col = index % 5; int delta = decrypt ? -1 : 1; char output = square[Alphabet.Mod(row + delta, 5) * 5 + Alphabet.Mod(col + delta, 5)]; result.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output); p++;
            }
            return result.ToString();
        }
        private static string RotateRows(string square, int block) { char[] r = square.ToCharArray(); int row = block % 5; char first = r[row * 5]; for (int c = 0; c < 4; c++) r[row * 5 + c] = r[row * 5 + c + 1]; r[row * 5 + 4] = first; return new string(r); }
    }

    internal static class SwagmanCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string clean = CipherUtilities.Letters(key); if (clean.Length < 2 || clean.Length > 12) throw new CipherException("关键词长度须为 2–12"); int n = clean.Length; int[] order = Order(clean); string source = input ?? string.Empty; StringBuilder result = new StringBuilder(source.Length);
            for (int start = 0; start < source.Length; start += n * n) { int length = Math.Min(n * n, source.Length - start); string part = source.Substring(start, length); if (length < n * n) { result.Append(decrypt ? ColumnarTranspositionCipher.DecryptText(part, clean) : ColumnarTranspositionCipher.EncryptText(part, clean)); continue; } char[] block = part.ToCharArray(), output = new char[length]; for (int i = 0; i < length; i++) { int row = i / n, col = i % n, target = row * n + ((order[col] + row) % n); if (!decrypt) output[target] = block[i]; else output[i] = block[target]; } result.Append(output); }
            return result.ToString();
        }
        private static int[] Order(string key) { List<int> p = new List<int>(); for (int i = 0; i < key.Length; i++) p.Add(i); p.Sort(delegate(int a, int b) { int c = key[a].CompareTo(key[b]); return c != 0 ? c : a.CompareTo(b); }); int[] r = new int[key.Length]; for (int i = 0; i < p.Count; i++) r[p[i]] = i; return r; }
    }

    internal static class CadenusCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string clean = CipherUtilities.Letters(key); if (clean.Length < 2) throw new CipherException("关键词至少需要 2 个字母"); int width = clean.Length, blockSize = width * 25; StringBuilder result = new StringBuilder();
            for (int start = 0; start < (input ?? string.Empty).Length; start += blockSize)
            {
                string block = input.Substring(start, Math.Min(blockSize, input.Length - start));
                if (block.Length < blockSize) { result.Append(decrypt ? ColumnarTranspositionCipher.DecryptText(block, clean) : ColumnarTranspositionCipher.EncryptText(block, clean)); continue; }
                if (!decrypt) { char[] shifted = ShiftBlock(block, clean, false); result.Append(ColumnarTranspositionCipher.EncryptText(new string(shifted), clean)); }
                else { string shifted = ColumnarTranspositionCipher.DecryptText(block, clean); result.Append(ShiftBlock(shifted, clean, true)); }
            }
            return result.ToString();
        }
        private static char[] ShiftBlock(string block, string key, bool inverse)
        {
            int width = key.Length; char[] output = new char[block.Length]; for (int i = 0; i < block.Length; i++) { int row = i / width, col = i % width, shift = key[col] - 'A', target = Alphabet.Mod(row - shift, 25) * width + col; if (target >= block.Length) target = i; if (!inverse) output[target] = block[i]; else output[i] = block[target]; } return output;
        }
    }

    internal static class NicodemusCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string clean = CipherUtilities.Letters(key); if (clean.Length < 2) throw new CipherException("关键词至少需要 2 个字母"); VigenereCipher v = new VigenereCipher();
            if (!decrypt) return ColumnarTranspositionCipher.EncryptText(v.Encrypt(input, clean), clean);
            return v.Decrypt(ColumnarTranspositionCipher.DecryptText(input, clean), clean);
        }
    }

    internal static class DisruptedTranspositionCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string clean = CipherUtilities.Letters(key); if (clean.Length < 2) throw new CipherException("关键词至少需要 2 个字母"); int n = clean.Length; int[] ranks = Ranks(clean); List<int> positions = new List<int>(); int row = 0;
            while (positions.Count < (input ?? string.Empty).Length) { int length = ranks[row % n] + 1; for (int c = 0; c < length && positions.Count < input.Length; c++) positions.Add(row * n + c); row++; }
            int cells = row * n; char[] grid = new char[cells]; if (!decrypt) { for (int i = 0; i < positions.Count; i++) grid[positions[i]] = input[i]; StringBuilder r = new StringBuilder(); for (int rank = 0; rank < n; rank++) { int col = Array.IndexOf(ranks, rank); for (int rr = 0; rr < row; rr++) { int p = rr * n + col; if (p < grid.Length && grid[p] != '\0') r.Append(grid[p]); } } return r.ToString(); }
            int source = 0; for (int rank = 0; rank < n; rank++) { int col = Array.IndexOf(ranks, rank); for (int rr = 0; rr < row; rr++) { int p = rr * n + col; if (positions.Contains(p) && source < input.Length) grid[p] = input[source++]; } } StringBuilder plain = new StringBuilder(); foreach (int p in positions) plain.Append(grid[p]); return plain.ToString();
        }
        private static int[] Ranks(string key) { List<int> order = new List<int>(); for (int i = 0; i < key.Length; i++) order.Add(i); order.Sort(delegate(int a, int b) { int c = key[a].CompareTo(key[b]); return c != 0 ? c : a.CompareTo(b); }); int[] ranks = new int[key.Length]; for (int i = 0; i < order.Count; i++) ranks[order[i]] = i; return ranks; }
    }
}
