using System;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class RotNCipher : ICipher
    {
        public string Name { get { return "ROT-N"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "13 / 47 / 5 / 18"; } }

        public string Encrypt(string input, string key) { return Transform(input, key, false); }
        public string Decrypt(string input, string key) { return Transform(input, key, true); }

        private static string Transform(string input, string key, bool decrypt)
        {
            int rotation;
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out rotation))
            {
                throw new CipherException("ROT 请输入整数");
            }

            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            foreach (char value in input ?? string.Empty)
            {
                if (rotation == 47 && value >= 33 && value <= 126)
                {
                    result.Append((char)(33 + ((value - 33 + 47) % 94)));
                }
                else if (rotation == 5 && value >= '0' && value <= '9')
                {
                    result.Append((char)('0' + ((value - '0' + 5) % 10)));
                }
                else if (rotation == 18)
                {
                    if (value >= '0' && value <= '9')
                    {
                        result.Append((char)('0' + ((value - '0' + 5) % 10)));
                    }
                    else
                    {
                        result.Append(Alphabet.Shift(value, 13));
                    }
                }
                else
                {
                    result.Append(Alphabet.Shift(value, decrypt ? -rotation : rotation));
                }
            }

            return result.ToString();
        }

        public override string ToString() { return Name; }
    }
}
