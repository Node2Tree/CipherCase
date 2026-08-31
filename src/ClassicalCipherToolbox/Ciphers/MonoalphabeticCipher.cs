using System;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class MonoalphabeticCipher : ICipher
    {
        public string Name { get { return "单表替换"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "26 字母替换表"; } }
        public string Encrypt(string input, string key) { return Transform(input, ReadKey(key), false); }
        public string Decrypt(string input, string key) { return Transform(input, ReadKey(key), true); }

        private static string ReadKey(string key)
        {
            string value = (key ?? string.Empty).ToUpperInvariant();
            if (value.Length != 26) throw new CipherException("替换表须为 26 个字母");
            bool[] used = new bool[26];
            foreach (char character in value)
            {
                if (character < 'A' || character > 'Z' || used[character - 'A'])
                    throw new CipherException("替换表须包含 26 个不重复字母");
                used[character - 'A'] = true;
            }
            return value;
        }

        private static string Transform(string input, string key, bool decrypt)
        {
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                bool lower = value >= 'a' && value <= 'z';
                char mapped;
                if (!decrypt) mapped = key[Alphabet.IndexOf(value)];
                else mapped = (char)('A' + key.IndexOf(char.ToUpperInvariant(value)));
                result.Append(lower ? char.ToLowerInvariant(mapped) : mapped);
            }
            return result.ToString();
        }

        public override string ToString() { return Name; }
    }
}
