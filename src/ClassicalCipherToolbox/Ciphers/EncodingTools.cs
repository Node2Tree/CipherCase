using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
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
        private static void Load()
        {
            if (loaded) return; loaded = true; Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicalCipherToolbox.Analysis.ChineseTelegraph"); if (resource == null) throw new CipherException("中文电报码表未嵌入"); using (resource) using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress)) using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8)) { string line; while ((line = reader.ReadLine()) != null) { int at = line.LastIndexOf('='); if (at <= 0) continue; string character = line.Substring(0, at), code = line.Substring(at + 1); ToCode[character] = code; if (!FromCode.ContainsKey(code)) FromCode[code] = character; } }
        }
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
