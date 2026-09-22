using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class MachineDataEncoding
    {
        internal static readonly string[] TypeChoices = {
            "Int8", "UInt8", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
            "Float16", "BFloat16", "Float32", "Float64", ".NET Decimal128", "Q 定点数"
        };
        internal static readonly string[] WidthChoices = { "8", "16", "24", "32", "64", "128", "256" };
        internal static readonly string[] FractionChoices = BuildNumbers(0, 128);

        private sealed class TypeInfo
        {
            internal string Kind;
            internal int Bits, ExponentBits, FractionBits, Bias;
            internal bool Signed;
        }

        internal static string Transform(ToolRequest request)
        {
            TypeInfo type = Type(request);
            int radix = RadixConversion.ReadRange(request.Get("radix"), 10, 2, 36, "数值进制");
            bool little = request.Get("endian") != "大端";
            bool binary = request.Get("bytes") == "二进制位";
            string[] records = request.Mode == ToolMode.Encode ?
                Regex.Split(request.Input.Trim(), @"\s+") :
                request.Input.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (records.Length == 0 || (records.Length == 1 && records[0].Length == 0)) return string.Empty;
            StringBuilder output = new StringBuilder();
            foreach (string raw in records)
            {
                request.ThrowIfCancellationRequested();
                string result = request.Mode == ToolMode.Encode ? Encode(raw, type, radix, little, binary, request) : Decode(raw, type, radix, little, binary, request);
                if (output.Length > 0) output.AppendLine();
                output.Append(result);
            }
            return output.ToString();
        }

        private static TypeInfo Type(ToolRequest request)
        {
            string name = request.Get("type");
            if (name.Length == 0) name = "Int32";
            if (name == "Q 定点数")
            {
                int bits = RadixConversion.ReadRange(request.Get("width"), 32, 8, 256, "位宽");
                if (bits % 8 != 0) throw new CipherException("定点数位宽必须是 8 的倍数");
                int fraction = RadixConversion.ReadRange(request.Get("fraction"), 16, 0, 128, "小数位数");
                if (fraction >= bits) throw new CipherException("定点小数位数必须小于总位宽");
                return new TypeInfo { Kind = "fixed", Bits = bits, FractionBits = fraction, Signed = request.Get("signed") != "无符号" };
            }
            if (name == "Float16") return Float(16, 5, 10, 15);
            if (name == "BFloat16") return Float(16, 8, 7, 127);
            if (name == "Float32") return Float(32, 8, 23, 127);
            if (name == "Float64") return Float(64, 11, 52, 1023);
            if (name == ".NET Decimal128") return new TypeInfo { Kind = "decimal", Bits = 128, Signed = true };
            bool signed = !name.StartsWith("U", StringComparison.Ordinal);
            Match match = Regex.Match(name, @"(8|16|32|64)$");
            if (!match.Success) throw new CipherException("未知数据类型");
            return new TypeInfo { Kind = "integer", Bits = int.Parse(match.Value, CultureInfo.InvariantCulture), Signed = signed };
        }

        private static TypeInfo Float(int bits, int exponent, int fraction, int bias)
        {
            return new TypeInfo { Kind = "float", Bits = bits, ExponentBits = exponent, FractionBits = fraction, Bias = bias, Signed = true };
        }

        private static string Encode(string token, TypeInfo type, int radix, bool little, bool binary, ToolRequest request)
        {
            BigInteger raw;
            if (type.Kind == "float") raw = EncodeFloat(token, type, radix, request);
            else if (type.Kind == "decimal") raw = EncodeDecimal(ExactNumber.Parse(token, radix, request));
            else
            {
                ExactNumber value = ExactNumber.Parse(token, radix, request);
                if (type.Kind == "fixed")
                {
                    BigInteger scaledNumerator = value.Numerator * (BigInteger.One << type.FractionBits);
                    raw = Quantize(scaledNumerator, value.Denominator, request.Get("quantize"));
                }
                else
                {
                    if (value.Denominator != BigInteger.One) throw new CipherException("整数类型不能编码小数");
                    raw = value.Numerator;
                }
                raw = ApplyRange(raw, type.Bits, type.Signed, request.Get("overflow"));
            }
            return FormatBytes(ToBytes(raw, type.Bits, little), binary);
        }

        private static string Decode(string text, TypeInfo type, int radix, bool little, bool binary, ToolRequest request)
        {
            byte[] bytes = ParseBytes(text, type.Bits, little, binary);
            BigInteger raw = FromBytes(bytes);
            if (type.Kind == "float") return DecodeFloat(raw, type, radix, request);
            if (type.Kind == "decimal") return DecodeDecimal(raw, radix, request);
            BigInteger signed = type.Signed && TestBit(raw, type.Bits - 1) ? raw - (BigInteger.One << type.Bits) : raw;
            if (type.Kind == "fixed")
                return RadixConversion.Format(new ExactNumber(signed, BigInteger.One << type.FractionBits, false), radix, 128, "精确", request);
            return ExactNumber.IntegerText(signed, radix, request);
        }

        private static BigInteger Quantize(BigInteger numerator, BigInteger denominator, string mode)
        {
            if (numerator % denominator == 0) return numerator / denominator;
            if (mode.Length == 0 || mode == "报错") throw new CipherException("该值不能由所选定点格式精确表示；请选择截断或最近偶数");
            bool negative = numerator.Sign < 0;
            BigInteger magnitude = BigInteger.Abs(numerator);
            BigInteger value = mode == "最近偶数" ? ExactNumber.RoundEven(magnitude, denominator) : magnitude / denominator;
            return negative ? -value : value;
        }

        private static BigInteger ApplyRange(BigInteger value, int bits, bool signed, string policy)
        {
            BigInteger modulus = BigInteger.One << bits;
            BigInteger minimum = signed ? -(BigInteger.One << (bits - 1)) : BigInteger.Zero;
            BigInteger maximum = signed ? (BigInteger.One << (bits - 1)) - 1 : modulus - 1;
            if (value >= minimum && value <= maximum) return value.Sign < 0 ? value + modulus : value;
            if (policy == "环绕") { value %= modulus; if (value.Sign < 0) value += modulus; return value; }
            if (policy == "饱和") { value = value < minimum ? minimum : maximum; return value.Sign < 0 ? value + modulus : value; }
            throw new CipherException("数值超出所选数据类型范围");
        }

        private static BigInteger EncodeFloat(string token, TypeInfo type, int radix, ToolRequest request)
        {
            bool negativeSpecial = token.StartsWith("-", StringComparison.Ordinal);
            string special = token.TrimStart('+', '-');
            BigInteger sign = negativeSpecial ? BigInteger.One << (type.Bits - 1) : BigInteger.Zero;
            BigInteger exponentMask = (BigInteger.One << type.ExponentBits) - 1;
            if (special.Equals("NaN", StringComparison.OrdinalIgnoreCase)) return sign | (exponentMask << type.FractionBits) | (BigInteger.One << (type.FractionBits - 1));
            if (special.Equals("Infinity", StringComparison.OrdinalIgnoreCase) || special.Equals("Inf", StringComparison.OrdinalIgnoreCase) || special == "∞")
                return sign | (exponentMask << type.FractionBits);
            ExactNumber value = ExactNumber.Parse(token, radix, request);
            sign = value.Negative ? BigInteger.One << (type.Bits - 1) : BigInteger.Zero;
            BigInteger numerator = BigInteger.Abs(value.Numerator);
            if (numerator.IsZero) return sign;
            int exponent = FloorLog2(numerator, value.Denominator);
            int minimum = 1 - type.Bias, maximum = ((1 << type.ExponentBits) - 2) - type.Bias;
            BigInteger fraction;
            int encodedExponent;
            if (exponent < minimum)
            {
                fraction = RoundScaled(numerator, value.Denominator, type.FractionBits - minimum);
                if (fraction.IsZero) return sign;
                if (fraction >= (BigInteger.One << type.FractionBits)) { encodedExponent = 1; fraction = BigInteger.Zero; }
                else encodedExponent = 0;
            }
            else
            {
                BigInteger significand = RoundScaled(numerator, value.Denominator, type.FractionBits - exponent);
                if (significand >= (BigInteger.One << (type.FractionBits + 1))) { significand >>= 1; exponent++; }
                if (exponent > maximum) return sign | (exponentMask << type.FractionBits);
                encodedExponent = exponent + type.Bias;
                fraction = significand - (BigInteger.One << type.FractionBits);
            }
            return sign | ((BigInteger)encodedExponent << type.FractionBits) | fraction;
        }

        private static string DecodeFloat(BigInteger raw, TypeInfo type, int radix, ToolRequest request)
        {
            bool negative = TestBit(raw, type.Bits - 1);
            BigInteger fractionMask = (BigInteger.One << type.FractionBits) - 1;
            BigInteger fraction = raw & fractionMask;
            int exponent = (int)((raw >> type.FractionBits) & ((BigInteger.One << type.ExponentBits) - 1));
            int allOnes = (1 << type.ExponentBits) - 1;
            if (exponent == allOnes) return fraction.IsZero ? (negative ? "-Infinity" : "Infinity") : "NaN";
            if (exponent == 0 && fraction.IsZero) return negative ? "-0" : "0";
            int power;
            BigInteger significand;
            if (exponent == 0) { significand = fraction; power = 1 - type.Bias - type.FractionBits; }
            else { significand = (BigInteger.One << type.FractionBits) | fraction; power = exponent - type.Bias - type.FractionBits; }
            BigInteger numerator = significand, denominator = BigInteger.One;
            if (power >= 0) numerator <<= power; else denominator <<= -power;
            if (negative) numerator = -numerator;
            ExactNumber exact = new ExactNumber(numerator, denominator, false);
            if (radix == 10 && request.Get("display") != "精确") return ShortestFloat(raw, type, exact);
            return RadixConversion.Format(exact, radix,
                RadixConversion.ReadRange(request.Get("precision"), 64, 1, 4096, "显示小数位"),
                "精确", request);
        }

        private static string ShortestFloat(BigInteger raw, TypeInfo type, ExactNumber exact)
        {
            if (type.Bits == 64)
            {
                ulong bits = (ulong)raw;
                return BitConverter.Int64BitsToDouble(unchecked((long)bits)).ToString("R", CultureInfo.InvariantCulture);
            }
            if (type.Bits == 32)
            {
                uint bits = (uint)raw;
                return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0).ToString("R", CultureInfo.InvariantCulture);
            }
            float value = (float)((double)exact.Numerator / (double)exact.Denominator);
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static int FloorLog2(BigInteger numerator, BigInteger denominator)
        {
            int exponent = BitLength(numerator) - BitLength(denominator);
            if (exponent >= 0) { if (numerator < (denominator << exponent)) exponent--; }
            else if ((numerator << -exponent) < denominator) exponent--;
            return exponent;
        }

        private static BigInteger RoundScaled(BigInteger numerator, BigInteger denominator, int shift)
        {
            return shift >= 0 ? ExactNumber.RoundEven(numerator << shift, denominator) : ExactNumber.RoundEven(numerator, denominator << -shift);
        }

        private static int BitLength(BigInteger value)
        {
            byte[] bytes = value.ToByteArray();
            int length = (bytes.Length - 1) * 8;
            byte top = bytes[bytes.Length - 1];
            while (top != 0) { length++; top >>= 1; }
            return length;
        }

        private static BigInteger EncodeDecimal(ExactNumber value)
        {
            BigInteger numerator = BigInteger.Abs(value.Numerator), denominator = value.Denominator;
            int twos = 0, fives = 0;
            while (denominator % 2 == 0) { denominator /= 2; twos++; }
            while (denominator % 5 == 0) { denominator /= 5; fives++; }
            if (denominator != BigInteger.One) throw new CipherException("该值不能由 .NET Decimal 精确表示");
            int scale = Math.Max(twos, fives);
            if (scale > 28) throw new CipherException(".NET Decimal 最多支持 28 位小数缩放");
            numerator *= BigInteger.Pow(5, scale - twos) * BigInteger.Pow(2, scale - fives);
            if (numerator >= (BigInteger.One << 96)) throw new CipherException("数值超出 .NET Decimal 的 96 位有效数范围");
            BigInteger flags = (BigInteger)scale << 16;
            if (value.Negative) flags |= BigInteger.One << 31;
            // Canonical 128-bit layout: flags | high | middle | low.
            BigInteger low = numerator & uint.MaxValue;
            BigInteger middle = (numerator >> 32) & uint.MaxValue;
            BigInteger high = (numerator >> 64) & uint.MaxValue;
            return (flags << 96) | (high << 64) | (middle << 32) | low;
        }

        private static string DecodeDecimal(BigInteger raw, int radix, ToolRequest request)
        {
            uint low = (uint)(raw & uint.MaxValue), middle = (uint)((raw >> 32) & uint.MaxValue), high = (uint)((raw >> 64) & uint.MaxValue), flags = (uint)((raw >> 96) & uint.MaxValue);
            if ((flags & 0x7F00FFFFU) != 0) throw new CipherException("无效的 .NET Decimal 标志位");
            int scale = (int)((flags >> 16) & 0xFF);
            if (scale > 28) throw new CipherException("无效的 .NET Decimal 小数缩放");
            BigInteger coefficient = low | ((BigInteger)middle << 32) | ((BigInteger)high << 64);
            if ((flags & 0x80000000U) != 0) coefficient = -coefficient;
            return RadixConversion.Format(new ExactNumber(coefficient, BigInteger.Pow(10, scale), coefficient.IsZero && (flags & 0x80000000U) != 0), radix, 128, "精确", request);
        }

        private static byte[] ToBytes(BigInteger raw, int bits, bool little)
        {
            int count = bits / 8;
            byte[] bytes = new byte[count];
            for (int i = 0; i < count; i++) { bytes[i] = (byte)(raw & 255); raw >>= 8; }
            if (!little) Array.Reverse(bytes);
            return bytes;
        }

        private static BigInteger FromBytes(byte[] bytes)
        {
            BigInteger result = BigInteger.Zero;
            for (int i = bytes.Length - 1; i >= 0; i--) result = (result << 8) | bytes[i];
            return result;
        }

        private static string FormatBytes(byte[] bytes, bool binary)
        {
            string[] values = new string[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) values[i] = binary ? Convert.ToString(bytes[i], 2).PadLeft(8, '0') : bytes[i].ToString("X2", CultureInfo.InvariantCulture);
            return string.Join(" ", values);
        }

        private static byte[] ParseBytes(string text, int bits, bool little, bool binary)
        {
            string compact = Regex.Replace(text, @"[\s_:-]+", string.Empty);
            if (binary)
            {
                if (!Regex.IsMatch(compact, "^[01]+$") || compact.Length != bits) throw new CipherException("二进制输入必须恰好包含 " + bits + " 位");
            }
            else
            {
                if (compact.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) compact = compact.Substring(2);
                if (!Regex.IsMatch(compact, "^[0-9A-Fa-f]+$") || compact.Length != bits / 4) throw new CipherException("十六进制输入必须恰好包含 " + (bits / 4) + " 位");
            }
            byte[] bytes = new byte[bits / 8];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = binary ? Convert.ToByte(compact.Substring(i * 8, 8), 2) : byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (!little) Array.Reverse(bytes);
            return bytes;
        }

        private static bool TestBit(BigInteger value, int bit) { return (value & (BigInteger.One << bit)) != 0; }
        private static string[] BuildNumbers(int min, int max) { string[] result = new string[max - min + 1]; for (int i = 0; i < result.Length; i++) result[i] = (i + min).ToString(CultureInfo.InvariantCulture); return result; }
    }
}
