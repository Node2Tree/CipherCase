using System;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class HillCipher : ICipher
    {
        public string Name { get { return "Hill 2×2"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "3,3,2,5"; } }
        public string Encrypt(string input, string key) { return Transform(input, ReadMatrix(key), false); }
        public string Decrypt(string input, string key) { return Transform(input, ReadMatrix(key), true); }
        private static int[] ReadMatrix(string key)
        {
            string[] parts = (key ?? string.Empty).Replace('，', ',').Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) throw new CipherException("矩阵格式：3,3,2,5");
            int[] matrix = new int[4];
            for (int i = 0; i < 4; i++)
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out matrix[i]))
                    throw new CipherException("矩阵须为 4 个整数");
            int determinant = Alphabet.Mod(matrix[0] * matrix[3] - matrix[1] * matrix[2], 26);
            if (Inverse(determinant) < 0) throw new CipherException("矩阵在模 26 下不可逆");
            return matrix;
        }
        private static string Transform(string input, int[] matrix, bool decrypt)
        {
            StringBuilder letters = new StringBuilder();
            foreach (char value in input ?? string.Empty)
                if (Alphabet.IsAsciiLetter(value)) letters.Append(char.ToUpperInvariant(value));
            if (letters.Length % 2 != 0)
            {
                if (decrypt) throw new CipherException("Hill 密文长度须为偶数");
                letters.Append('X');
            }
            int[] m = matrix;
            if (decrypt)
            {
                int determinant = Alphabet.Mod(m[0] * m[3] - m[1] * m[2], 26);
                int inverse = Inverse(determinant);
                m = new[] { m[3] * inverse, -m[1] * inverse, -m[2] * inverse, m[0] * inverse };
            }
            StringBuilder result = new StringBuilder(letters.Length);
            for (int i = 0; i < letters.Length; i += 2)
            {
                int a = letters[i] - 'A', b = letters[i + 1] - 'A';
                result.Append(Alphabet.FromIndex(m[0] * a + m[1] * b, false));
                result.Append(Alphabet.FromIndex(m[2] * a + m[3] * b, false));
            }
            return result.ToString();
        }
        private static int Inverse(int value)
        {
            for (int i = 1; i < 26; i++) if (Alphabet.Mod(value * i, 26) == 1) return i;
            return -1;
        }
        public override string ToString() { return Name; }
    }
}
