using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class CipherIdentifier
    {
        private sealed class Guess
        {
            internal Guess(string name, int score, string reason) { Name = name; Score = score; Reason = reason; }
            internal string Name; internal int Score; internal string Reason;
        }
        private sealed class PeriodicProbe
        {
            internal string Name; internal int Period; internal string Key; internal string Language; internal double AverageIc; internal int KasiskiVotes; internal double PlainScore; internal double Quality;
        }
        private sealed class TransformProbe
        {
            internal string Name; internal string Key; internal string Language; internal double PlainScore; internal double Gain;
        }

        internal static string Identify(string input, string clue)
        {
            return Identify(input, clue, "AUTO");
        }

        internal static string Identify(string input, string clue, string method)
        {
            string source = (input ?? string.Empty).Trim();
            if (source.Length < 2) throw new CipherException("文本太短，无法识别");
            string letters = Letters(source); string upper = source.ToUpperInvariant(); string digits = Digits(source); string matchMethod = LanguageModels.NormalizeMatchMethod(method, letters.Length);
            List<Guess> guesses = new List<Guess>();
            AddFormatGuesses(source, upper, letters, digits, guesses);
            if (letters.Length < 2)
            {
                List<string> unicode = UnicodeAnalysis.Units(source); if (unicode.Count >= 2) { int different = UnicodeAnalysis.Different(unicode); double unicodeIc = UnicodeAnalysis.Coincidence(unicode); if (different == 16 && unicode.Count >= 40 && unicode.Count % 4 == 0) { HashSet<string> leading = new HashSet<string>(); for (int i = 0; i < unicode.Count; i += 4) leading.Add(unicode[i]); if (leading.Count <= 10) AddGuess(guesses, "中文编码单表（Unicode 十六进制）", 97, string.Format(CultureInfo.InvariantCulture, "{0} 个符号可分为 {1} 组四位码；首位只出现 {2} 种符号，符合 Unicode 汉字码位结构；在单表替换中选择 ZH 破解", unicode.Count, unicode.Count / 4, leading.Count)); } if (unicode.Count >= 40 && different >= 2 && different <= 26 && unicodeIc >= .052 && UnicodeAnalysis.IsPredominantlyNonAscii(unicode)) AddGuess(guesses, "单表替换（Unicode 符号）", 92, string.Format(CultureInfo.InvariantCulture, "{0} 个文本元素、{1} 种符号；IC {2:0.000000} 保留单表频率结构；可直接进入单表替换破解", unicode.Count, different, unicodeIc)); AddGuess(guesses, "非拉丁文本 / 同字符集密码", 68, string.Format(CultureInfo.InvariantCulture, "文字体系 {0}；字符 {1}；符号 {2}；IC {3:0.000000}；Shannon {4:0.000}", UnicodeAnalysis.ScriptSummary(source), unicode.Count, different, unicodeIc, UnicodeAnalysis.Entropy(unicode))); }
            }

            if (letters.Length >= 12)
            {
                double ic = Coincidence(letters); string language; double sourceLanguageScore = BestLanguageScore(letters, out language, matchMethod); double coherence = FormatCoherence(source); bool compactLetters = ContainsOnlyLettersAndWhitespace(source) && !ContainsWhitespace(source);
                bool naturalLayout = coherence >= 0.58 || (!HasLowercase(source) && sourceLanguageScore >= 0.45);
                if (letters.Length >= 40 && ContainsWhitespace(source) && naturalLayout && sourceLanguageScore >= 0.12)
                    AddGuess(guesses, "未加密自然语言", Math.Min(99, 82 + (int)Math.Round(sourceLanguageScore * 18)), "语言 " + language + "（" + LanguageModels.MatchMethodLabel(matchMethod, letters.Length) + "），文本结构和连续片段与自然语言一致");

                TransformProbe substitution = letters.Length >= 40 && UniqueLetters(letters) >= 8 ? ProbeSimpleSubstitution(source, sourceLanguageScore, matchMethod) : null;
                if (substitution != null)
                    AddGuess(guesses, substitution.Name, ConfidenceFromGain(substitution.Gain, 78), "实际试解最符合 " + substitution.Language + "，密钥 " + substitution.Key);

                PeriodicProbe periodic = letters.Length >= 40 ? ProbePeriodic(letters, ic, matchMethod) : null;
                if (periodic != null)
                    AddGuess(guesses, periodic.Name, ConfidenceFromGain(periodic.PlainScore + periodic.AverageIc - ic, 74), string.Format(CultureInfo.InvariantCulture,
                        "整体 IC {0:0.0000}；周期 {1} 的平均分列 IC {2:0.0000}；Kasiski {3}；语言 {4}；候选密钥 {5}",
                        ic, periodic.Period, periodic.AverageIc, periodic.KasiskiVotes, periodic.Language, periodic.Key));

                if (compactLetters && letters.Length >= 60 && ic < 0.055)
                    AddGuess(guesses, "Fractionated Morse / 分数化密码", periodic == null ? 66 : 60,
                        string.Format(CultureInfo.InvariantCulture, "纯字母连续文本，IC {0:0.0000}；与周期多表结果一并验证", ic));

                TransformProbe transposition = letters.Length >= 40 && ic >= 0.052 ? ProbeSimpleTranspositions(source, sourceLanguageScore, matchMethod) : null;
                if (transposition != null)
                    AddGuess(guesses, transposition.Name, ConfidenceFromGain(transposition.Gain, 76), "实际试解最符合 " + transposition.Language + "，参数 " + transposition.Key);

                if (ic >= 0.055)
                {
                    if (compactLetters)
                    {
                        AddGuess(guesses, "Fractionated Morse / 分数化密码", 70, string.Format(CultureInfo.InvariantCulture, "纯字母连续文本，IC {0:0.0000}", ic));
                        AddGuess(guesses, "单表替换", 52, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000} 保留频率结构", ic));
                    }
                    else if (coherence >= 0.58)
                    {
                        AddGuess(guesses, "单表替换", 72, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000}，词界与标点结构完整", ic));
                        AddGuess(guesses, "换位密码", 49, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000} 保留字母频率", ic));
                    }
                    else
                    {
                        AddGuess(guesses, "换位密码", 78, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000} 保留频率，字符边界结构被重排", ic));
                        AddGuess(guesses, "单表替换", 48, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000} 保留字母频率", ic));
                    }
                }
                else if (ic < 0.054 && periodic == null)
                {
                    if (compactLetters)
                    {
                        if (letters.Length % 2 == 0) AddGuess(guesses, "Playfair / Four-square / Two-square", 68, string.Format(CultureInfo.InvariantCulture, "偶数字母流，IC {0:0.0000}", ic));
                        AddGuess(guesses, "Bifid / Trifid / Digrafid", 64, string.Format(CultureInfo.InvariantCulture, "连续分数化字母流，IC {0:0.0000}", ic));
                    }
                    else AddGuess(guesses, "Autokey / Running Key / 多表密码", 64, string.Format(CultureInfo.InvariantCulture, "IC {0:0.0000}，未发现稳定重复周期", ic));
                }
            }

            string hint = ClueHint(clue);
            if (hint.Length > 0) foreach (Guess guess in guesses) if (guess.Name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0) guess.Score = Math.Min(100, guess.Score + 10);
            guesses.Sort(delegate(Guess a, Guess b) { int score = b.Score.CompareTo(a.Score); return score != 0 ? score : string.CompareOrdinal(a.Name, b.Name); });
            if (guesses.Count == 0) guesses.Add(new Guess("未知结构", 30, "字符集和统计特征未形成已注册家族的匹配"));
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < Math.Min(10, guesses.Count); i++) result.AppendFormat("#{0}  类型 {1}  匹配 {2}\r\n{3}\r\n\r\n", i + 1, guesses[i].Name, guesses[i].Score, guesses[i].Reason);
            return result.ToString().TrimEnd();
        }

        private static void AddFormatGuesses(string source, string upper, string letters, string digits, List<Guess> guesses)
        {
            HashSet<char> symbols = new HashSet<char>(); foreach (char value in upper) if (!char.IsWhiteSpace(value)) symbols.Add(value);
            string compact = RemoveWhitespace(source); bool onlyHex = compact.Length >= 8 && compact.Length % 2 == 0, onlyBinary = compact.Length >= 8 && compact.Length % 8 == 0, onlyBase64 = compact.Length >= 8 && compact.Length % 4 == 0, onlyBase32 = compact.Length >= 8 && compact.Length % 8 == 0; int braille = 0; foreach (char c in compact) { if (!Uri.IsHexDigit(c)) onlyHex = false; if (c != '0' && c != '1') onlyBinary = false; if (!(char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=' || c == '-' || c == '_')) onlyBase64 = false; char b = char.ToUpperInvariant(c); if (!(b >= 'A' && b <= 'Z') && !(b >= '2' && b <= '7') && c != '=') onlyBase32 = false; if (c >= '\u2800' && c <= '\u28FF') braille++; }
            if (onlyBinary) AddGuess(guesses, "二进制", 98, "字符集仅为 0/1，位数是 8 的倍数");
            else if (onlyHex && HasDigitAndHexLetter(compact)) AddGuess(guesses, "十六进制", 92, "字符集为十六进制，位数是字节的倍数");
            if (onlyHex && HasDigitAndHexLetter(compact)) { string charset, decoded; if (TryChineseCharset(compact, out charset, out decoded)) AddGuess(guesses, "中文字符集字节", 97, charset + " 严格解码得到中文：“" + Preview(decoded) + "”"); }
            if (source.IndexOf("=?", StringComparison.Ordinal) >= 0 && source.IndexOf("?=", StringComparison.Ordinal) >= 0) AddGuess(guesses, "中文传输格式", 98, "符合 MIME encoded-word 边界");
            if (LooksLikeQrMatrix(source)) AddGuess(guesses, "QR Code", 99, "包含完整的 21×21 二进制矩阵");
            string bitPayload = LabeledBitPayload(source); if (bitPayload.Length == 95 && bitPayload.StartsWith("101", StringComparison.Ordinal) && bitPayload.EndsWith("101", StringComparison.Ordinal) && bitPayload.Substring(45, 5) == "01010") AddGuess(guesses, "条形码", 99, "95 位位串具有 EAN-13 守护条结构");
            if (LooksLikeFiveBitCode(source)) AddGuess(guesses, "博多码 ITA2", 94, "由成组的五单位二进制代码组成");
            string base64Plain; if (onlyBase64 && TryReadableBase64(compact, compact.IndexOf('-') >= 0 || compact.IndexOf('_') >= 0, out base64Plain)) AddGuess(guesses, compact.IndexOf('-') >= 0 || compact.IndexOf('_') >= 0 ? "Base64URL" : "Base64", 94, "字符集、分组和实际解码结果均有效");
            if (onlyBase32 && compact.IndexOfAny(new[] { '2','3','4','5','6','7' }) >= 0) AddGuess(guesses, "Base32", 90, "字符集为 A–Z、2–7 和填充符");
            if (source.IndexOf("<~", StringComparison.Ordinal) >= 0 && source.IndexOf("~>", StringComparison.Ordinal) > source.IndexOf("<~", StringComparison.Ordinal)) AddGuess(guesses, "ASCII85", 98, "包含 ASCII85 的 <~ ~> 边界");
            if (source.IndexOf("xn--", StringComparison.OrdinalIgnoreCase) >= 0) AddGuess(guesses, "Punycode", 97, "包含 xn-- 国际化域名标签");
            if (ContainsQuotedPrintable(source)) AddGuess(guesses, "Quoted-Printable", 94, "包含有效的 =HH 字节转义");
            if (LooksLikeBase58(compact)) AddGuess(guesses, "Base58", 76, "字符全部位于 Base58 字母表并同时含字母与数字");
            if (source.IndexOf("%", StringComparison.Ordinal) >= 0 && ContainsPercentEscape(source)) AddGuess(guesses, "URL 编码", 96, "包含有效的百分号十六进制转义");
            if (source.IndexOf("\\u", StringComparison.OrdinalIgnoreCase) >= 0) AddGuess(guesses, "Unicode 转义", 96, "包含 \\uXXXX 转义序列");
            if (source.IndexOf('&') >= 0 && source.IndexOf(';') > source.IndexOf('&')) AddGuess(guesses, "HTML 实体", 88, "包含 &...; 字符引用");
            if (braille >= Math.Max(2, compact.Length * 3 / 4)) AddGuess(guesses, "盲文（英语一级）", 97, "主要由 Unicode 盲文点阵组成");
            if (LooksLikeColors(source)) AddGuess(guesses, "颜色编码", 95, "包含连续的 #RRGGBB 颜色字节组");
            if (CountRange(source, '\u2190', '\u21FF') >= Math.Max(3, compact.Length / 2)) AddGuess(guesses, "旗语", 91, "主要由方向箭头符号组成");
            if (CountPigpen(source) >= Math.Max(3, compact.Length / 2)) AddGuess(guesses, "猪圈密码符号", 91, "主要由猪圈图形符号组成");
            bool onlyMorse = symbols.Count > 0; foreach (char c in symbols) if (c != '.' && c != '-' && c != '/' && c != '|') onlyMorse = false;
            bool onlyAdfgx = letters.Length > 0; foreach (char c in letters) if ("ADFGX".IndexOf(c) < 0) onlyAdfgx = false;
            bool onlyAdfgvx = letters.Length > 0; foreach (char c in letters) if ("ADFGVX".IndexOf(c) < 0) onlyAdfgvx = false;
            bool onlyAb = letters.Length > 0; foreach (char c in letters) if (c != 'A' && c != 'B') onlyAb = false;
            if (onlyMorse) AddGuess(guesses, "Morse", 99, "字符集为点、划和分隔符");
            if (onlyAb && letters.Length % 5 == 0) AddGuess(guesses, "培根", 98, "字符集为 A/B，长度是 5 的倍数");
            if (onlyAdfgvx && upper.IndexOf('V') >= 0 && letters.Length % 2 == 0) AddGuess(guesses, "ADFGVX", 96, "字符集为 ADFGVX");
            else if (onlyAdfgx && letters.Length % 2 == 0) AddGuess(guesses, "ADFGX", 96, "字符集为 ADFGX");
            if (digits.Length >= 8 && letters.Length == 0)
            {
                List<string> tokens = NumberTokens(source); int hyphens = Count(source, '-');
                if (FixedTokenWidth(tokens, 4) >= 0.85 && tokens.Count >= 2) AddGuess(guesses, "中文电报码", 96, "数字按四位十进制电报码分组");
                if (hyphens >= 3 && TokensInRange(tokens, 1, 26)) AddGuess(guesses, "A1Z26", 95, "连字符数字序列全部位于 1–26");
                if (AllDigitsInRange(digits, 1, 5) && digits.Length % 2 == 0) AddGuess(guesses, "Polybius / Tap Code", 93, "数字坐标只使用 1–5，坐标总数为偶数");
                if (FixedTokenWidth(tokens, 5) >= 0.85) AddGuess(guesses, "VIC", 91, "数字按五位消息组排列");
                if (FixedTokenWidth(tokens, 1) >= 0.85 && TokensInRange(tokens, 1, 9)) AddGuess(guesses, "Morbit / Pollux", 84, "主要由一位数字符号组成");
                if (FixedTokenWidth(tokens, 2) >= 0.75)
                {
                    if (LeadingZeroRatio(tokens) > 0.03) AddGuess(guesses, "同音替换 / Grandpré", 87, "两位数字符号中包含前导零");
                    if (TokensInRange(tokens, 22, 110)) AddGuess(guesses, "Nihilist", 86, "两至三位数值位于 Polybius 坐标相加范围");
                }
                if (TokenWidthVariation(tokens) >= 3 && HasPunctuation(source)) AddGuess(guesses, "跨行棋盘", 83, "数字段长度随字符编码变化并保留文本边界");
            }
            if (source == source.ToLowerInvariant() && LooksLikeInputCodeSequence(source)) { string scheme; int hits = ChineseCodeTables.BestMatch(source, out scheme); int tokens = source.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Length; if (hits >= 3 && hits * 4 >= tokens * 3) AddGuess(guesses, "中文输入法码", 91, scheme + " 码表命中 " + hits + "/" + tokens + " 个输入码"); }
            int fanqieTotal, fanqieHits = FanqieWorkbench.MatchCount(source, out fanqieTotal); if (fanqieTotal >= 2 && fanqieHits * 4 >= fanqieTotal * 3 && Digits(source).Length >= fanqieTotal / 2) AddGuess(guesses, "反切码", 94, "声母代表字、韵母代表字和数字声调命中 " + fanqieHits + "/" + fanqieTotal + " 组");
        }

        private static string RemoveWhitespace(string value) { StringBuilder result = new StringBuilder(); foreach (char c in value ?? string.Empty) if (!char.IsWhiteSpace(c)) result.Append(c); return result.ToString(); }
        private static string ClueHint(string value) { string source = (value ?? string.Empty).Trim(); foreach (string raw in source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)) { string line = raw.Trim(); if (line.StartsWith("算法：", StringComparison.Ordinal) || line.StartsWith("算法:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase)) return line.Substring(line.IndexOfAny(new[] { '：', ':' }) + 1).Trim(); } return source.IndexOf('\n') < 0 && !source.StartsWith("明文：", StringComparison.Ordinal) && !source.StartsWith("明文:", StringComparison.OrdinalIgnoreCase) ? source : string.Empty; }
        private static bool HasDigitAndHexLetter(string value) { bool digit = false, letter = false; foreach (char c in value) { if (char.IsDigit(c)) digit = true; else if (Uri.IsHexDigit(c)) letter = true; } return digit && letter; }
        private static bool ContainsPercentEscape(string value) { for (int i = 0; i + 2 < value.Length; i++) if (value[i] == '%' && Uri.IsHexDigit(value[i + 1]) && Uri.IsHexDigit(value[i + 2])) return true; return false; }
        private static bool ContainsQuotedPrintable(string value) { for (int i = 0; i + 2 < value.Length; i++) if (value[i] == '=' && Uri.IsHexDigit(value[i + 1]) && Uri.IsHexDigit(value[i + 2])) return true; return false; }
        private static bool OnlyCharacters(string value, string allowed) { if (value.Length == 0) return false; foreach (char c in value) if (allowed.IndexOf(c) < 0) return false; return true; }
        private static bool LooksLikeQrMatrix(string source) { int rows = 0; foreach (string raw in (source ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)) { string line = raw.Trim(); if (line.Length == 21 && OnlyCharacters(line, "01")) rows++; } return rows == 21; }
        private static string LabeledBitPayload(string source) { string value = source ?? string.Empty; int at = value.IndexOf("位串：", StringComparison.Ordinal); if (at >= 0) value = value.Substring(at + 3).Split(new[] { '\r', '\n' })[0]; StringBuilder bits = new StringBuilder(); foreach (char c in value) if (c == '0' || c == '1') bits.Append(c); else if (!char.IsWhiteSpace(c) && at < 0) return string.Empty; return bits.ToString(); }
        private static bool LooksLikeFiveBitCode(string source) { string[] parts = (source ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', '-' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 2) return false; foreach (string part in parts) if (part.Length != 5 || !OnlyCharacters(part, "01")) return false; return true; }
        private static bool LooksLikeBase58(string value) { if (value.Length < 12) return false; const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"; bool digit = false, letter = false; foreach (char c in value) { if (alphabet.IndexOf(c) < 0) return false; if (char.IsDigit(c)) digit = true; if (char.IsLetter(c)) letter = true; } return digit && letter; }
        private static bool LooksLikeColors(string source) { int found = 0; for (int i = 0; i + 6 < source.Length; i++) if (source[i] == '#' && Uri.IsHexDigit(source[i + 1]) && Uri.IsHexDigit(source[i + 2]) && Uri.IsHexDigit(source[i + 3]) && Uri.IsHexDigit(source[i + 4]) && Uri.IsHexDigit(source[i + 5]) && Uri.IsHexDigit(source[i + 6])) { found++; i += 6; } return found >= 2; }
        private static int CountRange(string source, char first, char last) { int count = 0; foreach (char c in source ?? string.Empty) if (c >= first && c <= last) count++; return count; }
        private static int CountPigpen(string source) { const string symbols = "⌜⌝⌞⌟┌┐└┘⊢⊣⊤⊥×◇◆△▽"; int count = 0; foreach (char c in source ?? string.Empty) if (symbols.IndexOf(c) >= 0) count++; return count; }
        private static bool TryReadableBase64(string value, bool url, out string plain) { plain = string.Empty; try { string encoded = url ? value.Replace('-', '+').Replace('_', '/') : value; encoded += new string('=', (4 - encoded.Length % 4) % 4); byte[] bytes = Convert.FromBase64String(encoded); plain = new UTF8Encoding(false, true).GetString(bytes); if (plain.Length < 3) return false; int printable = 0; foreach (char c in plain) if (!char.IsControl(c) || char.IsWhiteSpace(c)) printable++; return printable >= plain.Length * .9; } catch { return false; } }
        private static bool TryChineseCharset(string hex, out string charset, out string decoded) { string[] choices = { "GB18030", "GBK / CP936", "GB2312 / EUC-CN", "Big5 / CP950" }; charset = string.Empty; decoded = string.Empty; int best = 0; foreach (string choice in choices) try { string text = TransferEncoding.CharsetBytes(hex, choice, true); int han = 0; foreach (char c in text) if (c >= '\u3400' && c <= '\u9FFF') han++; if (han > best) { best = han; charset = choice; decoded = text; } } catch { } return best >= Math.Max(1, decoded.Length / 2); }
        private static string Preview(string value) { string text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " "); return text.Length > 24 ? text.Substring(0, 24) + "…" : text; }
        private static bool LooksLikeInputCodeSequence(string source) { string[] tokens = (source ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries); if (tokens.Length < 3 || tokens.Length > 64) return false; foreach (string raw in tokens) foreach (string token in raw.Split('/')) { if (token.Length < 1 || token.Length > 6) return false; foreach (char c in token) if (!char.IsLetterOrDigit(c) && c != ';' && c != '\'') return false; } return true; }

        private static TransformProbe ProbeSimpleSubstitution(string source, double sourceScore, string matchMethod)
        {
            AffineCipher cipher = new AffineCipher(); TransformProbe best = null;
            for (int a = 1; a < 26; a++)
            {
                if (GreatestCommonDivisor(a, 26) != 1) continue;
                for (int b = 0; b < 26; b++)
                {
                    if (a == 1 && b == 0) continue;
                    string key = a.ToString(CultureInfo.InvariantCulture) + "," + b.ToString(CultureInfo.InvariantCulture); string plain;
                    try { plain = cipher.Decrypt(source, key); } catch (CipherException) { continue; }
                    string language; double score = BestLanguageScore(Letters(plain), out language, matchMethod);
                    if (best == null || score > best.PlainScore)
                    {
                        string name = a == 1 ? "凯撒 / ROT13" : a == 25 ? "Atbash / 仿射" : b == 0 ? "Multiplicative / 仿射" : "仿射";
                        best = new TransformProbe { Name = name, Key = key, Language = language, PlainScore = score, Gain = score - sourceScore };
                    }
                }
            }
            return best != null && best.PlainScore >= 0.10 && best.Gain >= 0.10 ? best : null;
        }

        private static TransformProbe ProbeSimpleTranspositions(string source, double sourceScore, string matchMethod)
        {
            TransformProbe best = null; RailFenceCipher rail = new RailFenceCipher(); ScytaleCipher scytale = new ScytaleCipher(); CaesarBoxCipher box = new CaesarBoxCipher();
            ConsiderTransform(ref best, "Reverse", "反转", ReverseCipher.Transform(source), sourceScore, matchMethod);
            for (int value = 2; value <= Math.Min(30, source.Length / 2); value++)
            {
                ConsiderTransform(ref best, "栅栏", value.ToString(CultureInfo.InvariantCulture), rail.Decrypt(source, value.ToString(CultureInfo.InvariantCulture)), sourceScore, matchMethod);
                ConsiderTransform(ref best, "Scytale / Caesar Box", value.ToString(CultureInfo.InvariantCulture), scytale.Decrypt(source, value.ToString(CultureInfo.InvariantCulture)), sourceScore, matchMethod);
                ConsiderTransform(ref best, "Scytale / Caesar Box", value.ToString(CultureInfo.InvariantCulture), box.Decrypt(source, value.ToString(CultureInfo.InvariantCulture)), sourceScore, matchMethod);
                if (source.Length % value == 0) ConsiderTransform(ref best, "路线换位", value.ToString(CultureInfo.InvariantCulture), RouteCipher.Decrypt(source, value.ToString(CultureInfo.InvariantCulture)), sourceScore, matchMethod);
            }
            for (int rails = 2; rails <= Math.Min(10, source.Length / 2); rails++) for (int offset = 1; offset < 2 * rails - 2; offset++)
                ConsiderTransform(ref best, "Redefence", rails + "," + offset, RedefenceCipher.Decrypt(source, rails.ToString(CultureInfo.InvariantCulture), offset.ToString(CultureInfo.InvariantCulture)), sourceScore, matchMethod);
            return best != null && best.PlainScore >= 0.10 && best.Gain >= 0.10 ? best : null;
        }

        private static void ConsiderTransform(ref TransformProbe best, string name, string key, string plain, double sourceScore, string matchMethod)
        {
            string language; double score = BestLanguageScore(Letters(plain), out language, matchMethod);
            if (best == null || score > best.PlainScore) best = new TransformProbe { Name = name, Key = key, Language = language, PlainScore = score, Gain = score - sourceScore };
        }

        private static PeriodicProbe ProbePeriodic(string text, double globalIc, string matchMethod)
        {
            string[] languages = Languages(); int maximum = Math.Min(20, Math.Max(2, text.Length / 10)); Dictionary<int, int> kasiski = KasiskiFactorVotes(text, maximum); PeriodicProbe best = null; PortaCipher porta = new PortaCipher();
            for (int period = 2; period <= maximum; period++)
            {
                double average = 0; for (int column = 0; column < period; column++) average += ColumnIc(text, column, period); average /= period;
                if (average < 0.052 || average - globalIc < 0.004) continue;
                foreach (string language in languages)
                {
                    int votes = kasiski.ContainsKey(period) ? kasiski[period] : 0;
                    StringBuilder additiveKey = new StringBuilder(); for (int column = 0; column < period; column++) additiveKey.Append((char)('A' + BestShift(text, column, period, language)));
                    string additivePlain = DecodeVigenere(text, additiveKey.ToString()); string detected; double additiveScore = BestLanguageScore(additivePlain, out detected, matchMethod);
                    bool numeric = true; StringBuilder numericKey = new StringBuilder(); foreach (char value in additiveKey.ToString()) { int shift = value - 'A'; if (shift > 9) numeric = false; else numericKey.Append((char)('0' + shift)); }
                    ConsiderPeriodic(ref best, numeric ? "Gronsfeld / 维吉尼亚" : "维吉尼亚 / Variant Beaufort", period, numeric ? numericKey.ToString() : ReduceRepeatedKey(additiveKey.ToString()), detected, average, votes, additiveScore);

                    StringBuilder beaufortKey = new StringBuilder(); for (int column = 0; column < period; column++) beaufortKey.Append((char)('A' + BestBeaufortShift(text, column, period, language)));
                    string beaufortPlain = DecodeBeaufort(text, beaufortKey.ToString()); double beaufortScore = BestLanguageScore(beaufortPlain, out detected, matchMethod);
                    ConsiderPeriodic(ref best, "Beaufort", period, ReduceRepeatedKey(beaufortKey.ToString()), detected, average, votes, beaufortScore);

                    StringBuilder portaKey = new StringBuilder(); for (int column = 0; column < period; column++) portaKey.Append((char)('A' + BestPortaGroup(text, column, period, language, porta) * 2));
                    string portaPlain = Letters(porta.Decrypt(text, portaKey.ToString())); double portaScore = BestLanguageScore(portaPlain, out detected, matchMethod);
                    ConsiderPeriodic(ref best, "Porta", period, ReduceRepeatedKey(portaKey.ToString()), detected, average, votes, portaScore);
                }
            }
            if (best == null || best.AverageIc < 0.055 || best.AverageIc - globalIc < 0.006 || best.PlainScore < 0.08) return null; return best;
        }

        private static void ConsiderPeriodic(ref PeriodicProbe best, string name, int period, string key, string language, double average, int votes, double score)
        {
            double quality = score + (average * 2.0) + Math.Min(0.20, votes * 0.002) - period * 0.002;
            if (best == null || quality > best.Quality) best = new PeriodicProbe { Name = name, Period = period, Key = key, Language = language, AverageIc = average, KasiskiVotes = votes, PlainScore = score, Quality = quality };
        }
        private static int BestShift(string text, int column, int period, string language)
        {
            int bestShift = 0; double best = double.MaxValue; for (int shift = 0; shift < 26; shift++) { int[] counts = new int[26]; int total = 0; for (int i = column; i < text.Length; i += period) { counts[Alphabet.Mod(text[i] - 'A' - shift, 26)]++; total++; } double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestShift = shift; } } return bestShift;
        }
        private static int BestBeaufortShift(string text, int column, int period, string language)
        {
            int bestShift = 0; double best = double.MaxValue; for (int shift = 0; shift < 26; shift++) { int[] counts = new int[26]; int total = 0; for (int i = column; i < text.Length; i += period) { counts[Alphabet.Mod(shift - (text[i] - 'A'), 26)]++; total++; } double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestShift = shift; } } return bestShift;
        }
        private static int BestPortaGroup(string text, int column, int period, string language, PortaCipher cipher)
        {
            int bestGroup = 0; double best = double.MaxValue; for (int group = 0; group < 13; group++) { int[] counts = new int[26]; int total = 0; string key = ((char)('A' + group * 2)).ToString(); for (int i = column; i < text.Length; i += period) { string value = cipher.Decrypt(text[i].ToString(), key); counts[value[0] - 'A']++; total++; } double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestGroup = group; } } return bestGroup;
        }
        private static string DecodeVigenere(string text, string key) { StringBuilder result = new StringBuilder(text.Length); for (int i = 0; i < text.Length; i++) result.Append((char)('A' + Alphabet.Mod(text[i] - 'A' - (key[i % key.Length] - 'A'), 26))); return result.ToString(); }
        private static string DecodeBeaufort(string text, string key) { StringBuilder result = new StringBuilder(text.Length); for (int i = 0; i < text.Length; i++) result.Append((char)('A' + Alphabet.Mod((key[i % key.Length] - 'A') - (text[i] - 'A'), 26))); return result.ToString(); }
        private static double ColumnIc(string text, int column, int period) { int[] counts = new int[26]; int total = 0; for (int i = column; i < text.Length; i += period) { counts[text[i] - 'A']++; total++; } if (total < 2) return 0; double n = 0; foreach (int count in counts) n += count * (count - 1); return n / (total * (total - 1.0)); }
        private static Dictionary<int, int> KasiskiFactorVotes(string text, int maximum) { Dictionary<int, int> votes = new Dictionary<int, int>(); Dictionary<string, int> last = new Dictionary<string, int>(); for (int i = 0; i <= text.Length - 3; i++) { string gram = text.Substring(i, 3); int previous; if (last.TryGetValue(gram, out previous)) { int distance = i - previous; for (int factor = 2; factor <= maximum; factor++) if (distance % factor == 0) votes[factor] = votes.ContainsKey(factor) ? votes[factor] + 1 : 1; } last[gram] = i; } return votes; }
        private static string ReduceRepeatedKey(string key) { for (int length = 1; length <= key.Length / 2; length++) { if (key.Length % length != 0) continue; bool repeated = true; for (int i = length; i < key.Length; i++) if (key[i] != key[i % length]) { repeated = false; break; } if (repeated) return key.Substring(0, length); } return key; }

        private static double BestLanguageScore(string letters, out string language, string matchMethod)
        {
            language = "EN"; if (letters.Length == 0) return double.MinValue; language = LanguageModels.DetectLanguage(letters, matchMethod); return LanguageModels.TextScore(letters, language) / Math.Max(1.0, letters.Length);
        }
        private static string[] Languages() { return new[] { "EN", "FR", "DE", "ES", "IT", "PT", "NL", "SV", "PL", "TR" }; }
        private static int ConfidenceFromGain(double gain, int baseline) { return Math.Max(baseline, Math.Min(99, baseline + (int)Math.Round(gain * 28))); }
        private static void AddGuess(List<Guess> guesses, string name, int score, string reason) { foreach (Guess item in guesses) if (item.Name == name) { if (score > item.Score) { item.Score = score; item.Reason = reason; } return; } guesses.Add(new Guess(name, score, reason)); }
        private static string Digits(string source) { StringBuilder r = new StringBuilder(); foreach (char c in source) if (char.IsDigit(c)) r.Append(c); return r.ToString(); }
        private static string Letters(string source) { StringBuilder r = new StringBuilder(); foreach (char raw in source ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') r.Append(c); } return r.ToString(); }
        private static double Coincidence(string text) { int[] counts = new int[26]; foreach (char c in text) counts[c - 'A']++; double n = 0; foreach (int count in counts) n += count * (count - 1); return text.Length < 2 ? 0 : n / (text.Length * (text.Length - 1.0)); }
        private static int GreatestCommonDivisor(int a, int b) { while (b != 0) { int remainder = a % b; a = b; b = remainder; } return Math.Abs(a); }
        private static bool ContainsWhitespace(string source) { foreach (char c in source) if (char.IsWhiteSpace(c)) return true; return false; }
        private static bool ContainsOnlyLettersAndWhitespace(string source) { foreach (char raw in source) { char c = char.ToUpperInvariant(raw); if (!char.IsWhiteSpace(c) && (c < 'A' || c > 'Z')) return false; } return true; }
        private static bool HasPunctuation(string source) { foreach (char c in source) if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) return true; return false; }
        private static bool HasLowercase(string source) { foreach (char c in source) if (char.IsLower(c)) return true; return false; }
        private static int Count(string source, char target) { int count = 0; foreach (char c in source) if (c == target) count++; return count; }
        private static int UniqueLetters(string letters) { bool[] seen = new bool[26]; int count = 0; foreach (char c in letters) if (!seen[c - 'A']) { seen[c - 'A'] = true; count++; } return count; }
        private static double FormatCoherence(string source)
        {
            int checks = 0, good = 0;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (",.;:!?".IndexOf(c) >= 0) { checks++; bool previous = i > 0 && (char.IsLetterOrDigit(source[i - 1]) || source[i - 1] == '\''); bool next = i + 1 >= source.Length || char.IsWhiteSpace(source[i + 1]); if (previous && next) good++; }
                else if (c == '\'') { checks++; if (i > 0 && i + 1 < source.Length && char.IsLetter(source[i - 1]) && char.IsLetter(source[i + 1])) good++; }
                else if (char.IsUpper(c)) { checks++; int p = i - 1; while (p >= 0 && (source[p] == '\'' || source[p] == '"')) p--; if (p < 0 || source[p] == '\n' || source[p] == '\r' || ".!?".IndexOf(source[p]) >= 0) good++; }
            }
            return checks == 0 ? 0.5 : good / (double)checks;
        }
        private static List<string> NumberTokens(string source) { List<string> result = new List<string>(); StringBuilder token = new StringBuilder(); foreach (char c in source) { if (char.IsDigit(c)) token.Append(c); else if (token.Length > 0) { result.Add(token.ToString()); token.Length = 0; } } if (token.Length > 0) result.Add(token.ToString()); return result; }
        private static bool TokensInRange(List<string> tokens, int low, int high) { if (tokens.Count < 2) return false; foreach (string token in tokens) { int value; if (!int.TryParse(token, out value) || value < low || value > high) return false; } return true; }
        private static bool AllDigitsInRange(string digits, int low, int high) { foreach (char c in digits) { int value = c - '0'; if (value < low || value > high) return false; } return digits.Length > 0; }
        private static double FixedTokenWidth(List<string> tokens, int width) { if (tokens.Count == 0) return 0; int matches = 0; foreach (string token in tokens) if (token.Length == width) matches++; return matches / (double)tokens.Count; }
        private static double LeadingZeroRatio(List<string> tokens) { if (tokens.Count == 0) return 0; int matches = 0; foreach (string token in tokens) if (token.Length > 1 && token[0] == '0') matches++; return matches / (double)tokens.Count; }
        private static int TokenWidthVariation(List<string> tokens) { if (tokens.Count == 0) return 0; int minimum = int.MaxValue, maximum = 0; foreach (string token in tokens) { minimum = Math.Min(minimum, token.Length); maximum = Math.Max(maximum, token.Length); } return maximum - minimum; }
    }

    internal static class AnalysisWorkbench
    {
        private sealed class LanguageRow { internal string Name; internal double Score; }
        internal static string Analyze(string input, string language, string nText)
        {
            return Analyze(input, language, nText, "AUTO");
        }

        internal static string Analyze(string input, string language, string nText, string method)
        {
            List<string> units = UnicodeAnalysis.Units(input); if (units.Count < 2) throw new CipherException("至少需要 2 个可分析字符"); string letters = UnicodeAnalysis.LatinLetters(input);
            int n; if (!int.TryParse(nText, out n) || n < 1 || n > 8) n = 3;
            double ic = UnicodeAnalysis.Coincidence(units), entropy = UnicodeAnalysis.Entropy(units); string selectedMethod = LanguageModels.NormalizeMatchMethod(method, letters.Length), detected = letters.Length >= 2 ? LanguageModels.DetectLanguage(letters, selectedMethod) : string.Empty;
            StringBuilder result = new StringBuilder();
            result.AppendFormat(CultureInfo.InvariantCulture, "概览\r\n字符 {0}  不同字符 {1}  IC {2:0.000000}  Shannon {3:0.000}\r\n", units.Count, UnicodeAnalysis.Different(units), ic, entropy);
            result.Append("文字体系：").Append(UnicodeAnalysis.ScriptSummary(input)).Append("\r\n");
            result.Append("语言推测：").Append(detected.Length > 0 ? detected : "—");
            if (detected.Length > 0) result.Append("（").Append(LanguageModels.MatchMethodLabel(selectedMethod, letters.Length)).Append("）");
            if (!string.IsNullOrWhiteSpace(language) && !string.Equals(language, "AUTO", StringComparison.OrdinalIgnoreCase)) result.Append("（评分目标 ").Append(LanguageModels.Normalize(language)).Append('）');
            result.Append("\r\n结构判断：").Append(ic > 0.058 ? "偏向单表或换位" : ic < 0.050 ? "偏向多表或高扩散" : "信息不足").Append("\r\n\r\n");
            if (letters.Length >= 2)
            {
                List<LanguageRow> rows = new List<LanguageRow>(); foreach (string candidate in LanguageModels.SupportedLanguages()) rows.Add(new LanguageRow { Name = candidate, Score = LanguageModels.LanguageMatchScore(letters, candidate, selectedMethod) }); rows.Sort(delegate(LanguageRow a, LanguageRow b) { return b.Score.CompareTo(a.Score); }); result.Append("语言匹配（").Append(LanguageModels.MatchMethodLabel(selectedMethod, letters.Length)).Append("）\r\n"); for (int i = 0; i < rows.Count; i++) result.AppendFormat(CultureInfo.InvariantCulture, "{0,2}  {1,-3}  {2:0.000000}\r\n", i + 1, rows[i].Name, rows[i].Score); result.Append("\r\n");
            }
            result.Append(UnicodeAnalysis.Frequency(input)).Append("\r\n");
            result.Append("前 100 个 ").Append(n).Append("-gram\r\n").Append(UnicodeAnalysis.Ngrams(input, n)).Append("\r\n");
            result.Append("周期 IC\r\n");
            for (int period = 1; period <= Math.Min(20, units.Count / 2); period++)
            {
                double average = 0; for (int column = 0; column < period; column++) average += UnicodeAnalysis.ColumnIc(units, column, period); average /= period;
                result.AppendFormat(CultureInfo.InvariantCulture, "{0,2}  {1:0.000000}  {2}\r\n", period, average, Bar(average));
            }
            return result.ToString().TrimEnd();
        }
        private static string Bar(double value) { int length = Math.Max(0, Math.Min(24, (int)Math.Round(value * 240))); return new string('■', length); }
    }

    internal static class CribAnalysis
    {
        internal static string Analyze(string input, string crib, string algorithm)
        {
            string cipher = Letters(input), plain = Letters(crib); if (plain.Length == 0) throw new CipherException("请输入已知明文片段"); if (cipher.Length < plain.Length) throw new CipherException("已知明文长于密文");
            string mode = (algorithm ?? string.Empty).Trim().ToUpperInvariant(); StringBuilder result = new StringBuilder(); int rank = 1;
            for (int offset = 0; offset <= cipher.Length - plain.Length; offset++)
            {
                StringBuilder key = new StringBuilder(); Dictionary<char, char> mapping = new Dictionary<char, char>(); bool conflict = false;
                for (int i = 0; i < plain.Length; i++)
                {
                    key.Append((char)('A' + Alphabet.Mod(cipher[offset + i] - plain[i], 26)));
                    char c = cipher[offset + i], p = plain[i]; if (mapping.ContainsKey(c) && mapping[c] != p) conflict = true; else mapping[c] = p;
                }
                string caesar = AllSame(key.ToString()) ? "，凯撒位移 " + (key[0] - 'A') : string.Empty;
                if (mode.Length == 0 || mode.IndexOf("VIG") >= 0 || mode.IndexOf("维") >= 0 || mode.IndexOf("CAES") >= 0 || mode.IndexOf("凯") >= 0)
                    result.AppendFormat("#{0}  位置 {1}  密钥片段 {2}{3}\r\n", rank++, offset, key, caesar);
                if (!conflict && (mode.Length == 0 || mode.IndexOf("SUB") >= 0 || mode.IndexOf("替") >= 0))
                {
                    result.Append("    单表映射 "); foreach (KeyValuePair<char, char> pair in mapping) result.Append(pair.Key).Append('=').Append(pair.Value).Append(' '); result.AppendLine();
                }
                result.AppendLine();
                if (rank > 30) break;
            }
            return result.ToString().TrimEnd();
        }
        private static bool AllSame(string text) { for (int i = 1; i < text.Length; i++) if (text[i] != text[0]) return false; return text.Length > 0; }
        private static string Letters(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') r.Append(c); } return r.ToString(); }
    }
}

