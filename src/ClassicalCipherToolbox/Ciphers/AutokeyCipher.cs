using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class AutokeyCipher : ICipher
    {
        public string Name { get { return "Autokey"; } }
        public bool RequiresKey { get { return true; } }
        public string KeyHint { get { return "初始密钥"; } }
        public string Encrypt(string input, string key)
        {
            List<int> stream = ReadKey(key);
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            int position = 0;
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int plain = Alphabet.IndexOf(value);
                result.Append(Alphabet.FromIndex(plain + stream[position], value >= 'a' && value <= 'z'));
                stream.Add(plain);
                position++;
            }
            return result.ToString();
        }
        public string Decrypt(string input, string key)
        {
            List<int> stream = ReadKey(key);
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            int position = 0;
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int plain = Alphabet.Mod(Alphabet.IndexOf(value) - stream[position], 26);
                result.Append(Alphabet.FromIndex(plain, value >= 'a' && value <= 'z'));
                stream.Add(plain);
                position++;
            }
            return result.ToString();
        }
        private static List<int> ReadKey(string key)
        {
            List<int> result = new List<int>();
            foreach (char value in key ?? string.Empty)
                if (Alphabet.IsAsciiLetter(value)) result.Add(Alphabet.IndexOf(value));
            if (result.Count == 0) throw new CipherException("密钥请输入英文字母");
            return result;
        }
        public override string ToString() { return Name; }
    }
}
