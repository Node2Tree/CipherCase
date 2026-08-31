using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class PlayfairCipher : ICipher
    {
        public string Name { get { return "Playfair"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "方阵关键词"; } }
        public string Encrypt(string input, string key)
        {
            string letters = Normalize(input);
            StringBuilder pairs = new StringBuilder();
            for (int i = 0; i < letters.Length; i++)
            {
                char left = letters[i];
                char right = i + 1 < letters.Length ? letters[i + 1] : 'X';
                if (left == right) right = left == 'X' ? 'Q' : 'X';
                else i++;
                pairs.Append(left).Append(right);
            }
            return Transform(pairs.ToString(), PolybiusCipher.BuildSquare(key), 1);
        }
        public string Decrypt(string input, string key)
        {
            string letters = Normalize(input);
            if (letters.Length % 2 != 0) throw new CipherException("Playfair 密文长度须为偶数");
            return Transform(letters, PolybiusCipher.BuildSquare(key), -1);
        }
        private static string Transform(string input, string square, int direction)
        {
            StringBuilder result = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i += 2)
            {
                int left = square.IndexOf(input[i]);
                int right = square.IndexOf(input[i + 1]);
                int lr = left / 5, lc = left % 5, rr = right / 5, rc = right % 5;
                if (lr == rr)
                {
                    lc = Alphabet.Mod(lc + direction, 5); rc = Alphabet.Mod(rc + direction, 5);
                }
                else if (lc == rc)
                {
                    lr = Alphabet.Mod(lr + direction, 5); rr = Alphabet.Mod(rr + direction, 5);
                }
                else
                {
                    int swap = lc; lc = rc; rc = swap;
                }
                result.Append(square[lr * 5 + lc]).Append(square[rr * 5 + rc]);
            }
            return result.ToString();
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
        public override string ToString() { return Name; }
    }
}
