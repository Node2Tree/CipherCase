using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class RadixConversion
    {
        internal static string[] BaseChoices()
        {
            string[] choices = new string[35];
            for (int i = 0; i < choices.Length; i++) choices[i] = (i + 2).ToString(CultureInfo.InvariantCulture);
            return choices;
        }

        internal static string Convert(ToolRequest request)
        {
            int source = ReadRange(request.Get("from"), 10, 2, 36, "源进制");
            int target = ReadRange(request.Get("to"), 16, 2, 36, "目标进制");
            int precision = ReadRange(request.Get("precision"), 64, 1, 4096, "小数位上限");
            string rounding = request.Get("rounding");
            if (rounding.Length == 0) rounding = "精确";
            if (rounding != "精确" && rounding != "截断" && rounding != "最近偶数") throw new CipherException("未知舍入方式");
            if (request.Input.Length > 100000) throw new CipherException("进制换算输入最多 10 万字符");
            return Regex.Replace(request.Input, @"\S+", delegate(Match match)
            {
                request.ThrowIfCancellationRequested();
                return Format(ExactNumber.Parse(match.Value, source, request), target, precision, rounding, request);
            });
        }

        internal static string Format(ExactNumber value, int radix, int precision, string rounding, ToolRequest request)
        {
            BigInteger magnitude = BigInteger.Abs(value.Numerator), remainder;
            BigInteger whole = BigInteger.DivRem(magnitude, value.Denominator, out remainder);
            string sign = value.Negative ? "-" : string.Empty;
            string head = sign + ExactNumber.IntegerText(whole, radix, request);
            if (remainder.IsZero) return head;
            StringBuilder fraction = new StringBuilder();
            Dictionary<BigInteger, int> seen = new Dictionary<BigInteger, int>();
            while (!remainder.IsZero && fraction.Length < precision)
            {
                request.ThrowIfCancellationRequested();
                int cycle;
                if (rounding == "精确" && seen.TryGetValue(remainder, out cycle))
                    return head + "." + fraction.ToString(0, cycle) + "(" + fraction.ToString(cycle, fraction.Length - cycle) + ")";
                if (rounding == "精确") seen[remainder] = fraction.Length;
                BigInteger digit = BigInteger.DivRem(remainder * radix, value.Denominator, out remainder);
                fraction.Append(ExactNumber.Digits[(int)digit]);
            }
            if (remainder.IsZero) return head + "." + fraction;
            if (rounding == "精确")
            {
                int cycle;
                if (seen.TryGetValue(remainder, out cycle))
                    return head + "." + fraction.ToString(0, cycle) + "(" + fraction.ToString(cycle, fraction.Length - cycle) + ")";
                return ExactNumber.IntegerText(value.Numerator, radix, request) + "/" + ExactNumber.IntegerText(value.Denominator, radix, request);
            }
            BigInteger scale = BigInteger.Pow(radix, precision);
            BigInteger scaled = rounding == "最近偶数" ? ExactNumber.RoundEven(magnitude * scale, value.Denominator) : magnitude * scale / value.Denominator;
            string digits = ExactNumber.IntegerText(scaled, radix, request).PadLeft(precision + 1, '0');
            return "≈" + sign + digits.Substring(0, digits.Length - precision) + "." + digits.Substring(digits.Length - precision);
        }

        internal static int ReadRange(string text, int fallback, int min, int max, string label)
        {
            if (text.Length == 0) return fallback;
            int value;
            if (!int.TryParse(text, out value) || value < min || value > max)
                throw new CipherException(label + "必须在 " + min + "–" + max + " 之间");
            return value;
        }
    }
}
