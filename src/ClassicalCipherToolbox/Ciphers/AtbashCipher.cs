using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class AtbashCipher : ICipher
    {
        public string Name { get { return "Atbash"; } }
        public bool RequiresKey { get { return false; } }
        public string KeyHint { get { return string.Empty; } }

        public string Encrypt(string input, string key)
        {
            return Transform(input);
        }

        public string Decrypt(string input, string key)
        {
            return Transform(input);
        }

        private static string Transform(string input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(input.Length);
            foreach (char value in input)
            {
                if (!Alphabet.IsAsciiLetter(value))
                {
                    result.Append(value);
                    continue;
                }

                bool lowerCase = value >= 'a' && value <= 'z';
                result.Append(Alphabet.FromIndex(25 - Alphabet.IndexOf(value), lowerCase));
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
