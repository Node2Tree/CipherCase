using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class BifidCipher
    {
        internal static string Encrypt(string input, string key, string periodText)
        {
            string square = PolybiusCipher.BuildSquare(key);
            string letters = Normalize(input);
            int period = ReadPeriod(periodText);
            StringBuilder result = new StringBuilder(letters.Length);
            for (int start = 0; start < letters.Length; start += period)
            {
                int count = System.Math.Min(period, letters.Length - start);
                int[] values = new int[count * 2];
                for (int i = 0; i < count; i++)
                {
                    int index = square.IndexOf(letters[start + i]);
                    values[i] = index / 5;
                    values[count + i] = index % 5;
                }
                for (int i = 0; i < values.Length; i += 2) result.Append(square[values[i] * 5 + values[i + 1]]);
            }
            return result.ToString();
        }
        internal static string Decrypt(string input, string key, string periodText)
        {
            string square = PolybiusCipher.BuildSquare(key);
            string letters = Normalize(input);
            int period = ReadPeriod(periodText);
            StringBuilder result = new StringBuilder(letters.Length);
            for (int start = 0; start < letters.Length; start += period)
            {
                int count = System.Math.Min(period, letters.Length - start);
                int[] stream = new int[count * 2];
                for (int i = 0; i < count; i++)
                {
                    int index = square.IndexOf(letters[start + i]);
                    stream[i * 2] = index / 5;
                    stream[i * 2 + 1] = index % 5;
                }
                for (int i = 0; i < count; i++) result.Append(square[stream[i] * 5 + stream[count + i]]);
            }
            return result.ToString();
        }
        private static int ReadPeriod(string text)
        {
            int period;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out period) || period < 1 || period > 1000)
                throw new CipherException("周期须为 1–1000");
            return period;
        }
        private static string Normalize(string input)
        {
            StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (value >= 'A' && value <= 'Z') result.Append(value == 'J' ? 'I' : value);
            }
            return result.ToString();
        }
    }
}
