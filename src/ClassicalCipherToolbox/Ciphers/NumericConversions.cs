using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class GrayCode
    {
        internal static string Transform(string input, bool decode)
        {
            string text = input ?? string.Empty;
            if (text.Length > 1000000) throw new CipherException("格雷码输入最多 100 万字符");
            return Regex.Replace(text, @"\S+", delegate(Match match)
            {
                string bits = match.Value;
                StringBuilder result = new StringBuilder(bits.Length);
                char previous = '0';
                foreach (char bit in bits)
                {
                    if (bit != '0' && bit != '1') throw new CipherException("格雷码仅接受 0、1；空白分隔的每组独立转换");
                    char converted = bit == previous ? '0' : '1';
                    result.Append(converted);
                    previous = decode ? converted : bit;
                }
                return result.ToString();
            });
        }
    }

}
