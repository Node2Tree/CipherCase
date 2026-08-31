using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class PolybiusCipher : ICipher
    {
        private const string BaseAlphabet = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        public string Name { get { return "Polybius"; } }
        public bool RequiresKey { get { return false; } }
        public string KeyHint { get { return "方阵关键词（可选）"; } }
        public string Encrypt(string input, string key)
        {
            string square = BuildSquare(key);
            StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw) == 'J' ? 'I' : char.ToUpperInvariant(raw);
                int index = square.IndexOf(value);
                if (index < 0) result.Append(raw);
                else { result.Append((char)('1' + index / 5)); result.Append((char)('1' + index % 5)); }
            }
            return result.ToString();
        }
        public string Decrypt(string input, string key)
        {
            string square = BuildSquare(key);
            StringBuilder result = new StringBuilder();
            input = input ?? string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                if (i + 1 < input.Length && input[i] >= '1' && input[i] <= '5' && input[i + 1] >= '1' && input[i + 1] <= '5')
                {
                    result.Append(square[(input[i] - '1') * 5 + input[i + 1] - '1']);
                    i++;
                }
                else result.Append(input[i]);
            }
            return result.ToString();
        }
        internal static string BuildSquare(string key)
        {
            StringBuilder square = new StringBuilder(25);
            string source = (key ?? string.Empty).ToUpperInvariant() + BaseAlphabet;
            foreach (char raw in source)
            {
                char value = raw == 'J' ? 'I' : raw;
                if (BaseAlphabet.IndexOf(value) >= 0 && square.ToString().IndexOf(value) < 0) square.Append(value);
            }
            return square.ToString();
        }
        public override string ToString() { return Name; }
    }
}
