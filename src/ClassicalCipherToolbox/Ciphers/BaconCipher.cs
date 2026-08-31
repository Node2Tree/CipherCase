using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class BaconCipher : ICipher
    {
        public string Name { get { return "培根"; } }
        public bool RequiresKey { get { return false; } }
        public string KeyHint { get { return string.Empty; } }
        public string Encrypt(string input, string key)
        {
            StringBuilder result = new StringBuilder();
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int index = Alphabet.IndexOf(value);
                for (int bit = 4; bit >= 0; bit--) result.Append((index & (1 << bit)) == 0 ? 'A' : 'B');
            }
            return result.ToString();
        }
        public string Decrypt(string input, string key)
        {
            StringBuilder result = new StringBuilder();
            StringBuilder block = new StringBuilder(5);
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (value == 'A' || value == 'B')
                {
                    block.Append(value);
                    if (block.Length == 5)
                    {
                        int index = 0;
                        foreach (char bit in block.ToString()) index = index * 2 + (bit == 'B' ? 1 : 0);
                        result.Append(index < 26 ? (char)('A' + index) : '?');
                        block.Clear();
                    }
                }
                else result.Append(raw);
            }
            if (block.Length > 0) result.Append(block);
            return result.ToString();
        }
        public override string ToString() { return Name; }
    }
}
