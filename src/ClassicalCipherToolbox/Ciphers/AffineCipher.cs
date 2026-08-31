using System;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class AffineCipher : ICipher
    {
        public string Name { get { return "仿射"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "5,8"; } }

        public string Encrypt(string input, string key)
        {
            int a;
            int b;
            ReadKey(key, out a, out b);
            return Transform(input, a, b, false);
        }

        public string Decrypt(string input, string key)
        {
            int a;
            int b;
            ReadKey(key, out a, out b);
            return Transform(input, a, b, true);
        }

        private static string Transform(string input, int a, int b, bool decrypt)
        {
            if (input == null)
            {
                return string.Empty;
            }

            int inverse = decrypt ? ModularInverse(a, Alphabet.Length) : 0;
            StringBuilder result = new StringBuilder(input.Length);
            foreach (char value in input)
            {
                if (!Alphabet.IsAsciiLetter(value))
                {
                    result.Append(value);
                    continue;
                }

                int index = Alphabet.IndexOf(value);
                int transformed = decrypt
                    ? inverse * (index - b)
                    : a * index + b;
                bool lowerCase = value >= 'a' && value <= 'z';
                result.Append(Alphabet.FromIndex(transformed, lowerCase));
            }

            return result.ToString();
        }

        private static void ReadKey(string key, out int a, out int b)
        {
            string normalized = (key ?? string.Empty).Replace('，', ',');
            string[] parts = normalized.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out a) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
            {
                throw new CipherException("密钥格式：5,8");
            }

            a = Alphabet.Mod(a, Alphabet.Length);
            b = Alphabet.Mod(b, Alphabet.Length);
            if (GreatestCommonDivisor(a, Alphabet.Length) != 1)
            {
                throw new CipherException("第一个数须与 26 互质");
            }
        }

        private static int GreatestCommonDivisor(int left, int right)
        {
            while (right != 0)
            {
                int remainder = left % right;
                left = right;
                right = remainder;
            }

            return Math.Abs(left);
        }

        private static int ModularInverse(int value, int modulus)
        {
            for (int candidate = 1; candidate < modulus; candidate++)
            {
                if ((value * candidate) % modulus == 1)
                {
                    return candidate;
                }
            }

            throw new CipherException("密钥不可逆");
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
