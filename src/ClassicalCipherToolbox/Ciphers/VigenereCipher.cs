using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class VigenereCipher : ICipher
    {
        public string Name { get { return "维吉尼亚"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "KEY"; } }

        public string Encrypt(string input, string key)
        {
            return Transform(input, key, false);
        }

        public string Decrypt(string input, string key)
        {
            return Transform(input, key, true);
        }

        private static string Transform(string input, string key, bool decrypt)
        {
            IList<int> shifts = ReadKey(key);
            if (input == null)
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(input.Length);
            int keyIndex = 0;
            foreach (char value in input)
            {
                if (!Alphabet.IsAsciiLetter(value))
                {
                    result.Append(value);
                    continue;
                }

                int shift = shifts[keyIndex % shifts.Count];
                result.Append(Alphabet.Shift(value, decrypt ? -shift : shift));
                keyIndex++;
            }

            return result.ToString();
        }

        private static IList<int> ReadKey(string key)
        {
            List<int> shifts = new List<int>();
            if (key != null)
            {
                foreach (char value in key)
                {
                    if (Alphabet.IsAsciiLetter(value))
                    {
                        shifts.Add(Alphabet.IndexOf(value));
                    }
                }
            }

            if (shifts.Count == 0)
            {
                throw new CipherException("密钥请输入英文字母");
            }

            return shifts;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
