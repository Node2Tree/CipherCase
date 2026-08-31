using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class ColumnarTranspositionCipher : ICipher
    {
        public string Name { get { return "列换位"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "关键词"; } }
        public string Encrypt(string input, string key) { return EncryptText(input, key); }
        public string Decrypt(string input, string key) { return DecryptText(input, key); }

        internal static string EncryptText(string input, string key)
        {
            int[] order = ReadOrder(key);
            input = input ?? string.Empty;
            StringBuilder result = new StringBuilder(input.Length);
            foreach (int column in order)
                for (int index = column; index < input.Length; index += order.Length)
                    result.Append(input[index]);
            return result.ToString();
        }

        internal static string DecryptText(string input, string key)
        {
            int[] order = ReadOrder(key);
            input = input ?? string.Empty;
            int columns = order.Length;
            int shortLength = input.Length / columns;
            int longColumns = input.Length % columns;
            char[][] data = new char[columns][];
            int position = 0;
            foreach (int column in order)
            {
                int length = shortLength + (column < longColumns ? 1 : 0);
                data[column] = input.Substring(position, length).ToCharArray();
                position += length;
            }
            StringBuilder result = new StringBuilder(input.Length);
            int rows = (input.Length + columns - 1) / columns;
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                    if (row < data[column].Length) result.Append(data[column][row]);
            return result.ToString();
        }

        private static int[] ReadOrder(string key)
        {
            key = (key ?? string.Empty).Trim();
            if (key.Length < 2) throw new CipherException("关键词至少 2 个字符");
            List<int> indexes = new List<int>();
            for (int i = 0; i < key.Length; i++) indexes.Add(i);
            indexes.Sort(delegate(int left, int right)
            {
                int compare = char.ToUpperInvariant(key[left]).CompareTo(char.ToUpperInvariant(key[right]));
                return compare != 0 ? compare : left.CompareTo(right);
            });
            return indexes.ToArray();
        }

        public override string ToString() { return Name; }
    }
}
