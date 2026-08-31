using System;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class CaesarCipher : ICipher
    {
        public string Name { get { return "凯撒"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "3"; } }

        public string Encrypt(string input, string key)
        {
            return Transform(input, ReadShift(key));
        }

        public string Decrypt(string input, string key)
        {
            return Transform(input, -ReadShift(key));
        }

        private static int ReadShift(string key)
        {
            int shift;
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out shift))
            {
                throw new CipherException("密钥请输入整数");
            }

            return Alphabet.Mod(shift, Alphabet.Length);
        }

        private static string Transform(string input, int shift)
        {
            if (input == null)
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(input.Length);
            foreach (char value in input)
            {
                result.Append(Alphabet.Shift(value, shift));
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
