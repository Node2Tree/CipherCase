using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class AdfgxCipher
    {
        internal static string Encrypt(string input, string squareKey, string transpositionKey, bool withDigits)
        {
            string alphabet = BuildAlphabet(squareKey, withDigits);
            string coordinates = withDigits ? "ADFGVX" : "ADFGX";
            int size = coordinates.Length;
            StringBuilder substituted = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (!withDigits && value == 'J') value = 'I';
                int index = alphabet.IndexOf(value);
                if (index >= 0) substituted.Append(coordinates[index / size]).Append(coordinates[index % size]);
            }
            return ColumnarTranspositionCipher.EncryptText(substituted.ToString(), transpositionKey);
        }
        internal static string Decrypt(string input, string squareKey, string transpositionKey, bool withDigits)
        {
            string alphabet = BuildAlphabet(squareKey, withDigits);
            string coordinates = withDigits ? "ADFGVX" : "ADFGX";
            int size = coordinates.Length;
            string stream = ColumnarTranspositionCipher.DecryptText(NormalizeCoordinates(input, coordinates), transpositionKey);
            if (stream.Length % 2 != 0) throw new CipherException("坐标密文长度须为偶数");
            StringBuilder result = new StringBuilder(stream.Length / 2);
            for (int i = 0; i < stream.Length; i += 2)
            {
                int row = coordinates.IndexOf(stream[i]);
                int column = coordinates.IndexOf(stream[i + 1]);
                if (row < 0 || column < 0) throw new CipherException("密文包含无效坐标");
                result.Append(alphabet[row * size + column]);
            }
            return result.ToString();
        }
        private static string BuildAlphabet(string key, bool withDigits)
        {
            string baseAlphabet = withDigits ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" : "ABCDEFGHIKLMNOPQRSTUVWXYZ";
            StringBuilder result = new StringBuilder(baseAlphabet.Length);
            foreach (char raw in (key ?? string.Empty).ToUpperInvariant() + baseAlphabet)
            {
                char value = !withDigits && raw == 'J' ? 'I' : raw;
                if (baseAlphabet.IndexOf(value) >= 0 && result.ToString().IndexOf(value) < 0) result.Append(value);
            }
            return result.ToString();
        }
        private static string NormalizeCoordinates(string input, string coordinates)
        {
            StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (coordinates.IndexOf(value) >= 0) result.Append(value);
            }
            return result.ToString();
        }
    }
}
