using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class TransferEncoding
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        internal static readonly string[] CharsetChoices = {
            "UTF-8", "UTF-16LE", "UTF-16BE", "UTF-32LE", "UTF-32BE",
            "GB18030", "GBK / CP936", "GB2312 / EUC-CN", "HZ-GB-2312", "ISO-2022-CN / CP50227",
            "Big5 / CP950", "Mac 简体中文", "Mac 繁体中文", "CNS 11643", "TCA 台湾",
            "Big5 ETen", "IBM5550 台湾", "TeleText 台湾", "Wang 台湾", "Shift_JIS"
        };

        internal static string Base64(string input, bool decode) { try { return decode ? Encoding.UTF8.GetString(Convert.FromBase64String(Compact(input))) : Convert.ToBase64String(Encoding.UTF8.GetBytes(input ?? string.Empty)); } catch { throw new CipherException("Base64 格式无效"); } }
        internal static string Base64Url(string input, bool decode) { if (!decode) return Convert.ToBase64String(Encoding.UTF8.GetBytes(input ?? string.Empty)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); string value = Compact(input).Replace('-', '+').Replace('_', '/'); value += new string('=', (4 - value.Length % 4) % 4); try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); } catch { throw new CipherException("Base64URL 格式无效"); } }
        internal static string Hex(string input, bool decode) { if (!decode) return BytesToHex(Encoding.UTF8.GetBytes(input ?? string.Empty)); try { return Encoding.UTF8.GetString(HexToBytes(input)); } catch { throw new CipherException("十六进制格式无效"); } }
        internal static string Binary(string input, bool decode)
        {
            if (!decode) { byte[] bytes = Encoding.UTF8.GetBytes(input ?? string.Empty); StringBuilder result = new StringBuilder(); for (int i = 0; i < bytes.Length; i++) { if (i > 0) result.Append(' '); result.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0')); } return result.ToString(); }
            string bits = CompactBits(input); if (bits.Length == 0 || bits.Length % 8 != 0) throw new CipherException("二进制位数必须是 8 的倍数"); byte[] output = new byte[bits.Length / 8]; try { for (int i = 0; i < output.Length; i++) output[i] = Convert.ToByte(bits.Substring(i * 8, 8), 2); } catch { throw new CipherException("二进制格式无效"); } return Encoding.UTF8.GetString(output);
        }
        internal static string Base32(string input, bool decode) { try { return decode ? Encoding.UTF8.GetString(Base32Decode(input)) : Base32Encode(Encoding.UTF8.GetBytes(input ?? string.Empty)); } catch { throw new CipherException("Base32 格式无效"); } }
        internal static string Base58(string input, bool decode) { try { return decode ? Encoding.UTF8.GetString(Base58Decode(input)) : Base58Encode(Encoding.UTF8.GetBytes(input ?? string.Empty)); } catch { throw new CipherException("Base58 格式无效"); } }
        internal static string Ascii85(string input, bool decode) { try { return decode ? Encoding.UTF8.GetString(Ascii85Decode(input)) : Ascii85Encode(Encoding.UTF8.GetBytes(input ?? string.Empty)); } catch { throw new CipherException("ASCII85 格式无效"); } }
        internal static string Url(string input, bool decode) { try { return decode ? Uri.UnescapeDataString((input ?? string.Empty).Replace("+", " ")) : Uri.EscapeDataString(input ?? string.Empty); } catch { throw new CipherException("URL 百分号格式无效"); } }
        internal static string Html(string input, bool decode) { return decode ? WebUtility.HtmlDecode(input ?? string.Empty) : WebUtility.HtmlEncode(input ?? string.Empty); }
        internal static string Punycode(string input, bool decode) { try { IdnMapping idn = new IdnMapping(); string[] parts = (input ?? string.Empty).Split('.'); for (int i = 0; i < parts.Length; i++) parts[i] = decode ? idn.GetUnicode(parts[i]) : idn.GetAscii(parts[i]); return string.Join(".", parts); } catch { throw new CipherException("Punycode 域名格式无效"); } }
        internal static string UnicodeEscape(string input, bool decode)
        {
            if (!decode) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) { if (c >= 32 && c <= 126 && c != '\\') result.Append(c); else result.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)); } return result.ToString(); }
            StringBuilder output = new StringBuilder(); string value = input ?? string.Empty; for (int i = 0; i < value.Length; i++) { if (value[i] == '\\' && i + 5 < value.Length && value[i + 1] == 'u') { int code; if (!int.TryParse(value.Substring(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)) throw new CipherException("Unicode 转义格式无效"); output.Append((char)code); i += 5; } else output.Append(value[i]); } return output.ToString();
        }
        internal static string QuotedPrintable(string input, bool decode)
        {
            if (!decode) { byte[] data = Encoding.UTF8.GetBytes(input ?? string.Empty); StringBuilder result = new StringBuilder(); foreach (byte b in data) { if ((b >= 33 && b <= 60) || (b >= 62 && b <= 126) || b == 32 || b == 9 || b == 10 || b == 13) result.Append((char)b); else result.Append('=').Append(b.ToString("X2", CultureInfo.InvariantCulture)); } return result.ToString(); }
            MemoryStream bytes = new MemoryStream(); string source = (input ?? string.Empty).Replace("=\r\n", string.Empty).Replace("=\n", string.Empty); for (int i = 0; i < source.Length; i++) { if (source[i] == '=' && i + 2 < source.Length) { byte b; if (!byte.TryParse(source.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b)) throw new CipherException("Quoted-Printable 格式无效"); bytes.WriteByte(b); i += 2; } else { byte[] raw = Encoding.UTF8.GetBytes(new[] { source[i] }); bytes.Write(raw, 0, raw.Length); } } return Encoding.UTF8.GetString(bytes.ToArray());
        }
        internal static string CharsetBytes(string input, string charset, bool decode)
        {
            Encoding encoding; try { encoding = CharsetEncoding(charset); } catch { throw new CipherException("不支持该字符集"); }
            try
            {
                if (decode) { byte[] bytes = HexToBytes(input); if (IsGb2312(charset)) ValidateGb2312(bytes); return encoding.GetString(bytes); }
                byte[] encoded = encoding.GetBytes(input ?? string.Empty); if (IsGb2312(charset)) ValidateGb2312(encoded); return BytesToHex(encoded);
            }
            catch { throw new CipherException("文本或字节不符合所选字符集"); }
        }
        internal static string CharsetText(byte[] bytes, string charset) { return CharsetBytes(BytesToHex(bytes ?? new byte[0]), charset, true); }
        internal static Encoding CharsetEncoding(string value)
        {
            string name = (value ?? string.Empty).Trim(), key = name.ToUpperInvariant().Replace('_', '-');
            if (key.Length == 0 || key == "UTF8" || key == "UTF-8") return new UTF8Encoding(false, true);
            if (key == "UTF-16" || key == "UTF-16LE" || key == "UNICODE") return new UnicodeEncoding(false, false, true);
            if (key == "UTF-16BE" || key == "UNICODEFFFE") return new UnicodeEncoding(true, false, true);
            if (key == "UTF-32" || key == "UTF-32LE") return new UTF32Encoding(false, false, true);
            if (key == "UTF-32BE") return new UTF32Encoding(true, false, true);
            if (key == "GBK" || key == "CP936" || key.StartsWith("GBK /")) return StrictCodePage(936);
            if (key == "GB2312" || key == "EUC-CN" || key.StartsWith("GB2312 /")) return StrictCodePage(936);
            if (key == "GB18030") return StrictCodePage(54936);
            if (key == "HZ-GB-2312" || key == "HZ-GB2312") return StrictCodePage(52936);
            if (key == "ISO-2022-CN" || key == "CP50227" || key == "X-CP50227" || key.StartsWith("ISO-2022-CN /")) return StrictCodePage(50227);
            if (key == "BIG5" || key == "CP950" || key.StartsWith("BIG5 /")) return StrictCodePage(950);
            if (key == "MAC 简体中文" || key == "X-MAC-CHINESESIMP") return StrictCodePage(10008);
            if (key == "MAC 繁体中文" || key == "X-MAC-CHINESETRAD") return StrictCodePage(10002);
            if (key == "CNS 11643" || key == "X-CHINESE-CNS") return StrictCodePage(20000);
            if (key == "TCA 台湾" || key == "X-CP20001") return StrictCodePage(20001);
            if (key == "BIG5 ETEN" || key == "X-CHINESE-ETEN") return StrictCodePage(20002);
            if (key == "IBM5550 台湾" || key == "X-CP20003") return StrictCodePage(20003);
            if (key == "TELETEXT 台湾" || key == "X-CP20004") return StrictCodePage(20004);
            if (key == "WANG 台湾" || key == "X-CP20005") return StrictCodePage(20005);
            return Encoding.GetEncoding(name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
        private static Encoding StrictCodePage(int codePage) { return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback); }
        private static bool IsGb2312(string value) { string key = (value ?? string.Empty).Trim().ToUpperInvariant().Replace('_', '-'); return key == "GB2312" || key == "EUC-CN" || key.StartsWith("GB2312 /"); }
        private static void ValidateGb2312(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++) { if (bytes[i] <= 0x7F) continue; if (i + 1 >= bytes.Length || bytes[i] < 0xA1 || bytes[i] > 0xF7 || bytes[i + 1] < 0xA1 || bytes[i + 1] > 0xFE) throw new EncoderFallbackException(); i++; }
        }
        internal static string BytesToHex(byte[] bytes) { StringBuilder result = new StringBuilder(bytes.Length * 2); foreach (byte b in bytes) result.Append(b.ToString("X2", CultureInfo.InvariantCulture)); return result.ToString(); }
        internal static byte[] HexToBytes(string input) { string value = CompactHex(input); if (value.Length == 0 || value.Length % 2 != 0) throw new FormatException(); byte[] bytes = new byte[value.Length / 2]; for (int i = 0; i < bytes.Length; i++) bytes[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture); return bytes; }
        private static string Compact(string input) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) if (!char.IsWhiteSpace(c)) result.Append(c); return result.ToString(); }
        private static string CompactHex(string input) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) if (Uri.IsHexDigit(c)) result.Append(c); return result.ToString(); }
        private static string CompactBits(string input) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) if (c == '0' || c == '1') result.Append(c); else if (!char.IsWhiteSpace(c) && c != '-' && c != '_') throw new FormatException(); return result.ToString(); }
        private static string Base32Encode(byte[] data) { StringBuilder result = new StringBuilder(); int buffer = 0, bits = 0; foreach (byte b in data) { buffer = (buffer << 8) | b; bits += 8; while (bits >= 5) { result.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; } } if (bits > 0) result.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]); while (result.Length % 8 != 0) result.Append('='); return result.ToString(); }
        private static byte[] Base32Decode(string input) { string value = Compact(input).TrimEnd('=').ToUpperInvariant(); List<byte> result = new List<byte>(); int buffer = 0, bits = 0; foreach (char c in value) { int index = Base32Alphabet.IndexOf(c); if (index < 0) throw new FormatException(); buffer = (buffer << 5) | index; bits += 5; if (bits >= 8) { result.Add((byte)((buffer >> (bits - 8)) & 255)); bits -= 8; } } return result.ToArray(); }
        private static string Base58Encode(byte[] data) { if (data.Length == 0) return string.Empty; List<byte> digits = new List<byte> { 0 }; foreach (byte b in data) { int carry = b; for (int i = 0; i < digits.Count; i++) { carry += digits[i] << 8; digits[i] = (byte)(carry % 58); carry /= 58; } while (carry > 0) { digits.Add((byte)(carry % 58)); carry /= 58; } } StringBuilder result = new StringBuilder(); foreach (byte b in data) { if (b == 0) result.Append('1'); else break; } for (int i = digits.Count - 1; i >= 0; i--) result.Append(Base58Alphabet[digits[i]]); return result.ToString(); }
        private static byte[] Base58Decode(string input) { string value = Compact(input); if (value.Length == 0) return new byte[0]; List<byte> bytes = new List<byte> { 0 }; foreach (char c in value) { int carry = Base58Alphabet.IndexOf(c); if (carry < 0) throw new FormatException(); for (int i = 0; i < bytes.Count; i++) { carry += bytes[i] * 58; bytes[i] = (byte)(carry & 255); carry >>= 8; } while (carry > 0) { bytes.Add((byte)(carry & 255)); carry >>= 8; } } List<byte> result = new List<byte>(); foreach (char c in value) { if (c == '1') result.Add(0); else break; } for (int i = bytes.Count - 1; i >= 0; i--) result.Add(bytes[i]); return result.ToArray(); }
        private static string Ascii85Encode(byte[] data) { StringBuilder result = new StringBuilder("<~"); for (int offset = 0; offset < data.Length; offset += 4) { int count = Math.Min(4, data.Length - offset); uint value = 0; for (int i = 0; i < 4; i++) value = (value << 8) | (uint)(i < count ? data[offset + i] : 0); if (count == 4 && value == 0) result.Append('z'); else { char[] block = new char[5]; for (int i = 4; i >= 0; i--) { block[i] = (char)(value % 85 + 33); value /= 85; } result.Append(block, 0, count + 1); } } return result.Append("~>").ToString(); }
        private static byte[] Ascii85Decode(string input) { string value = Compact(input); if (value.StartsWith("<~")) value = value.Substring(2); if (value.EndsWith("~>")) value = value.Substring(0, value.Length - 2); List<byte> result = new List<byte>(); List<int> block = new List<int>(); foreach (char c in value) { if (c == 'z' && block.Count == 0) { result.AddRange(new byte[4]); continue; } if (c < '!' || c > 'u') throw new FormatException(); block.Add(c - 33); if (block.Count == 5) { WriteAscii85(block, 4, result); block.Clear(); } } if (block.Count == 1) throw new FormatException(); if (block.Count > 1) { int bytes = block.Count - 1; while (block.Count < 5) block.Add(84); WriteAscii85(block, bytes, result); } return result.ToArray(); }
        private static void WriteAscii85(List<int> block, int count, List<byte> output) { ulong value = 0; foreach (int digit in block) value = value * 85 + (uint)digit; for (int i = 0; i < count; i++) output.Add((byte)(value >> (24 - i * 8))); }
    }

    internal static class AutoDecoder
    {
        private sealed class Candidate { internal string Name; internal string Text; internal double Score; }
        private delegate string Decoder(string input);
        internal static string Decode(string input)
        {
            List<KeyValuePair<string, Decoder>> decoders = new List<KeyValuePair<string, Decoder>> {
                Pair("Base64", delegate(string s){return TransferEncoding.Base64(s,true);}), Pair("Base64URL", delegate(string s){return TransferEncoding.Base64Url(s,true);}), Pair("Base32", delegate(string s){return TransferEncoding.Base32(s,true);}), Pair("Hex/UTF-8", delegate(string s){return TransferEncoding.Hex(s,true);}), Pair("二进制/UTF-8", delegate(string s){return TransferEncoding.Binary(s,true);}), Pair("URL", delegate(string s){return TransferEncoding.Url(s,true);}), Pair("HTML 实体", delegate(string s){return TransferEncoding.Html(s,true);}), Pair("Unicode 转义", delegate(string s){return TransferEncoding.UnicodeEscape(s,true);}), Pair("Quoted-Printable", delegate(string s){return TransferEncoding.QuotedPrintable(s,true);}), Pair("Base58", delegate(string s){return TransferEncoding.Base58(s,true);}), Pair("ASCII85", delegate(string s){return TransferEncoding.Ascii85(s,true);}) };
            string[] chineseCharsets = { TransferEncoding.CharsetChoices[6], TransferEncoding.CharsetChoices[5], TransferEncoding.CharsetChoices[7], TransferEncoding.CharsetChoices[8], TransferEncoding.CharsetChoices[9], TransferEncoding.CharsetChoices[10], TransferEncoding.CharsetChoices[11], TransferEncoding.CharsetChoices[12], TransferEncoding.CharsetChoices[13], TransferEncoding.CharsetChoices[14], TransferEncoding.CharsetChoices[15], TransferEncoding.CharsetChoices[16], TransferEncoding.CharsetChoices[17], TransferEncoding.CharsetChoices[18] };
            foreach (string value in chineseCharsets) { string charset = value; decoders.Add(Pair("Hex/" + charset, delegate(string s) { return DecodeCharsetHex(s, charset); })); }
            List<Candidate> values = new List<Candidate>(); foreach (KeyValuePair<string, Decoder> decoder in decoders) TryAdd(values, decoder.Key, input, decoder.Value); List<Candidate> first = new List<Candidate>(values); foreach (Candidate outer in first) foreach (KeyValuePair<string, Decoder> decoder in decoders) TryAdd(values, outer.Name + " → " + decoder.Key, outer.Text, decoder.Value); values.Sort(delegate(Candidate a, Candidate b) { return b.Score.CompareTo(a.Score); }); if (values.Count == 0) return "没有发现可直接还原的常见编码"; StringBuilder output = new StringBuilder(); for (int i = 0; i < Math.Min(20, values.Count); i++) { if (i > 0) output.Append("\r\n\r\n"); output.Append('#').Append(i + 1).Append("  类型 ").Append(values[i].Name).Append("  评分 ").Append(values[i].Score.ToString("0.0", CultureInfo.InvariantCulture)).Append("\r\n明文：").Append(values[i].Text); } return output.ToString();
        }
        private static string DecodeCharsetHex(string input, string charset)
        {
            int digits = 0; foreach (char c in input ?? string.Empty) { if (Uri.IsHexDigit(c)) digits++; else if (!char.IsWhiteSpace(c) && c != '-' && c != ':' && c != ',') throw new FormatException(); } if (digits < 2 || digits % 2 != 0) throw new FormatException(); return TransferEncoding.CharsetBytes(input, charset, true);
        }
        private static KeyValuePair<string, Decoder> Pair(string name, Decoder decoder) { return new KeyValuePair<string, Decoder>(name, decoder); }
        private static void TryAdd(List<Candidate> values, string name, string input, Decoder decoder) { try { string text = decoder(input); if (string.IsNullOrEmpty(text) || text == input || Bad(text)) return; foreach (Candidate old in values) if (old.Text == text) return; values.Add(new Candidate { Name = name, Text = text, Score = Readable(text) }); } catch { } }
        private static bool Bad(string text) { int control = 0; foreach (char c in text) if (char.IsControl(c) && !char.IsWhiteSpace(c)) control++; return control > Math.Max(0, text.Length / 20); }
        private static double Readable(string text) { int printable = 0, letters = 0, spaces = 0, chinese = 0; StringBuilder latin = new StringBuilder(); foreach (char raw in text) { if (!char.IsControl(raw) || char.IsWhiteSpace(raw)) printable++; if (char.IsWhiteSpace(raw)) spaces++; if (raw >= '\u3400' && raw <= '\u9FFF') chinese++; char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') { letters++; latin.Append(c); } } double score = 45 * printable / Math.Max(1.0, text.Length) + Math.Min(18, spaces * 120.0 / Math.Max(1, text.Length)) + Math.Min(25, (letters + chinese) * 70.0 / Math.Max(1, text.Length)); if (latin.Length >= 8) score += Math.Max(0, Math.Min(10, (LanguageModels.LanguageMatchScore(latin.ToString(), "EN", "COSINE") - .65) * 30)); return Math.Min(98, score); }
    }

    internal static class BrailleCode
    {
        private const string Letters = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string[] Cells = { "⠁","⠃","⠉","⠙","⠑","⠋","⠛","⠓","⠊","⠚","⠅","⠇","⠍","⠝","⠕","⠏","⠟","⠗","⠎","⠞","⠥","⠧","⠺","⠭","⠽","⠵" };
        internal static string Transform(string input, bool decode)
        {
            if (!decode) { StringBuilder result = new StringBuilder(); bool number = false; foreach (char raw in input ?? string.Empty) { char c = char.ToLowerInvariant(raw); int p = Letters.IndexOf(c); if (p >= 0) { if (char.IsUpper(raw)) result.Append('⠠'); result.Append(Cells[p]); number = false; } else if (char.IsDigit(c)) { if (!number) result.Append('⠼'); result.Append(Cells[c == '0' ? 9 : c - '1']); number = true; } else { result.Append(raw); number = false; } } return result.ToString(); }
            StringBuilder output = new StringBuilder(); bool capital = false, numberMode = false; foreach (char c in input ?? string.Empty) { if (c == '⠠') { capital = true; continue; } if (c == '⠼') { numberMode = true; continue; } int p = Array.IndexOf(Cells, c.ToString()); if (p >= 0) { char value = numberMode && p < 10 ? (p == 9 ? '0' : (char)('1' + p)) : Letters[p]; output.Append(capital ? char.ToUpperInvariant(value) : value); capital = false; } else { output.Append(c); if (char.IsWhiteSpace(c)) numberMode = false; } } return output.ToString();
        }
    }

    internal static class BaudotCode
    {
        private const string Letters = "\0E\nA SIU\rDRJNFCKTZLWHYPQOBG\0MXV\0";
        private const string Figures = "\03\n- '87\r$4\0,!:(5\")2#6019?&\0./;\0";
        internal static string Transform(string input, bool decode)
        {
            if (!decode) { StringBuilder result = new StringBuilder(); bool figures = false; foreach (char raw in (input ?? string.Empty).ToUpperInvariant()) { int li = Letters.IndexOf(raw), fi = Figures.IndexOf(raw); bool needFigures = li < 0 && fi >= 0; int code = needFigures ? fi : li; if (code < 0) continue; if (needFigures != figures) { if (result.Length > 0) result.Append(' '); result.Append(needFigures ? "11011" : "11111"); figures = needFigures; } if (result.Length > 0) result.Append(' '); result.Append(Convert.ToString(code, 2).PadLeft(5, '0')); } return result.ToString(); }
            string bits = OnlyBits(input); if (bits.Length % 5 != 0) throw new CipherException("博多码位数必须是 5 的倍数"); StringBuilder output = new StringBuilder(); bool figureMode = false; for (int i = 0; i < bits.Length; i += 5) { int code = Convert.ToInt32(bits.Substring(i, 5), 2); if (code == 27) { figureMode = true; continue; } if (code == 31) { figureMode = false; continue; } char c = (figureMode ? Figures : Letters)[code]; if (c != '\0') output.Append(c); } return output.ToString();
        }
        private static string OnlyBits(string input) { StringBuilder result = new StringBuilder(); foreach (char c in input ?? string.Empty) if (c == '0' || c == '1') result.Append(c); else if (!char.IsWhiteSpace(c) && c != '-') throw new CipherException("博多码只能包含 0、1 和分隔符"); return result.ToString(); }
    }

    internal static class ChineseTelegraphCode
    {
        private static readonly Dictionary<string, string> ToCode = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> FromCode = new Dictionary<string, string>();
        private static bool loaded;
        internal static string Transform(string input, bool decode)
        {
            Load(); if (!decode) { List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) { string code; result.Add(ToCode.TryGetValue(unit, out code) ? code : unit); } return string.Join(" ", result.ToArray()); }
            StringBuilder output = new StringBuilder(); string[] parts = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', '-', ',' }, StringSplitOptions.RemoveEmptyEntries); foreach (string part in parts) { string value; output.Append(FromCode.TryGetValue(part.PadLeft(4, '0'), out value) ? value : "[" + part + "]"); } return output.ToString();
        }
        internal static bool TryGetCode(string character, out string code) { Load(); return ToCode.TryGetValue(character ?? string.Empty, out code); }
        internal static bool TryGetCharacter(string code, out string character) { Load(); return FromCode.TryGetValue((code ?? string.Empty).PadLeft(4, '0'), out character); }
        private static void Load()
        {
            if (loaded) return; loaded = true; Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicalCipherToolbox.Analysis.ChineseTelegraph"); if (resource == null) throw new CipherException("中文电报码表未嵌入"); using (resource) using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress)) using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8)) { string line; while ((line = reader.ReadLine()) != null) { int at = line.LastIndexOf('='); if (at <= 0) continue; string character = line.Substring(0, at), code = line.Substring(at + 1); ToCode[character] = code; if (!FromCode.ContainsKey(code)) FromCode[code] = character; } }
        }
    }

    internal static class ChineseInputCode
    {
        internal static readonly string[] SchemeChoices = {
            "汉语拼音", "汉语拼音（数字声调）", "汉语拼音（声调符号）", "拼音首字母", "注音", "粤拼",
            "自然码双拼", "智能ABC双拼", "小鹤双拼", "微软双拼", "拼音加加双拼", "四通双拼",
            "仓颉", "速成", "五笔86", "五笔98", "郑码", "二笔", "表形码", "行列30", "大易四码", "嘸蝦米",
            "笔顺五码", "小鹤音形", "自然码音形", "吴语拼音", "苏州吴语", "白话字 POJ", "台语注音", "台罗 TLPA",
            "四角号码", "总笔画", "部首余笔", "康熙索引"
        };

        private sealed class Entry
        {
            internal string Character, Mandarin, Cantonese, Cangjie, FourCorner, Strokes, RadicalStroke, Definition;
            internal string Simplified, Traditional, Semantic, Compatibility, HanyuPinlu, HanyuPinyin, CheungBauer, Phonetic, KangXi;
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static readonly Dictionary<string, Dictionary<string, List<string>>> Reverse = new Dictionary<string, Dictionary<string, List<string>>>();
        private const string CommonCharacters = "的一是在不了有人我他这个们中来上大为和国地到以说时要就出会可也你对生能而子那得于着下自之年过发后作里用道行所然家种事成方多经么去法学如都同现当没动面起看定天分还进好小部其些主样理心她本前开但因只从想实日军者意无力它与长把机十民第公此已工使情明性知全三又关点正业外将两高间由问很最重并物手应战向头文体政美相见被利什二等产或新己制身果加西斯月话合回特代内信表化老给世位次度门任常先海通教儿原东声提立及比员解水名真论处走义各入几口认条平系气题活尔更别打女变四神总何电数安少报才结反受目太量再感建务做接必场件计管期市直德资命山金指克许统区保至队形社便空决治展马科司五基眼书非则听白却界达光放强即像难且权思王象完设式色路记南品住告类求据程北边死张该交规万取拉格望觉术领共确传师观清今切院让识候带导争运笑飞风步改收根干造言联持组每济车亲极林服快办议往元英士证近失转夫令准布始怎呢存未远叫台单影具罗字爱击流备兵连调深商算质团集百需价花党华城石级整府离况亚请技际约示复病息究线似官火断精满支视消越器容照须九增研写称企八功吗包片史委乎查轻易早曾除农找装广显吧阿李标谈吃图念六引历首医局突专费号尽另周较注语仅考落青随选列武红响虽推势参希古众构房半节土投某案黑维革划敌致陈律足态护七兴派孩验责营星够章音跟志底站严巴例防族供效续施留讲型料终答紧黄绝奇察母京段依批群项故按河米围江织害斗双境客纪采举杀攻父苏密低朝友诉止细愿千值仍男钱破网热助倒育属坐帝限船脸职速刻乐否刚威毛状率甚独球般普怕弹校苦创假久错承印晚兰试股拿脑预谁益阳若哪微尼继送急血惊伤素药适波夜省初喜卫源食险待述陆习置居劳财环排福纳欢雷警获模充负云停木游龙树疑层冷洲冲射略范竟句室异激汉村哈策演简卡罪判担州静退既衣您宗积余痛检差富灵协角占配征修皮挥胜降阶审沉坚善妈刘读啊超免压银买皇养伊怀执副乱抗犯追帮宣佛岁航优怪香著田铁控税左右份穿艺背阵草脚概恶块顿敢守酒岛托央户烈洋哥索胡款靠评版宝座释景顾弟登货互付伯慢欧换闻危忙核暗姐介坏讨丽良序升监临亮露永呼味野架域沙掉括舰鱼杂误湾吉减编楚肯测败屋跑梦散温困剑渐封救贵枪缺楼县尚毫移娘朋画班智亦耳恩短掌恐遗固席松秘谢鲁遇康虑幸均销钟诗藏赶剧票损忽巨炮旧端探湖录叶春乡附吸予礼港雨呀板庭妇归睛饭额含顺输摇招婚脱补谓督毒油疗旅泽材灭逐莫笔亡鲜词圣择寻厂睡博勒烟授诺伦岸奥唐卖俄炸载洛健堂旁宫喝借君禁阴园谋宋避抓荣姑孙逃牙束跳顶玉镇雪午练迫爷篇肉嘴馆遍凡础洞卷坦牛宁纸诸训私庄祖丝翻暴森塔默握戏隐熟骨访弱蒙店鬼软典欲萨伙遭盘爸扩盖弄雄稳忘亿刺拥徒姆杨齐赛趣曲刀床迎冰虚玩析窗醒妻透购替塞努休虎扬途侵刑绿兄迅套贸毕唯谷轮库迹尤竞街促延震弃甲伟麻川申缓潜闪售灯针哲络抵朱埃抱鼓植纯夏忍页杰筑折郑贝尊吴秀混臣雅振染盛怒舞圆搞狂措姓残秋培迷诚宽宇猛摆梅毁伸摩盟末乃悲拍丁赵硬麦蒋操耶阻订彩抽赞魔纷沿喊违妹浪汇币丰蓝殊献桌啦瓦莱援译夺汽烧距裁偏符勇触课哭懂墙袭召罚侠厅拜巧侧韩冒债曼融惯享戴童犹乘挂奖绍厚纵障讯涉彻刊丈爆乌役描洗玛患妙镜唱烦签仙彼弗症仿倾牌陷鸟轰咱菜闭奋庆撤泪茶疾缘播朗杜奶季丹狗尾仪偷奔珠虫驻孔宜艾桥淡翼恨繁寒伴叹旦愈潮粮缩罢聚径恰挑袋灰捕徐珍幕映裂泰隔启尖忠累炎暂估泛荒偿横拒瑞忆孤鼻闹羊呆厉衡胞零穷舍码赫婆魂灾洪腿胆津俗辩胸晓劲贫仁偶辑恢复较";
        private static bool loaded;

        internal static string Transform(string input, string scheme, bool reverse)
        {
            Load();
            string selected = string.IsNullOrWhiteSpace(scheme) ? SchemeChoices[0] : scheme;
            if (ChineseCodeTables.IsScheme(selected)) return ChineseCodeTables.Transform(input, selected, reverse);
            return reverse ? ReverseLookup(input, selected) : Encode(input, selected);
        }

        internal static IList<string> CodesFor(string character, string scheme)
        {
            Load(); Entry entry;
            return Entries.TryGetValue(character ?? string.Empty, out entry) ? Codes(entry, scheme) : new List<string>();
        }

        internal static string Metadata(string character, string field)
        {
            Load(); Entry e; if (!Entries.TryGetValue(character ?? string.Empty, out e)) return string.Empty;
            if (field == "释义") return e.Definition; if (field == "部首余笔") return e.RadicalStroke; if (field == "康熙索引") return e.KangXi;
            if (field == "简体异体") return e.Simplified; if (field == "繁体异体") return e.Traditional; if (field == "语义异体") return e.Semantic;
            if (field == "兼容异体") return e.Compatibility; if (field == "汉语拼音位置") return e.HanyuPinyin; if (field == "频率读音") return e.HanyuPinlu;
            if (field == "粤语资料") return e.CheungBauer; if (field == "注音资料") return e.Phonetic; return string.Empty;
        }

        internal static string LookupSummary(string code, string scheme)
        {
            Load(); Dictionary<string, List<string>> index = ReverseIndex(scheme); List<string> values; string token = NormalizeQuery(code, scheme); if (!index.TryGetValue(token, out values) || values.Count == 0) return "未收录"; int count = Math.Min(96, values.Count); StringBuilder result = new StringBuilder(); for (int i = 0; i < count; i++) result.Append(values[i]); if (values.Count > count) result.Append(" …（共 ").Append(values.Count).Append(" 字）"); return result.ToString();
        }

        internal static string NormalizePinyin(string value, out int tone) { return PlainPinyin(value, out tone); }
        internal static string ToBopomofo(string pinyin, int tone) { return Bopomofo(pinyin, tone); }
        internal static int MatchCount(string input, string scheme)
        {
            Load(); string[] raw = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，' }, StringSplitOptions.RemoveEmptyEntries); HashSet<string> pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (string token in raw) pending.Add(NormalizeQuery(token, scheme)); if (pending.Count == 0) return 0; HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (Entry entry in Entries.Values) foreach (string code in Codes(entry, scheme)) { string normalized = NormalizeQuery(code, scheme); if (pending.Contains(normalized)) found.Add(normalized); } return found.Count;
        }

        private static string Encode(string input, string scheme)
        {
            List<string> output = new List<string>();
            foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty))
            {
                if (string.IsNullOrWhiteSpace(unit)) continue;
                Entry entry; List<string> codes;
                if (Entries.TryGetValue(unit, out entry) && (codes = Codes(entry, scheme)).Count > 0) output.Add(string.Join("/", codes.ToArray()));
                else output.Add("[" + unit + "]");
            }
            return string.Join(" ", output.ToArray());
        }

        private static string ReverseLookup(string input, string scheme)
        {
            Dictionary<string, List<string>> index = ReverseIndex(scheme);
            string[] tokens = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，', ';', '；', '/', '、' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) throw new CipherException("请输入一个或多个输入码");
            StringBuilder output = new StringBuilder();
            foreach (string raw in tokens)
            {
                string token = NormalizeQuery(raw, scheme); List<string> values;
                if (output.Length > 0) output.Append("\r\n");
                output.Append(raw).Append(" → ");
                if (!index.TryGetValue(token, out values) && (token.IndexOf('*') >= 0 || token.IndexOf('?') >= 0)) { values = new List<string>(); Regex pattern = new Regex("^" + Regex.Escape(token).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase); foreach (KeyValuePair<string, List<string>> pair in index) if (pattern.IsMatch(pair.Key)) foreach (string value in pair.Value) if (!values.Contains(value)) values.Add(value); values.Sort(CompareCharacters); }
                if (values == null || values.Count == 0) { output.Append("未收录"); continue; }
                int count = Math.Min(96, values.Count);
                for (int i = 0; i < count; i++) output.Append(values[i]);
                if (values.Count > count) output.Append(" …（共 ").Append(values.Count).Append(" 字）");
            }
            return output.ToString();
        }

        private static Dictionary<string, List<string>> ReverseIndex(string scheme)
        {
            Dictionary<string, List<string>> index;
            if (Reverse.TryGetValue(scheme, out index)) return index;
            index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (Entry entry in Entries.Values)
            {
                foreach (string code in Codes(entry, scheme))
                {
                    string key = NormalizeQuery(code, scheme); List<string> values;
                    if (key.Length == 0) continue;
                    if (!index.TryGetValue(key, out values)) { values = new List<string>(); index[key] = values; }
                    if (!values.Contains(entry.Character)) values.Add(entry.Character);
                }
            }
            foreach (List<string> values in index.Values) values.Sort(CompareCharacters);
            Reverse[scheme] = index;
            return index;
        }

        private static int CompareCharacters(string left, string right)
        {
            int commonLeft = CommonCharacters.IndexOf(left, StringComparison.Ordinal), commonRight = CommonCharacters.IndexOf(right, StringComparison.Ordinal);
            if (commonLeft >= 0 || commonRight >= 0) { if (commonLeft < 0) return 1; if (commonRight < 0) return -1; if (commonLeft != commonRight) return commonLeft.CompareTo(commonRight); }
            int a = char.ConvertToUtf32(left, 0), b = char.ConvertToUtf32(right, 0);
            int ar = a >= 0x4E00 && a <= 0x9FFF ? 0 : a >= 0x3400 && a <= 0x4DBF ? 1 : 2;
            int br = b >= 0x4E00 && b <= 0x9FFF ? 0 : b >= 0x3400 && b <= 0x4DBF ? 1 : 2;
            return ar != br ? ar.CompareTo(br) : a.CompareTo(b);
        }

        private static List<string> Codes(Entry entry, string scheme)
        {
            List<string> result = new List<string>();
            if (ChineseCodeTables.IsScheme(scheme)) foreach (string value in ChineseCodeTables.CodesFor(entry.Character, scheme)) AddUnique(result, value);
            else if (scheme == "汉语拼音" || scheme == "汉语拼音（数字声调）" || scheme == "汉语拼音（声调符号）" || scheme == "拼音首字母" || scheme == "注音" || ChineseRomanization.IsDoublePinyin(scheme))
            {
                foreach (string reading in Split(entry.Mandarin))
                {
                    int tone; string plain = PlainPinyin(reading, out tone);
                    string value = scheme == "汉语拼音" ? plain : scheme == "汉语拼音（数字声调）" ? plain + (tone > 0 ? tone.ToString(CultureInfo.InvariantCulture) : string.Empty) : scheme == "汉语拼音（声调符号）" ? reading.ToLowerInvariant() : scheme == "拼音首字母" ? (plain.Length > 0 ? plain.Substring(0, 1) : string.Empty) : scheme == "注音" ? Bopomofo(plain, tone) : ChineseRomanization.DoublePinyin(plain, scheme);
                    AddUnique(result, value);
                }
            }
            else if (scheme == "粤拼") foreach (string value in Split(entry.Cantonese)) AddUnique(result, value.ToLowerInvariant());
            else if (scheme == "仓颉") foreach (string value in Split(entry.Cangjie)) AddUnique(result, value.ToUpperInvariant());
            else if (scheme == "速成") foreach (string value in Split(entry.Cangjie)) { string code = value.ToUpperInvariant(); AddUnique(result, code.Length < 2 ? code : code.Substring(0, 1) + code.Substring(code.Length - 1)); }
            else if (scheme == "四角号码") foreach (string value in Split(entry.FourCorner)) AddUnique(result, value);
            else if (scheme == "总笔画") foreach (string value in Split(entry.Strokes)) AddUnique(result, value);
            else if (scheme == "部首余笔") foreach (string value in Split(entry.RadicalStroke)) AddUnique(result, value);
            else if (scheme == "康熙索引") foreach (string value in Split(entry.KangXi)) AddUnique(result, value);
            return result;
        }

        private static string[] Split(string value) { return (value ?? string.Empty).Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries); }
        private static void AddUnique(List<string> values, string value) { if (!string.IsNullOrEmpty(value) && !values.Contains(value)) values.Add(value); }
        private static string NormalizeQuery(string value, string scheme)
        {
            string result = (value ?? string.Empty).Trim();
            if (scheme == "汉语拼音" || scheme == "汉语拼音（数字声调）" || scheme == "汉语拼音（声调符号）" || scheme == "拼音首字母") { int tone; string plain = PlainPinyin(result, out tone); return scheme == "汉语拼音（数字声调）" && tone > 0 ? plain + tone.ToString(CultureInfo.InvariantCulture) : scheme == "汉语拼音（声调符号）" ? result.ToLowerInvariant() : plain; }
            if (scheme == "仓颉" || scheme == "速成") return result.ToUpperInvariant();
            return result.ToLowerInvariant();
        }

        private static string PlainPinyin(string value, out int tone)
        {
            tone = 0; StringBuilder result = new StringBuilder();
            foreach (char raw in (value ?? string.Empty).ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(raw);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    if (raw == '\u0304') tone = 1; else if (raw == '\u0301') tone = 2; else if (raw == '\u030C') tone = 3; else if (raw == '\u0300') tone = 4;
                    else if (raw == '\u0308' && result.Length > 0 && result[result.Length - 1] == 'u') result[result.Length - 1] = 'v';
                    continue;
                }
                if (raw >= '1' && raw <= '5') { tone = raw - '0'; continue; }
                if (raw >= 'a' && raw <= 'z') result.Append(raw);
            }
            return result.ToString();
        }

        private static string Bopomofo(string pinyin, int tone)
        {
            string value = pinyin, initial = string.Empty;
            if (value.StartsWith("y"))
            {
                string rest = value.Substring(1); value = rest == "i" ? "i" : rest == "u" ? "v" : rest.StartsWith("u") ? "v" + rest.Substring(1) : rest.StartsWith("i") ? rest : "i" + rest;
            }
            else if (value.StartsWith("w")) { string rest = value.Substring(1); value = rest == "u" ? "u" : rest.StartsWith("u") ? rest : "u" + rest; }
            string[] initials = { "zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "r", "z", "c", "s" };
            string[] symbols = { "ㄓ", "ㄔ", "ㄕ", "ㄅ", "ㄆ", "ㄇ", "ㄈ", "ㄉ", "ㄊ", "ㄋ", "ㄌ", "ㄍ", "ㄎ", "ㄏ", "ㄐ", "ㄑ", "ㄒ", "ㄖ", "ㄗ", "ㄘ", "ㄙ" };
            for (int i = 0; i < initials.Length; i++) if (value.StartsWith(initials[i])) { initial = symbols[i]; value = value.Substring(initials[i].Length); if ((initials[i] == "j" || initials[i] == "q" || initials[i] == "x") && value.StartsWith("u")) value = "v" + value.Substring(1); break; }
            if (value == "i" && (initial == "ㄓ" || initial == "ㄔ" || initial == "ㄕ" || initial == "ㄖ" || initial == "ㄗ" || initial == "ㄘ" || initial == "ㄙ")) value = string.Empty;
            string[] finals = { "iang", "iong", "uang", "ueng", "iao", "ian", "ing", "uai", "uan", "van", "ang", "eng", "ong", "ia", "ie", "iu", "in", "ua", "uo", "ui", "un", "ve", "vn", "ai", "ei", "ao", "ou", "an", "en", "er", "a", "o", "e", "i", "u", "v", "" };
            string[] finalsZh = { "ㄧㄤ", "ㄩㄥ", "ㄨㄤ", "ㄨㄥ", "ㄧㄠ", "ㄧㄢ", "ㄧㄥ", "ㄨㄞ", "ㄨㄢ", "ㄩㄢ", "ㄤ", "ㄥ", "ㄨㄥ", "ㄧㄚ", "ㄧㄝ", "ㄧㄡ", "ㄧㄣ", "ㄨㄚ", "ㄨㄛ", "ㄨㄟ", "ㄨㄣ", "ㄩㄝ", "ㄩㄣ", "ㄞ", "ㄟ", "ㄠ", "ㄡ", "ㄢ", "ㄣ", "ㄦ", "ㄚ", "ㄛ", "ㄜ", "ㄧ", "ㄨ", "ㄩ", "" };
            int at = Array.IndexOf(finals, value); if (at < 0) return string.Empty;
            string[] tones = { "", "", "ˊ", "ˇ", "ˋ", "˙" };
            return initial + finalsZh[at] + (tone >= 0 && tone < tones.Length ? tones[tone] : string.Empty);
        }

        private static void Load()
        {
            if (loaded) return; loaded = true;
            Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicalCipherToolbox.Analysis.ChineseInputCodes");
            if (resource == null) throw new CipherException("中文输入法码表未嵌入");
            using (resource) using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress)) using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
            {
                string line; while ((line = reader.ReadLine()) != null)
                {
                    string[] fields = line.Split('\t'); int codepoint;
                    if (fields.Length < 6 || !int.TryParse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codepoint)) continue;
                    string character = char.ConvertFromUtf32(codepoint);
                    Entries[character] = new Entry { Character = character, Mandarin = fields[1], Cantonese = fields[2], Cangjie = fields[3], FourCorner = fields[4], Strokes = fields[5], RadicalStroke = Field(fields, 6), Definition = Field(fields, 7), Simplified = Field(fields, 8), Traditional = Field(fields, 9), Semantic = Field(fields, 10), Compatibility = Field(fields, 11), HanyuPinlu = Field(fields, 12), HanyuPinyin = Field(fields, 13), CheungBauer = Field(fields, 14), Phonetic = Field(fields, 15), KangXi = Field(fields, 16) };
                }
            }
        }

        private static string Field(string[] fields, int index) { return fields.Length > index ? fields[index] : string.Empty; }
    }

    internal static class NatoPhonetic
    {
        private static readonly string[] Words = { "Alfa","Bravo","Charlie","Delta","Echo","Foxtrot","Golf","Hotel","India","Juliett","Kilo","Lima","Mike","November","Oscar","Papa","Quebec","Romeo","Sierra","Tango","Uniform","Victor","Whiskey","X-ray","Yankee","Zulu" };
        internal static string Transform(string input, bool decode) { if (!decode) { List<string> result = new List<string>(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); result.Add(c >= 'A' && c <= 'Z' ? Words[c - 'A'] : raw.ToString()); } return string.Join(" ", result.ToArray()); } StringBuilder output = new StringBuilder(); foreach (string token in (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)) { int p = Array.FindIndex(Words, delegate(string value) { return value.Equals(token, StringComparison.OrdinalIgnoreCase); }); output.Append(p >= 0 ? (char)('A' + p) : token[0]); } return output.ToString(); }
    }

    internal static class SymbolCodes
    {
        private const string PigpenSymbols = "⌜⌝⌞⌟┬┤┴├┼◰◳◲◱◉⊞◈△▷▽◁▲▶▼◀◆◇";
        private static readonly string[] Semaphore = { "↓↙","↓←","↓↖","↓↑","↗↓","→↓","↘↓","↙←","↙↖","↑→","↙↑","↙↗","↙→","↙↘","←↖","←↑","←↗","←→","←↘","↖↑","↑↘","↘↗","→↖","↖↗","↖↘","↗→" };
        internal static string Pigpen(string input, bool decode) { if (!decode) { StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); result.Append(c >= 'A' && c <= 'Z' ? PigpenSymbols[c - 'A'] : raw); } return result.ToString(); } StringBuilder output = new StringBuilder(); foreach (char c in input ?? string.Empty) { int p = PigpenSymbols.IndexOf(c); output.Append(p >= 0 ? (char)('A' + p) : c); } return output.ToString(); }
        internal static string FlagSemaphore(string input, bool decode) { if (!decode) { List<string> result = new List<string>(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); result.Add(c >= 'A' && c <= 'Z' ? Semaphore[c - 'A'] : raw.ToString()); } return string.Join(" / ", result.ToArray()); } StringBuilder output = new StringBuilder(); foreach (string token in (input ?? string.Empty).Split(new[] { '/', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) { int p = Array.IndexOf(Semaphore, token); output.Append(p >= 0 ? (char)('A' + p) : '?'); } return output.ToString(); }
    }

    internal static class ColorEncoding
    {
        internal static string Text(string input, bool decode)
        {
            if (!decode) { byte[] data = Encoding.UTF8.GetBytes(input ?? string.Empty); StringBuilder result = new StringBuilder("长度:").Append(data.Length).Append("\r\n"); for (int i = 0; i < data.Length; i += 3) { if (i > 0) result.Append(' '); result.Append('#'); for (int j = 0; j < 3; j++) result.Append((i + j < data.Length ? data[i + j] : (byte)0).ToString("X2", CultureInfo.InvariantCulture)); } return result.ToString(); }
            int length = -1, marker = (input ?? string.Empty).IndexOf("长度:", StringComparison.Ordinal); if (marker >= 0) { int end = (input ?? string.Empty).IndexOfAny(new[] { '\r', '\n' }, marker); int.TryParse((end < 0 ? input.Substring(marker + 3) : input.Substring(marker + 3, end - marker - 3)).Trim(), out length); } List<byte> bytes = new List<byte>(); string source = input ?? string.Empty; for (int i = 0; i + 6 < source.Length; i++) if (source[i] == '#' && IsHex6(source, i + 1)) { for (int j = 0; j < 3; j++) bytes.Add(byte.Parse(source.Substring(i + 1 + j * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 6; } if (length >= 0 && length < bytes.Count) bytes.RemoveRange(length, bytes.Count - length); return Encoding.UTF8.GetString(bytes.ToArray());
        }
        internal static string Palette(string input)
        {
            int r, g, b; ParseColor(input, out r, out g, out b); double h, s, l; RgbToHsl(r, g, b, out h, out s, out l); List<string> colors = new List<string> { Hex(r,g,b), HslHex((h+180)%360,s,l), HslHex((h+30)%360,s,l), HslHex((h+330)%360,s,l), HslHex((h+120)%360,s,l), HslHex((h+240)%360,s,l) }; return "HEX：" + colors[0] + "\r\nRGB：" + r + ", " + g + ", " + b + "\r\nHSL：" + h.ToString("0.0",CultureInfo.InvariantCulture) + "°, " + (s*100).ToString("0.0",CultureInfo.InvariantCulture) + "%, " + (l*100).ToString("0.0",CultureInfo.InvariantCulture) + "%\r\n调色盘：" + string.Join("  ", colors.ToArray());
        }
        private static bool IsHex6(string value, int start) { if (start + 6 > value.Length) return false; for (int i = 0; i < 6; i++) if (!Uri.IsHexDigit(value[start+i])) return false; return true; }
        private static void ParseColor(string input, out int r, out int g, out int b) { string value = (input ?? string.Empty).Trim(); if (value.StartsWith("#")) value = value.Substring(1); if (value.Length == 3) value = new string(new[] { value[0],value[0],value[1],value[1],value[2],value[2] }); if (value.Length == 6 && IsHex6(value,0)) { r=int.Parse(value.Substring(0,2),NumberStyles.HexNumber); g=int.Parse(value.Substring(2,2),NumberStyles.HexNumber); b=int.Parse(value.Substring(4,2),NumberStyles.HexNumber); return; } string[] parts=value.Replace("rgb(",string.Empty).Replace(")",string.Empty).Split(','); if(parts.Length==3&&int.TryParse(parts[0].Trim(),out r)&&int.TryParse(parts[1].Trim(),out g)&&int.TryParse(parts[2].Trim(),out b)){r=Limit(r);g=Limit(g);b=Limit(b);return;} throw new CipherException("请输入 #RRGGBB 或 R,G,B"); }
        private static int Limit(int v){return Math.Max(0,Math.Min(255,v));} private static string Hex(int r,int g,int b){return "#"+r.ToString("X2")+g.ToString("X2")+b.ToString("X2");}
        private static void RgbToHsl(int r,int g,int b,out double h,out double s,out double l){double rr=r/255.0,gg=g/255.0,bb=b/255.0,max=Math.Max(rr,Math.Max(gg,bb)),min=Math.Min(rr,Math.Min(gg,bb)),d=max-min;l=(max+min)/2;s=d==0?0:d/(1-Math.Abs(2*l-1));if(d==0)h=0;else if(max==rr)h=60*(((gg-bb)/d)%6);else if(max==gg)h=60*((bb-rr)/d+2);else h=60*((rr-gg)/d+4);if(h<0)h+=360;}
        private static string HslHex(double h,double s,double l){double c=(1-Math.Abs(2*l-1))*s,x=c*(1-Math.Abs((h/60)%2-1)),m=l-c/2,rr=0,gg=0,bb=0;if(h<60){rr=c;gg=x;}else if(h<120){rr=x;gg=c;}else if(h<180){gg=c;bb=x;}else if(h<240){gg=x;bb=c;}else if(h<300){rr=x;bb=c;}else{rr=c;bb=x;}return Hex((int)Math.Round((rr+m)*255),(int)Math.Round((gg+m)*255),(int)Math.Round((bb+m)*255));}
    }

    internal static class BarcodeCode
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
        private static readonly string[] Patterns = { "nnnwwnwnn","wnnwnnnnw","nnwwnnnnw","wnwwnnnnn","nnnwwnnnw","wnnwwnnnn","nnwwwnnnn","nnnwnnwnw","wnnwnnwnn","nnwwnnwnn","wnnnnwnnw","nnwnnwnnw","wnwnnwnnn","nnnnwwnnw","wnnnwwnnn","nnwnwwnnn","nnnnnwwnw","wnnnnwwnn","nnwnnwwnn","nnnnwwwnn","wwnnnnnnw","nwwnnnnnw","wwwnnnnnn","nwnnwnnnw","wwnnwnnnn","nwwnwnnnn","nwnnwnwnn","wwnnnnwnn","nwwnnnwnn","nwnwnwnnn","wwnnnwnnn","nwwnwnnnn","nnnwnwnnw","wnnwnwnnn","nnwwnwnnn","nnnwnwwnn","wnnnnnnww","nnwnnnnww","wnwnnnnwn","nnnnwnnww","wnnnwnnwn","nnwnwnnwn","nnnnnnwww","wnnnnnwwn" };
        internal static string Transform(string input, string type, bool decode) { string kind=(type??string.Empty).Trim().ToUpperInvariant(); return kind=="EAN13"||kind=="EAN-13" ? Ean13(input,decode) : Code39(input,decode); }
        private static string Code39(string input,bool decode){if(!decode){string text="*"+(input??string.Empty).ToUpperInvariant()+"*";StringBuilder bits=new StringBuilder();foreach(char c in text){int p=Alphabet.IndexOf(c);if(p<0)throw new CipherException("Code 39 仅支持数字、大写字母和 - . 空格 $ / + %");if(bits.Length>0)bits.Append('0');string pattern=Patterns[p];for(int i=0;i<9;i++)bits.Append(new string(i%2==0?'1':'0',pattern[i]=='w'?3:1));}return Render(bits.ToString(),text.Substring(1,text.Length-2));}string raw=ExtractBits(input);List<int> runs=Runs(raw);StringBuilder value=new StringBuilder();for(int at=0;at+8<runs.Count;at+=10){StringBuilder pattern=new StringBuilder();for(int i=0;i<9;i++)pattern.Append(runs[at+i]>1?'w':'n');int p=Array.IndexOf(Patterns,pattern.ToString());if(p<0)throw new CipherException("无法识别 Code 39 条纹");value.Append(Alphabet[p]);}string result=value.ToString();return result.Length>=2&&result[0]=='*'&&result[result.Length-1]=='*'?result.Substring(1,result.Length-2):result;}
        private static string Ean13(string input,bool decode){string[] l={"0001101","0011001","0010011","0111101","0100011","0110001","0101111","0111011","0110111","0001011"},g={"0100111","0110011","0011011","0100001","0011101","0111001","0000101","0010001","0001001","0010111"},r={"1110010","1100110","1101100","1000010","1011100","1001110","1010000","1000100","1001000","1110100"};string[] parity={"LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG","LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL"};if(!decode){string digits="";foreach(char c in input??string.Empty)if(char.IsDigit(c))digits+=c;if(digits.Length==12)digits+=Checksum(digits);if(digits.Length!=13||Checksum(digits.Substring(0,12))!=digits[12])throw new CipherException("EAN-13 需要 12 位数字，或含正确校验位的 13 位数字");StringBuilder bits=new StringBuilder("101");int first=digits[0]-'0';for(int i=1;i<=6;i++){int d=digits[i]-'0';bits.Append(parity[first][i-1]=='L'?l[d]:g[d]);}bits.Append("01010");for(int i=7;i<13;i++)bits.Append(r[digits[i]-'0']);bits.Append("101");return Render(bits.ToString(),digits);}string raw=ExtractBits(input);if(raw.Length!=95||!raw.StartsWith("101")||!raw.EndsWith("101"))throw new CipherException("无法识别 EAN-13 位串");StringBuilder left=new StringBuilder(),patternCode=new StringBuilder();for(int i=0;i<6;i++){string part=raw.Substring(3+i*7,7);int p=Array.IndexOf(l,part);if(p>=0){left.Append(p);patternCode.Append('L');}else{p=Array.IndexOf(g,part);if(p<0)throw new CipherException("EAN-13 左半区无效");left.Append(p);patternCode.Append('G');}}int leading=Array.IndexOf(parity,patternCode.ToString());if(leading<0)throw new CipherException("EAN-13 奇偶模式无效");StringBuilder result=new StringBuilder().Append(leading).Append(left);for(int i=0;i<6;i++){int p=Array.IndexOf(r,raw.Substring(50+i*7,7));if(p<0)throw new CipherException("EAN-13 右半区无效");result.Append(p);}return result.ToString();}
        private static char Checksum(string value){int sum=0;for(int i=0;i<12;i++)sum+=(value[i]-'0')*(i%2==0?1:3);return(char)('0'+(10-sum%10)%10);}private static int IndexBlock(string table,string block){for(int i=0;i<10;i++)if(table.Substring(i*7,7)==block)return i;return-1;}private static string Render(string bits,string text){StringBuilder visual=new StringBuilder();foreach(char b in bits)visual.Append(b=='1'?'█':' ');return "内容："+text+"\r\n位串："+bits+"\r\n图形：\r\n"+visual+"\r\n"+visual+"\r\n"+visual;}
        private static string ExtractBits(string input){string source=input??string.Empty;int at=source.IndexOf("位串：",StringComparison.Ordinal);if(at>=0)source=source.Substring(at+3).Split(new[]{'\r','\n'})[0];StringBuilder bits=new StringBuilder();foreach(char c in source)if(c=='0'||c=='1')bits.Append(c);return bits.ToString().Trim('0');}private static List<int> Runs(string bits){List<int> result=new List<int>();if(bits.Length==0)return result;char last=bits[0];int count=0;foreach(char c in bits){if(c==last)count++;else{result.Add(count);last=c;count=1;}}result.Add(count);return result;}
    }

    internal static class QrCodeV1
    {
        private const int Size=21;
        internal static string Transform(string input,bool decode){return decode?Decode(input):Encode(input);}
        private static string Encode(string input){byte[] data=Encoding.UTF8.GetBytes(input??string.Empty);if(data.Length>17)throw new CipherException("当前离线 QR 实现支持 Version 1-L，UTF-8 最多 17 字节");List<int> bits=new List<int>();Append(bits,4,4);Append(bits,data.Length,8);foreach(byte b in data)Append(bits,b,8);for(int i=0;i<4&&bits.Count<152;i++)bits.Add(0);while(bits.Count%8!=0)bits.Add(0);List<byte> code=new List<byte>();for(int i=0;i<bits.Count;i+=8){int v=0;for(int j=0;j<8;j++)v=(v<<1)|bits[i+j];code.Add((byte)v);}bool toggle=true;while(code.Count<19){code.Add(toggle?(byte)0xEC:(byte)0x11);toggle=!toggle;}code.AddRange(ReedSolomon(code.ToArray(),7));bool[,] matrix=BuildBase();PlaceData(matrix,code.ToArray(),0);PlaceFormat(matrix,0);return Render(matrix,input??string.Empty);}
        private static string Decode(string input){bool[,] matrix=ParseMatrix(input);List<int> bits=ReadData(matrix,0);int mode=Read(bits,0,4),count=Read(bits,4,8);if(mode!=4||count<0||count>17)throw new CipherException("仅支持本工具生成的 QR Version 1-L 字节模式");byte[] data=new byte[count];for(int i=0;i<count;i++)data[i]=(byte)Read(bits,12+i*8,8);return Encoding.UTF8.GetString(data);}
        private static bool[,] BuildBase(){bool[,] m=new bool[Size,Size];bool[,] used=new bool[Size,Size];PlaceFinder(m,used,0,0);PlaceFinder(m,used,Size-7,0);PlaceFinder(m,used,0,Size-7);for(int i=8;i<Size-8;i++){Set(m,used,i,6,i%2==0);Set(m,used,6,i,i%2==0);}for(int i=0;i<9;i++){if(!used[8,i])Set(m,used,8,i,false);if(!used[i,8])Set(m,used,i,8,false);}for(int i=Size-8;i<Size;i++){Set(m,used,8,i,false);Set(m,used,i,8,false);}Set(m,used,8,Size-8,true);FunctionMask=m;UsedMask=used;return m;}
        [ThreadStatic]private static bool[,] FunctionMask;[ThreadStatic]private static bool[,] UsedMask;
        private static void PlaceFinder(bool[,]m,bool[,]u,int x,int y){for(int dy=-1;dy<=7;dy++)for(int dx=-1;dx<=7;dx++){int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=Size||yy>=Size)continue;bool dark=dx>=0&&dx<=6&&dy>=0&&dy<=6&&(dx==0||dx==6||dy==0||dy==6||(dx>=2&&dx<=4&&dy>=2&&dy<=4));Set(m,u,xx,yy,dark);}}
        private static void Set(bool[,]m,bool[,]u,int x,int y,bool value){m[y,x]=value;u[y,x]=true;}
        private static void PlaceData(bool[,]m,byte[]code,int mask){int bit=0;bool upward=true;for(int right=Size-1;right>=1;right-=2){if(right==6)right--;for(int vert=0;vert<Size;vert++){int y=upward?Size-1-vert:vert;for(int j=0;j<2;j++){int x=right-j;if(UsedMask[y,x])continue;bool value=bit<code.Length*8&&((code[bit>>3]>>(7-(bit&7)))&1)!=0;if(Mask(mask,x,y))value=!value;m[y,x]=value;bit++;}}upward=!upward;}}
        private static List<int> ReadData(bool[,]m,int mask){bool[,]basis=BuildBase();List<int>result=new List<int>();bool upward=true;for(int right=Size-1;right>=1;right-=2){if(right==6)right--;for(int vert=0;vert<Size;vert++){int y=upward?Size-1-vert:vert;for(int j=0;j<2;j++){int x=right-j;if(UsedMask[y,x])continue;bool value=m[y,x];if(Mask(mask,x,y))value=!value;result.Add(value?1:0);}}upward=!upward;}return result;}
        private static bool Mask(int mask,int x,int y){switch(mask){case 0:return(x+y)%2==0;case 1:return y%2==0;case 2:return x%3==0;default:return(x+y)%3==0;}}
        private static void PlaceFormat(bool[,]m,int mask){int data=(1<<3)|mask,bits=data<<10;int generator=0x537;for(int i=14;i>=10;i--)if(((bits>>i)&1)!=0)bits^=generator<<(i-10);bits=((data<<10)|bits)^0x5412;for(int i=0;i<=5;i++)m[i,8]=Bit(bits,i);m[7,8]=Bit(bits,6);m[8,8]=Bit(bits,7);m[8,7]=Bit(bits,8);for(int i=9;i<15;i++)m[8,14-i]=Bit(bits,i);for(int i=0;i<8;i++)m[8,Size-1-i]=Bit(bits,i);for(int i=8;i<15;i++)m[Size-15+i,8]=Bit(bits,i);m[Size-8,8]=true;}
        private static bool Bit(int value,int index){return((value>>index)&1)!=0;}private static void Append(List<int>b,int value,int count){for(int i=count-1;i>=0;i--)b.Add((value>>i)&1);}private static int Read(List<int>b,int start,int count){if(start+count>b.Count)return-1;int value=0;for(int i=0;i<count;i++)value=(value<<1)|b[start+i];return value;}
        private static byte[] ReedSolomon(byte[]data,int degree){byte[]gen={1};for(int i=0;i<degree;i++){byte[]next=new byte[gen.Length+1];for(int j=0;j<gen.Length;j++){next[j]^=gen[j];next[j+1]^=Multiply(gen[j],Pow2(i));}gen=next;}byte[]rem=new byte[degree];foreach(byte value in data){byte factor=(byte)(value^rem[0]);for(int i=0;i<degree-1;i++)rem[i]=rem[i+1];rem[degree-1]=0;for(int i=0;i<degree;i++)rem[i]^=Multiply(gen[i+1],factor);}return rem;}
        private static byte Multiply(byte x,byte y){int z=0;for(int i=7;i>=0;i--){z=(z<<1)^(((z>>7)&1)*0x11D);if(((y>>i)&1)!=0)z^=x;}return(byte)z;}private static byte Pow2(int power){int value=1;for(int i=0;i<power;i++)value=(value<<1)^(((value>>7)&1)*0x11D);return(byte)value;}
        private static string Render(bool[,]m,string content){StringBuilder outp=new StringBuilder("内容：").Append(content).Append("\r\n矩阵：\r\n");for(int y=0;y<Size;y++){for(int x=0;x<Size;x++)outp.Append(m[y,x]?'1':'0');outp.Append("\r\n");}outp.Append("图形：\r\n");for(int y=-4;y<Size+4;y++){for(int x=-4;x<Size+4;x++){bool dark=x>=0&&y>=0&&x<Size&&y<Size&&m[y,x];outp.Append(dark?"██":"  ");}outp.Append("\r\n");}return outp.ToString().TrimEnd();}
        private static bool[,] ParseMatrix(string input){string source=input??string.Empty;int at=source.IndexOf("矩阵：",StringComparison.Ordinal);if(at>=0)source=source.Substring(at+3);string[]lines=source.Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries);List<string>rows=new List<string>();foreach(string line in lines){string s=line.Trim();if(s.Length==Size){bool valid=true;foreach(char c in s)if(c!='0'&&c!='1')valid=false;if(valid)rows.Add(s);}if(rows.Count==Size)break;}if(rows.Count!=Size)throw new CipherException("请粘贴本工具输出中的 21×21 矩阵");bool[,]m=new bool[Size,Size];for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)m[y,x]=rows[y][x]=='1';return m;}
    }
}

