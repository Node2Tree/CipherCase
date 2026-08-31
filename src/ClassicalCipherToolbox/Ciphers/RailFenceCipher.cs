using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class RailFenceCipher : ICipher
    {
        public string Name { get { return "栅栏"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "栏数 3"; } }

        public string Encrypt(string input, string key)
        {
            int rails = ReadRails(key);
            if (rails == 1 || string.IsNullOrEmpty(input)) return input ?? string.Empty;
            StringBuilder[] rows = new StringBuilder[rails];
            for (int i = 0; i < rails; i++) rows[i] = new StringBuilder();
            int row = 0;
            int direction = 1;
            foreach (char value in input)
            {
                rows[row].Append(value);
                if (row == 0) direction = 1;
                else if (row == rails - 1) direction = -1;
                row += direction;
            }
            StringBuilder result = new StringBuilder(input.Length);
            foreach (StringBuilder value in rows) result.Append(value);
            return result.ToString();
        }

        public string Decrypt(string input, string key)
        {
            int rails = ReadRails(key);
            if (rails == 1 || string.IsNullOrEmpty(input)) return input ?? string.Empty;
            int[] pattern = new int[input.Length];
            int[] counts = new int[rails];
            int row = 0;
            int direction = 1;
            for (int i = 0; i < input.Length; i++)
            {
                pattern[i] = row;
                counts[row]++;
                if (row == 0) direction = 1;
                else if (row == rails - 1) direction = -1;
                row += direction;
            }
            char[][] rows = new char[rails][];
            int position = 0;
            for (int i = 0; i < rails; i++)
            {
                rows[i] = input.Substring(position, counts[i]).ToCharArray();
                position += counts[i];
            }
            int[] indexes = new int[rails];
            StringBuilder result = new StringBuilder(input.Length);
            for (int i = 0; i < pattern.Length; i++)
                result.Append(rows[pattern[i]][indexes[pattern[i]]++]);
            return result.ToString();
        }

        private static int ReadRails(string key)
        {
            int rails;
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out rails) || rails < 1 || rails > 100)
                throw new CipherException("栏数须为 1–100");
            return rails;
        }

        public override string ToString() { return Name; }
    }
}
