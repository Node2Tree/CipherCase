using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class Rot13Cipher : ICipher
    {
        public string Name { get { return "ROT13"; } }
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
                result.Append(Alphabet.Shift(value, 13));
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
