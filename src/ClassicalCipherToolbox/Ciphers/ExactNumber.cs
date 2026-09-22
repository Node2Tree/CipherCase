using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    // Exact rational arithmetic shared by positional notation and machine encodings.
    internal sealed class ExactNumber
    {
        internal readonly BigInteger Numerator;
        internal readonly BigInteger Denominator;
        internal readonly bool NegativeZero;
        internal bool Negative { get { return Numerator.Sign < 0 || NegativeZero; } }
        internal const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        internal ExactNumber(BigInteger numerator, BigInteger denominator, bool negativeZero)
        {
            if (denominator <= 0) throw new CipherException("分母必须大于零");
            BigInteger divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            Numerator = numerator / divisor;
            Denominator = denominator / divisor;
            NegativeZero = numerator.IsZero && negativeZero;
        }

        internal static ExactNumber Parse(string input, int radix, ToolRequest request)
        {
            if (input.Length > 4096) throw new CipherException("每个数最多 4096 个字符");
            string text = input.ToUpperInvariant();
            bool negative = text.StartsWith("-", StringComparison.Ordinal);
            if (negative || text.StartsWith("+", StringComparison.Ordinal)) text = text.Substring(1);
            if (text.Length == 0) throw new CipherException("正负号后需要数值");
            if (text.IndexOf('≈') >= 0) throw new CipherException("≈ 标记的是近似结果；请选择精确输出，或手动确认数值后移除该标记");
            string prefix = radix == 16 ? "0X" : radix == 2 ? "0B" : radix == 8 ? "0O" : string.Empty;
            if (prefix.Length > 0 && text.StartsWith(prefix, StringComparison.Ordinal)) text = text.Substring(2);
            int exponent = 0;
            if (radix == 10 && text.IndexOf('E') >= 0)
            {
                int position = text.IndexOf('E');
                string exp = text.Substring(position + 1);
                if (!Regex.IsMatch(exp, @"^[+-]?[0-9]+$") || !int.TryParse(exp, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent) || Math.Abs((long)exponent) > 4096)
                    throw new CipherException("十进制指数必须是 -4096 至 4096 的整数");
                text = text.Substring(0, position);
            }
            BigInteger numerator, denominator;
            int slash = text.IndexOf('/');
            if (slash >= 0)
            {
                numerator = ReadDigits(text.Substring(0, slash), radix, request);
                denominator = ReadDigits(text.Substring(slash + 1), radix, request);
                if (denominator.IsZero) throw new CipherException("分母不能为零");
            }
            else
            {
                Match match = Regex.Match(text, @"^([0-9A-Z_]*)(?:\.([0-9A-Z_]*)(?:\(([0-9A-Z_]+)\))?)?$");
                if (!match.Success || match.Groups[1].Length + match.Groups[2].Length + match.Groups[3].Length == 0)
                    throw new CipherException("请输入整数、小数、分数 a/b 或循环小数 0.(3)");
                string whole = match.Groups[1].Value;
                string fraction = CleanDigits(match.Groups[2].Value, radix);
                string repeating = CleanDigits(match.Groups[3].Value, radix);
                numerator = whole.Length == 0 ? BigInteger.Zero : ReadDigits(whole, radix, request);
                denominator = BigInteger.Pow(radix, fraction.Length);
                numerator *= denominator;
                if (fraction.Length > 0) numerator += ReadDigits(fraction, radix, request);
                if (repeating.Length > 0)
                {
                    BigInteger cycle = BigInteger.Pow(radix, repeating.Length) - 1;
                    numerator = numerator * cycle + ReadDigits(repeating, radix, request);
                    denominator *= cycle;
                }
            }
            if (exponent > 0) numerator *= BigInteger.Pow(10, exponent);
            if (exponent < 0) denominator *= BigInteger.Pow(10, -exponent);
            return new ExactNumber(negative ? -numerator : numerator, denominator, negative);
        }

        private static string CleanDigits(string text, int radix)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '_')
                {
                    if (i == 0 || i + 1 == text.Length || text[i - 1] == '_' || text[i + 1] == '_')
                        throw new CipherException("下划线只能位于两个数字之间");
                    continue;
                }
                int digit = Digits.IndexOf(text[i]);
                if (digit < 0 || digit >= radix) throw new CipherException("字符 " + text[i] + " 不属于 " + radix + " 进制");
            }
            return text.Replace("_", string.Empty);
        }

        internal static BigInteger ReadDigits(string text, int radix, ToolRequest request)
        {
            text = CleanDigits(text, radix);
            if (text.Length == 0) throw new CipherException("缺少数字");
            BigInteger value = BigInteger.Zero;
            for (int i = 0; i < text.Length; i++)
            {
                if ((i & 255) == 0) request.ThrowIfCancellationRequested();
                value = value * radix + Digits.IndexOf(text[i]);
            }
            return value;
        }

        internal static string IntegerText(BigInteger value, int radix, ToolRequest request)
        {
            if (value.IsZero) return "0";
            bool negative = value.Sign < 0;
            value = BigInteger.Abs(value);
            StringBuilder result = new StringBuilder();
            while (!value.IsZero)
            {
                if ((result.Length & 255) == 0) request.ThrowIfCancellationRequested();
                BigInteger remainder;
                value = BigInteger.DivRem(value, radix, out remainder);
                result.Append(Digits[(int)remainder]);
            }
            if (negative) result.Append('-');
            char[] characters = result.ToString().ToCharArray();
            Array.Reverse(characters);
            return new string(characters);
        }

        // Positive rational to integer, ties to even. All floating/fixed encodings use this path.
        internal static BigInteger RoundEven(BigInteger numerator, BigInteger denominator)
        {
            BigInteger remainder;
            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out remainder);
            int compare = (remainder * 2).CompareTo(denominator);
            if (compare > 0 || (compare == 0 && !quotient.IsEven)) quotient++;
            return quotient;
        }
    }
}
