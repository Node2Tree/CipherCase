using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using ClassicalCipherToolbox.Core;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class UniversalCracker
    {
        private sealed class Job { internal ICryptoTool Tool; internal ToolMode Mode; internal string Label; internal int Compatibility; internal Dictionary<string, string> Values; }
        private sealed class Result { internal string Type; internal string Plain; internal string Parameter; internal string Detail; internal double Natural; internal int Compatibility; internal double Combined; internal long Milliseconds; }
        private sealed class ClueInfo { internal string Algorithm = string.Empty; internal string Plaintext = string.Empty; }

        private static readonly HashSet<string> DeepTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Two-square", "Four-square", "Trifid", "双重列换位", "Ubchi", "同音替换", "Fractionated Morse", "Nihilist", "跨行棋盘", "Bifid", "Polybius", "Three-square", "Digrafid" };
        private static readonly HashSet<string> FastTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "自动解码", "凯撒", "仿射", "ROT-N", "栅栏", "维吉尼亚", "Beaufort", "Variant Beaufort", "Porta", "Gronsfeld", "渐进凯撒", "Scytale", "Caesar Box", "Redefence", "路线换位", "Morse", "A1Z26", "Tap Code", "培根", "ROT13", "Atbash", "单表替换", "中文电码加密", "中文隐写分析" };
        private static readonly HashSet<string> DirectTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Morse", "A1Z26", "Tap Code", "培根", "ROT13", "Atbash" };
        private static readonly HashSet<string> IdentifiedDecodeTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Base64", "Base64URL", "Base32", "Base58", "ASCII85", "十六进制", "二进制", "URL 编码", "Unicode 转义", "HTML 实体", "Quoted-Printable", "Punycode", "字符集字节", "盲文（英语一级）", "博多码 ITA2", "中文电报码", "反切码", "猪圈密码符号", "旗语", "条形码", "QR Code", "颜色编码" };
        private const string CommonChinese = "的一是不了在人有我他这中大来上个国到说们为子和你地出道也时年得就那要下以生会自着去之过家学对可她里后小么心多天而能好都然没日于起还发成事只作当想看文无开手十用主行方又如前所本见经头面公同三已老从动两长知民样现分将外但身些与高意进把法此实回二理力它应女种教工使便度明性先名情加化太战间真话利因很定表最向全相点新内数正反原比或质气第命变条结解问建月系军者立代通并提直题程展果料象员位入常总次品式活设及管特件求基资边流路级少图山统接较组计别";

        internal static string Crack(ToolRequest request)
        {
            string input = request.Input ?? string.Empty, language = NormalizeLanguage(request.Get("language")), effortText = (request.Get("effort") ?? string.Empty).Trim(); int effort = effortText == "深入" || effortText.Equals("DEEP", StringComparison.OrdinalIgnoreCase) ? 2 : effortText == "快速" || effortText.Equals("FAST", StringComparison.OrdinalIgnoreCase) ? 0 : 1; ClueInfo clue = ParseClue(request.Get("clue"));
            string identifier; try { identifier = CipherIdentifier.Identify(input, clue.Algorithm, "AUTO"); } catch { identifier = string.Empty; }
            List<Job> jobs = CreateJobs(input, language, effort, clue, identifier); List<Result> results = new List<Result>(); object gate = new object(); int cursor = 0, completed = 0;
            Result original = BuildResult("原文", input, string.Empty, "未变换的输入，用作自然度基线", 45, 0, input, language, clue.Plaintext); results.Add(original); request.ReportPartial(Format(results, completed, jobs.Count, identifier));
            Action worker = delegate
            {
                while (true)
                {
                    Job job; lock (gate) { if (cursor >= jobs.Count) return; job = jobs[cursor++]; } if (request.IsCancellationRequested) return; Stopwatch watch = Stopwatch.StartNew(); string output = null;
                    try { output = job.Tool.Execute(new ToolRequest(job.Mode, input, job.Values, null, delegate { return request.IsCancellationRequested; })); } catch { }
                    watch.Stop(); List<Result> parsed = Parse(job, output, input, language, clue.Plaintext, effort, watch.ElapsedMilliseconds); string partial;
                    lock (gate) { foreach (Result item in parsed) Add(results, item); completed++; partial = Format(results, completed, jobs.Count, identifier); }
                    request.ReportProgress(jobs.Count == 0 ? 100 : completed * 100 / jobs.Count, "通用破解 · " + job.Label + " · " + completed + "/" + jobs.Count); request.ReportPartial(partial);
                }
            };
            int workers = Math.Min(2, Math.Max(1, jobs.Count)); Task[] tasks = new Task[workers]; for (int i = 0; i < workers; i++) tasks[i] = Task.Factory.StartNew(worker); Task.WaitAll(tasks); request.ThrowIfCancellationRequested(); lock (gate) return Format(results, completed, jobs.Count, identifier);
        }

        private static List<Job> CreateJobs(string input, string language, int effort, ClueInfo clue, string identifier)
        {
            List<Job> jobs = new List<Job>(); foreach (ICryptoTool tool in ToolRegistry.CreateAll())
            {
                if (tool.Name == "通用破解" || tool.Name == "密码识别器" || tool.Name == "Crib 工具") continue; int tier = DeepTools.Contains(tool.Name) ? 2 : FastTools.Contains(tool.Name) ? 0 : 1, compatibility = Compatibility(tool.Name, identifier, input); bool forced = AlgorithmHintMatches(tool.Name, clue.Algorithm), promoted = IsPromotedIdentification(tool.Name, identifier); if ((tier > effort && !forced && !promoted) || (!forced && !promoted && !Eligible(tool.Name, input))) continue; if (forced) compatibility = Math.Max(compatibility, 100); int jobEffort = tier > effort ? tier : effort;
                if (tool.Modes.Contains(ToolMode.Crack) && HasRequired(tool, ToolMode.Crack, clue.Plaintext))
                {
                    Dictionary<string, string> values = DefaultValues(language, jobEffort, clue.Plaintext); jobs.Add(new Job { Tool = tool, Mode = ToolMode.Crack, Label = tool.Name, Compatibility = compatibility, Values = values });
                    if (tool.Name == "单表替换" && LooksChineseCarrier(input) && language != "ZH") { Dictionary<string, string> chinese = DefaultValues("ZH", jobEffort, clue.Plaintext); jobs.Add(new Job { Tool = tool, Mode = ToolMode.Crack, Label = "单表替换·ZH", Compatibility = Math.Max(92, compatibility), Values = chinese }); }
                }
                else if (DirectTools.Contains(tool.Name) || ((promoted || forced) && IdentifiedDecodeTools.Contains(tool.Name)))
                {
                    ToolMode directMode = tool.Modes.Contains(ToolMode.Decrypt) ? ToolMode.Decrypt : tool.Modes.Contains(ToolMode.Decode) ? ToolMode.Decode : ToolMode.Analyze;
                    if (directMode != ToolMode.Analyze && HasRequired(tool, directMode, clue.Plaintext)) jobs.Add(new Job { Tool = tool, Mode = directMode, Label = tool.Name, Compatibility = compatibility, Values = DefaultValues(language, jobEffort, clue.Plaintext) });
                }
            }
            jobs.Sort(delegate(Job a, Job b) { int score = b.Compatibility.CompareTo(a.Compatibility); return score != 0 ? score : string.CompareOrdinal(a.Label, b.Label); }); return jobs;
        }

        private static ClueInfo ParseClue(string raw)
        {
            ClueInfo result = new ClueInfo(); string value = (raw ?? string.Empty).Trim(); if (value.Length == 0) return result;
            string[] lines = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries); StringBuilder plain = new StringBuilder();
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim(), content;
                if (TakePrefix(line, new[] { "算法:", "算法：", "算法=", "TYPE:", "TYPE=" }, out content)) result.Algorithm = content;
                else if (TakePrefix(line, new[] { "明文:", "明文：", "明文=", "CRIB:", "CRIB=" }, out content)) { if (plain.Length > 0) plain.Append(' '); plain.Append(content); }
                else if (lines.Length == 1 && LooksLikeAlgorithmName(line)) result.Algorithm = line;
                else { if (plain.Length > 0) plain.Append(' '); plain.Append(line); }
            }
            result.Plaintext = plain.ToString().Trim(); return result;
        }

        private static bool TakePrefix(string value, string[] prefixes, out string content)
        {
            foreach (string prefix in prefixes) if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { content = value.Substring(prefix.Length).Trim(); return true; } content = string.Empty; return false;
        }

        private static bool LooksLikeAlgorithmName(string value)
        {
            foreach (ICryptoTool tool in ToolRegistry.CreateAll()) if (value.Equals(tool.Name, StringComparison.OrdinalIgnoreCase)) return true;
            return value.Equals("Vigenere", StringComparison.OrdinalIgnoreCase) || value.Equals("Rail Fence", StringComparison.OrdinalIgnoreCase) || value == "分数化摩尔斯" || value == "换位密码";
        }

        private static bool HasRequired(ICryptoTool tool, ToolMode mode, string clue)
        {
            foreach (ToolParameter parameter in tool.Parameters) if (parameter.AppliesTo(mode) && parameter.Required && !(parameter.Id == "crib" && clue.Length > 0)) return false; return true;
        }

        private static Dictionary<string, string> DefaultValues(string language, int effort, string clue)
        {
            string iterations = effort == 0 ? "2500" : effort == 1 ? "12000" : "60000", max = effort == 0 ? "6" : effort == 1 ? "8" : "10"; if (language == "ZH") iterations = effort == 0 ? "10000" : effort == 1 ? "50000" : "250000";
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "language", language }, { "method", "NGRAM" }, { "heuristic", "自动" }, { "iterations", iterations }, { "restarts", effort == 0 ? "2" : effort == 1 ? "4" : "7" }, { "min", "2" }, { "max", max }, { "minperiod", "2" }, { "maxperiod", effort == 2 ? "18" : "10" }, { "wordlimit", effort == 0 ? "60" : effort == 1 ? "220" : "700" }, { "nullmax", effort == 2 ? "4" : "2" }, { "crib", clue }, { "size", "4" }, { "length", string.Empty }, { "partial", string.Empty }, { "locks", string.Empty } };
        }

        private static bool Eligible(string name, string input)
        {
            int letters = 0, digits = 0, nonSpace = 0; HashSet<char> unique = new HashSet<char>(); foreach (char raw in input) { if (char.IsWhiteSpace(raw)) continue; nonSpace++; unique.Add(char.ToUpperInvariant(raw)); if ((raw >= 'A' && raw <= 'Z') || (raw >= 'a' && raw <= 'z')) letters++; if (char.IsDigit(raw)) digits++; } string upper = input.ToUpperInvariant();
            if (name == "自动解码") return nonSpace >= 4; if (name == "中文电码加密") return digits >= 8 && digits == nonSpace; if (name == "中文隐写分析") return nonSpace >= 8 && (input.IndexOf('\n') >= 0 || letters < nonSpace / 2); if (name == "Morse") return Only(upper, ".-/| "); if (name == "A1Z26") return digits >= 4 && letters == 0; if (name == "Tap Code") return digits >= 8 && Only(upper, "12345 ./-|\r\n\t"); if (name == "培根") return letters >= 5 && Only(upper, "AB \r\n\t"); if (name == "Morbit" || name == "Pollux" || name == "Nihilist" || name == "跨行棋盘" || name == "同音替换") return digits >= Math.Max(20, nonSpace * 2 / 3); if (name == "ADFGX") return letters >= 30 && Only(upper, "ADFGX \r\n\t"); if (name == "ADFGVX") return letters >= 30 && Only(upper, "ADFGVX \r\n\t"); if (name == "Turning Grille") return letters == 16 || letters == 36; if (name == "Hill 2×2") return letters >= 40 && letters % 2 == 0; if (name == "单表替换") return letters >= 40 || (nonSpace >= 40 && unique.Count <= 26); if (name == "ROT13" || name == "Atbash") return letters >= 8; return letters >= 24;
        }

        private static bool Only(string text, string allowed) { foreach (char c in text) if (allowed.IndexOf(c) < 0) return false; return text.Length > 0; }
        private static bool LooksChineseCarrier(string input) { List<string> units = UnicodeAnalysis.Units(input); if (units.Count < 40) return false; int different = UnicodeAnalysis.Different(units); if (different == 16 && units.Count % 4 == 0) return true; return different <= 26 && UnicodeAnalysis.IsPredominantlyNonAscii(units); }

        private static int Compatibility(string name, string identifier, string input)
        {
            int best = 48; string[] lines = (identifier ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries); foreach (string line in lines) { if (line.IndexOf("类型 ", StringComparison.Ordinal) < 0 || !Related(name, line)) continue; int at = line.IndexOf("匹配 ", StringComparison.Ordinal); if (at >= 0) { int start = at + 3, end = start; while (start < line.Length && char.IsWhiteSpace(line[start])) start++; end = start; while (end < line.Length && char.IsDigit(line[end])) end++; int value; if (end > start && int.TryParse(line.Substring(start, end - start), out value)) best = Math.Max(best, value); } } if (name == "单表替换" && LooksChineseCarrier(input)) best = Math.Max(best, 92); return best;
        }
        private static bool IsPromotedIdentification(string name, string identifier)
        {
            int rank = 0; foreach (string line in (identifier ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.StartsWith("#", StringComparison.Ordinal) || line.IndexOf("类型 ", StringComparison.Ordinal) < 0) continue; rank++; if (rank > 3) return false; if (Related(name, line)) return true;
            }
            return false;
        }
        private static bool AlgorithmHintMatches(string name, string hint) { return hint.Length > 0 && (name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0 || Related(name, "类型 " + hint)); }
        private static bool Related(string name, string line) { if (line.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true; if (name == "中文电码加密" && line.IndexOf("中文电报码", StringComparison.Ordinal) >= 0) return true; if (name == "Vigenere" && line.IndexOf("维吉尼亚", StringComparison.Ordinal) >= 0) return true; if (name == "Rail Fence" && line.IndexOf("栅栏", StringComparison.Ordinal) >= 0) return true; if (name == "单表替换" && line.IndexOf("单表", StringComparison.Ordinal) >= 0) return true; if (name == "列换位" && line.IndexOf("换位", StringComparison.Ordinal) >= 0) return true; return false; }

        private static List<Result> Parse(Job job, string output, string input, string language, string clue, int effort, long milliseconds)
        {
            List<Result> result = new List<Result>(); if (string.IsNullOrWhiteSpace(output)) return result; List<string> blocks = Blocks(output); int limit = job.Compatibility >= 58 ? 12 : effort == 0 ? 2 : effort == 1 ? 4 : 6; for (int i = 0; i < blocks.Count && result.Count < limit; i++) { string plain = PlainText(blocks[i]); if (plain.Length < 2) continue; string header = FirstLine(blocks[i]), parameter = Field(header, "密钥 "), type = job.Label; if (parameter.Length == 0) parameter = Field(header, "参数 "); if (job.Tool.Name == "自动解码") { string decodedType = Field(header, "类型 "); if (decodedType.Length > 0) type = decodedType; } Result item = BuildResult(type, plain, parameter, blocks[i], job.Compatibility, milliseconds, input, language, clue); result.Add(item); } return result;
        }

        private static Result BuildResult(string type, string plain, string parameter, string detail, int compatibility, long milliseconds, string input, string language, string clue)
        {
            double natural = Naturalness(plain, language); if (type != "原文" && Compact(plain) == Compact(input)) natural = Math.Max(0, natural - 18); if (clue.Length > 0 && plain.IndexOf(clue, StringComparison.OrdinalIgnoreCase) >= 0) natural = Math.Min(98, natural + 8); double combined = natural * .76 + compatibility * .24; return new Result { Type = type, Plain = plain, Parameter = parameter, Detail = detail, Natural = natural, Compatibility = compatibility, Combined = combined, Milliseconds = milliseconds };
        }

        private static double Naturalness(string text, string requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0; int printable = 0, chinese = 0, common = 0, letters = 0; StringBuilder latin = new StringBuilder(); foreach (char raw in text) { if (!char.IsControl(raw) || char.IsWhiteSpace(raw)) printable++; if (raw >= '\u3400' && raw <= '\u9FFF') { chinese++; if (CommonChinese.IndexOf(raw) >= 0) common++; } char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') { letters++; latin.Append(c); } }
            double printableRatio = printable / (double)Math.Max(1, text.Length);
            if (chinese >= Math.Max(2, letters))
            {
                string selected; double languageScore = ChineseLanguageScoring.Score(text, "自动", out selected), ratio = chinese / (double)Math.Max(1, text.Length); return Math.Min(98, languageScore * .82 + ratio * 12 + printableRatio * 4);
            }
            if (letters < 3) return Math.Min(35, printableRatio * 25); string clean = latin.ToString(), language = requestedLanguage == "AUTO" || requestedLanguage == "ZH" ? LanguageModels.DetectLanguage(clean, "NGRAM") : LanguageModels.Normalize(requestedLanguage); double gram = language == "EN" && clean.Length >= 5 ? LanguageModels.SpacelessSubstitutionScore(clean, language) / clean.Length : LanguageModels.SubstitutionScore(clean, language) / clean.Length, cosine = LanguageModels.LanguageMatchScore(clean, language, "COSINE"), coverage = language == "EN" ? SpacelessWordSegmenter.Segment(clean).Coverage : 0, boundaries = HasWordBoundaries(text) ? 12 : 0;
            double frequencyPart = Clamp((cosine - .62) / .36 * 26), sequencePart = Clamp((gram + 9.0) / 4.5 * 28), rawScore = 7 + frequencyPart + sequencePart + coverage * 22 + boundaries + printableRatio * 4, lengthCap = 58 + 38 * (1 - Math.Exp(-clean.Length / 100.0)); return Math.Min(98, Math.Min(rawScore, lengthCap));
        }

        private static bool HasWordBoundaries(string text) { foreach (char c in text ?? string.Empty) if (char.IsWhiteSpace(c)) return true; return false; }

        private static void Add(List<Result> values, Result candidate) { string signature = Compact(candidate.Plain); for (int i = 0; i < values.Count; i++) if (Compact(values[i].Plain) == signature) { if (candidate.Combined > values[i].Combined) values[i] = candidate; Sort(values); return; } values.Add(candidate); Sort(values); if (values.Count > 60) values.RemoveAt(values.Count - 1); }
        private static void Sort(List<Result> values) { values.Sort(delegate(Result a, Result b) { int score = b.Combined.CompareTo(a.Combined); return score != 0 ? score : b.Natural.CompareTo(a.Natural); }); }
        private static string Format(List<Result> values, int completed, int total, string identifier)
        {
            string identified = FirstLine(identifier ?? string.Empty); List<Result> sorted = new List<Result>(values); sorted.Sort(delegate(Result a, Result b) { return b.Combined.CompareTo(a.Combined); }); StringBuilder output = new StringBuilder(); int limit = Math.Min(30, sorted.Count); for (int i = 0; i < limit; i++) { Result item = sorted[i]; if (i > 0) output.Append("\r\n\r\n"); output.Append('#').Append(i + 1).Append("  类型 ").Append(item.Type).Append("  语言分 ").Append(item.Natural.ToString("0.0", CultureInfo.InvariantCulture)).Append("  匹配 ").Append(item.Compatibility).Append("  综合 ").Append(item.Combined.ToString("0.0", CultureInfo.InvariantCulture)).Append("  耗时 ").Append(item.Milliseconds).Append("ms\r\n"); if (i == 0 && identified.Length > 0) output.Append("识别：").Append(identified).Append("\r\n"); if (!string.IsNullOrEmpty(item.Parameter)) output.Append("参数：").Append(item.Parameter).Append("\r\n"); output.Append("明文：").Append(item.Plain); } if (total > 0) output.Append("\r\n\r\n进度：").Append(completed).Append('/').Append(total); return output.ToString();
        }

        private static List<string> Blocks(string output) { List<string> result = new List<string>(); StringBuilder current = null; foreach (string line in (output ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)) { if (line.StartsWith("#", StringComparison.Ordinal) && line.Length > 1 && char.IsDigit(line[1])) { if (current != null) result.Add(current.ToString()); current = new StringBuilder(); } if (current != null) current.AppendLine(line); } if (current != null) result.Add(current.ToString()); if (result.Count == 0 && !string.IsNullOrWhiteSpace(output)) result.Add(output); return result; }
        private static string PlainText(string block)
        {
            string[] lines = (block ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None); string[] preferred = { "文本：", "原串：", "明文：" }; foreach (string prefix in preferred) foreach (string line in lines) if (line.StartsWith(prefix, StringComparison.Ordinal) && line.Length > prefix.Length) return line.Substring(prefix.Length).Trim(); StringBuilder plain = new StringBuilder(); for (int i = lines[0].StartsWith("#", StringComparison.Ordinal) ? 1 : 0; i < lines.Length; i++) { string line = lines[i].Trim(); if (line.Length == 0 || line.IndexOf("表：", StringComparison.Ordinal) >= 0 || line.StartsWith("分词：", StringComparison.Ordinal) || line.StartsWith("参数：", StringComparison.Ordinal) || line.StartsWith("位置：", StringComparison.Ordinal)) continue; if (plain.Length > 0) plain.Append(' '); plain.Append(line); } return plain.ToString().Trim();
        }
        private static string FirstLine(string text) { int at = text.IndexOfAny(new[] { '\r', '\n' }); return (at < 0 ? text : text.Substring(0, at)).Trim(); }
        private static string Field(string header, string label) { int start = header.IndexOf(label, StringComparison.Ordinal); if (start < 0) return string.Empty; start += label.Length; int end = header.IndexOf("  ", start, StringComparison.Ordinal); return (end < 0 ? header.Substring(start) : header.Substring(start, end - start)).Trim(); }
        private static string Compact(string value) { StringBuilder result = new StringBuilder(); foreach (char c in value ?? string.Empty) if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c)) result.Append(char.ToUpperInvariant(c)); return result.ToString(); }
        private static string NormalizeLanguage(string value) { string language = (value ?? string.Empty).Trim().ToUpperInvariant(); if (language == "中文" || language == "CHINESE" || language == "ZH-CN") return "ZH"; if (language == "ZH") return "ZH"; if (language == "AUTO" || language.Length == 0) return "AUTO"; return LanguageModels.Normalize(language); }
        private static double Clamp(double value) { return Math.Max(0, Math.Min(100, value)); }
    }
}

