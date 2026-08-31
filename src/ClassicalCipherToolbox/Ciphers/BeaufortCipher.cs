using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class BeaufortCipher : ICipher
    {
        public string Name { get { return "Beaufort"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "KEY"; } }
        public string Encrypt(string input, string key) { return Transform(input, key); }
        public string Decrypt(string input, string key) { return Transform(input, key); }
        private static string Transform(string input, string key)
        {
            List<int> shifts = ReadKey(key);
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            int position = 0;
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int index = shifts[position++ % shifts.Count] - Alphabet.IndexOf(value);
                result.Append(Alphabet.FromIndex(index, value >= 'a' && value <= 'z'));
            }
            return result.ToString();
        }
        private static List<int> ReadKey(string key)
        {
            List<int> result = new List<int>();
            foreach (char value in key ?? string.Empty)
                if (Alphabet.IsAsciiLetter(value)) result.Add(Alphabet.IndexOf(value));
            if (result.Count == 0) throw new CipherException("密钥请输入英文字母");
            return result;
        }
        public override string ToString() { return Name; }
    }
}
