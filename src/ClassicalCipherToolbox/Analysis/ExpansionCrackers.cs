using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class ExpansionCrackers
    {
        private sealed class Candidate { internal string Key; internal string Text; internal double Score; }
        private static readonly string[] Words = BuildWords();

        internal static string CrackBazeries(ToolRequest r)
        {
            Need(r.Input, 30, "Bazeries"); int min = Read(r.Get("minnumber"), 1, 1, 9999), max = Read(r.Get("maxnumber"), 999, min, 9999), wordLimit = Math.Min(Words.Length, Read(r.Get("wordlimit"), 1200, 50, Words.Length)); string language = Lang(r); List<Candidate> best = new List<Candidate>(); long total = (long)(max - min + 1) * (wordLimit + 1), done = 0;
            for (int number = min; number <= max; number++) for (int w = -1; w < wordLimit; w++) { if ((done++ & 511) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), "Bazeries · " + number); } string key = w < 0 ? string.Empty : Words[w]; string text = BazeriesCipher.Transform(r.Input, number.ToString(CultureInfo.InvariantCulture), key, true); Add(best, key + " / number=" + number, text, language); }
            r.ReportProgress(100, "Bazeries · 完成"); return Format(best);
        }

        internal static string CrackRagbaby(ToolRequest r)
        {
            Need(r.Input, 30, "Ragbaby"); int wordLimit = Math.Min(Words.Length, Read(r.Get("wordlimit"), 1500, 50, Words.Length)), minFirst = Read(r.Get("minfirst"), 1, 0, 23), maxFirst = Read(r.Get("maxfirst"), 23, minFirst, 23), minStep = Read(r.Get("minstep"), 0, 0, 23), maxStep = Read(r.Get("maxstep"), 23, minStep, 23); string language = Lang(r); List<Candidate> best = new List<Candidate>(); long total = (long)(wordLimit + 1) * (maxFirst - minFirst + 1) * (maxStep - minStep + 1), done = 0;
            for (int w = -1; w < wordLimit; w++) { string key = w < 0 ? string.Empty : Words[w]; for (int first = minFirst; first <= maxFirst; first++) for (int step = minStep; step <= maxStep; step++) { if ((done++ & 511) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), "Ragbaby · 搜索"); } string text = RagbabyCipher.Transform(r.Input, key, first.ToString(), step.ToString(), true); Add(best, (key.Length == 0 ? "标准字母表" : key) + " / first=" + first + " / step=" + step, text, language); } }
            r.ReportProgress(100, "Ragbaby · 完成"); return Format(best);
        }

        internal static string CrackAlberti(ToolRequest r)
        {
            Need(r.Input, 30, "Alberti"); int min = Read(r.Get("minperiod"), 1, 1, 50), max = Read(r.Get("maxperiod"), 20, min, 50), limit = Math.Min(Words.Length, Read(r.Get("wordlimit"), Words.Length, 50, Words.Length)); string language = Lang(r); List<Candidate> best = new List<Candidate>(); long total = (long)limit * (max - min + 1), done = 0;
            for (int w = 0; w < limit; w++) for (int period = min; period <= max; period++) { if ((done++ & 511) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), "Alberti · 周期 " + period); } string text = AlbertiCipher.Transform(r.Input, Words[w], period.ToString(), true); Add(best, Words[w] + " / period=" + period, text, language); }
            r.ReportProgress(100, "Alberti · 完成"); return Format(best);
        }

        internal static string CrackBellaso(ToolRequest r)
        {
            Need(r.Input, 30, "Bellaso"); string alphabetKey = r.Get("alphabet"), language = Lang(r); int limit = Math.Min(Words.Length, Read(r.Get("wordlimit"), Words.Length, 50, Words.Length)); List<Candidate> best = new List<Candidate>();
            for (int i = 0; i < limit; i++) { if ((i & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress(i * 100 / Math.Max(1, limit), "Bellaso · 词典密钥"); } string text = BellasoCipher.Transform(r.Input, Words[i], alphabetKey, true); Add(best, Words[i] + (alphabetKey.Length > 0 ? " / alphabet=" + alphabetKey : string.Empty), text, language); }
            r.ReportProgress(100, "Bellaso · 完成"); return Format(best);
        }

        internal static string CrackJefferson(ToolRequest r)
        {
            Need(r.Input, 20, "Jefferson Wheel"); int min = Read(r.Get("minseed"), 1700, -1000000, 1000000), max = Read(r.Get("maxseed"), 1850, min, 1000000); string language = Lang(r); List<Candidate> best = new List<Candidate>(); long done = 0, total = (long)(max - min + 1) * 25;
            for (int seed = min; seed <= max; seed++) for (int offset = 1; offset <= 25; offset++) { if ((done++ & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), "Jefferson Wheel · seed " + seed); } string text = JeffersonWheelCipher.Transform(r.Input, seed.ToString(), offset.ToString(), true); Add(best, "seed=" + seed + " / offset=" + offset, text, language); }
            r.ReportProgress(100, "Jefferson Wheel · 完成"); return Format(best);
        }

        internal static string CrackRunningKey(ToolRequest r)
        {
            string cipher = Letters(r.Input); Need(cipher, 40, "Running Key"); string crib = Letters(r.Get("crib")); if (crib.Length >= cipher.Length) { string key = DeriveKey(cipher, crib.Substring(0, cipher.Length)); return Single("crib / key=" + key, crib.Substring(0, cipher.Length), Lang(r)); }
            int iterations = Read(r.Get("iterations"), 120000, 1000, 1000000), restarts = Read(r.Get("restarts"), 8, 1, 30); string language = Lang(r), heuristic = HeuristicSearch.Normalize(r.Get("heuristic")); Random random = new Random(4201); List<Candidate> best = new List<Candidate>();
            for (int restart = 0; restart < restarts; restart++)
            {
                char[] key = SeedRunningKey(cipher.Length, restart), topKey = null; for (int i = 0; i < crib.Length; i++) key[i] = (char)('A' + Alphabet.Mod(cipher[i] - crib[i], 26)); string plain = DecodeRunning(cipher, key); double score = JointScore(plain, new string(key), language), top = score; topKey = (char[])key.Clone(); HeuristicSearchState searchState = HeuristicSearch.Create(score);
                for (int step = 0; step < iterations; step++) { if ((step & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((restart * iterations + step) * 100 / (restarts * iterations), "Running Key · " + HeuristicSearch.Label(heuristic) + " · 重启 " + (restart + 1)); } int p = crib.Length >= key.Length ? key.Length - 1 : crib.Length + random.Next(key.Length - crib.Length); char old = key[p]; key[p] = (char)('A' + random.Next(26)); string trial = DecodeRunning(cipher, key); double value = JointScore(trial, new string(key), language); if (HeuristicSearch.Accept(heuristic, value, score, step, iterations, 14.0, 1.0, random, searchState)) { score = value; plain = trial; if (value > top) { top = value; topKey = (char[])key.Clone(); } } else key[p] = old; }
                AddRaw(best, new Candidate { Key = new string(topKey), Text = DecodeRunning(cipher, topKey), Score = top });
            }
            r.ReportProgress(100, "Running Key · 完成"); return Format(best);
        }

        internal static string CrackThreeSquare(ToolRequest r) { return CrackTwoKeySquares(r, "Three-square", false); }
        internal static string CrackDigrafid(ToolRequest r) { return CrackTwoKeySquares(r, "Digrafid", true); }

        private static string CrackTwoKeySquares(ToolRequest r, string name, bool periods)
        {
            Need(r.Input, 30, name); string language = Lang(r); int min = periods ? Read(r.Get("minperiod"), 2, 1, 30) : 1, max = periods ? Read(r.Get("maxperiod"), 12, min, 30) : 1, limit = Math.Min(Words.Length, Read(r.Get("wordlimit"), 1200, 100, Words.Length)); List<Candidate> left = new List<Candidate>(), right = new List<Candidate>(), best = new List<Candidate>();
            for (int i = 0; i < limit; i++) { string k = Words[i], a = DecodePair(r.Input, k, string.Empty, min, periods), b = DecodePair(r.Input, string.Empty, k, min, periods); AddLimited(left, new Candidate { Key = k, Text = a, Score = Score(a, language) }, 35); AddLimited(right, new Candidate { Key = k, Text = b, Score = Score(b, language) }, 35); }
            int directLimit = Math.Min(60, limit); for (int period = min; period <= max; period++) for (int x = 0; x < directLimit; x++) for (int y = 0; y < directLimit; y++) { string text = DecodePair(r.Input, Words[x], Words[y], period, periods); Add(best, Words[x] + " / " + Words[y] + (periods ? " / period=" + period : string.Empty), text, language); }
            int jobs = (max - min + 1) * left.Count * right.Count, done = 0; for (int period = min; period <= max; period++) foreach (Candidate a in left) foreach (Candidate b in right) { if ((done++ & 127) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress(done * 100 / Math.Max(1, jobs), name + (periods ? " · 周期 " + period : " · 词典方阵")); } string text = DecodePair(r.Input, a.Key, b.Key, period, periods); Add(best, a.Key + " / " + b.Key + (periods ? " / period=" + period : string.Empty), text, language); }
            string direct = DecodePair(r.Input, string.Empty, string.Empty, min, periods); Add(best, "标准方阵" + (periods ? " / period=" + min : string.Empty), direct, language); r.ReportProgress(100, name + " · 完成"); return Format(best);
        }
        private static string DecodePair(string input, string a, string b, int period, bool digrafid) { return digrafid ? DigrafidCipher.Decrypt(input, a, b, period.ToString()) : ThreeSquareCipher.Decrypt(input, a, b); }

        private static char[] SeedRunningKey(int length, int restart) { StringBuilder source = new StringBuilder(); int start = Words.Length == 0 ? 0 : (restart * 137) % Words.Length; for (int i = 0; source.Length < length; i++) source.Append(Words[(start + i) % Words.Length]); return source.ToString(0, length).ToCharArray(); }
        private static string DecodeRunning(string cipher, char[] key) { StringBuilder r = new StringBuilder(cipher.Length); for (int i = 0; i < cipher.Length; i++) r.Append((char)('A' + Alphabet.Mod(cipher[i] - 'A' - (key[i] - 'A'), 26))); return r.ToString(); }
        private static string DeriveKey(string cipher, string plain) { StringBuilder r = new StringBuilder(); for (int i = 0; i < cipher.Length; i++) r.Append((char)('A' + Alphabet.Mod(cipher[i] - plain[i], 26))); return r.ToString(); }
        private static double JointScore(string plain, string key, string language) { return Score(plain, language) + Score(key, language); }
        private static string[] BuildWords() { List<string> result = new List<string>(new[] { "KEY", "KEYWORD", "EXAMPLE", "CIPHER", "SECRET", "FORT", "GERMANY", "CARGO", "ALPHABET", "ENIGMA" }); foreach (string word in EnglishNgramData.LoadWords()) if (!result.Contains(word)) result.Add(word); return result.ToArray(); }
        private static void Need(string input, int count, string name) { if (Letters(input).Length < count) throw new CipherException(name + " 破解至少需要 " + count + " 个字母"); }
        private static int Read(string text, int fallback, int min, int max) { int v; if (!int.TryParse(text, out v)) v = fallback; return Math.Max(min, Math.Min(max, v)); }
        private static string Lang(ToolRequest r) { return LanguageModels.Normalize(r.Get("language")); }
        private static string Letters(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') r.Append(c); } return r.ToString(); }
        private static double Score(string text, string language) { return LanguageModels.TextScore(Letters(text), language); }
        private static void Add(List<Candidate> list, string key, string text, string language) { AddLimited(list, new Candidate { Key = key, Text = text, Score = Score(text, language) }, 15); }
        private static void AddRaw(List<Candidate> list, Candidate c) { AddLimited(list, c, 15); }
        private static void AddLimited(List<Candidate> list, Candidate c, int limit) { list.Add(c); list.Sort(delegate(Candidate a, Candidate b) { return b.Score.CompareTo(a.Score); }); if (list.Count > limit) list.RemoveAt(list.Count - 1); }
        private static string Single(string key, string text, string language) { return "#1  密钥 " + key + "  评分 " + Score(text, language).ToString("0.00", CultureInfo.InvariantCulture) + "\r\n" + text; }
        private static string Format(List<Candidate> list) { StringBuilder r = new StringBuilder(); for (int i = 0; i < list.Count; i++) r.AppendFormat(CultureInfo.InvariantCulture, "#{0}  密钥 {1}  评分 {2:0.00}\r\n{3}\r\n\r\n", i + 1, list[i].Key, list[i].Score, list[i].Text); return r.ToString().TrimEnd(); }
    }

    internal static class EnigmaCracker
    {
        private sealed class Candidate { internal string Key; internal string Text; internal double Score; }
        internal static string Crack(ToolRequest r)
        {
            string model = EnigmaCipher.NormalizeModel(r.Get("model")), crib = Letters(r.Get("crib")); if (crib.Length < 3) throw new CipherException("Enigma 搜索至少需要 3 个字母的 Crib"); string baseRotors = string.IsNullOrWhiteSpace(r.Get("rotors")) ? (model == "M4" ? "Beta I II III" : "I II III") : r.Get("rotors"); List<string> rotorSets = RotorSets(baseRotors, IsTrue(r.Get("rotorsearch")), model); List<Candidate> best = new List<Candidate>(); int total = rotorSets.Count * 26 * 26 * 26, done = 0;
            foreach (string rotors in rotorSets) for (int a = 0; a < 26; a++) for (int b = 0; b < 26; b++) for (int c = 0; c < 26; c++)
            {
                if ((done++ & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress(done * 100 / Math.Max(1, total), "Enigma · 搜索初始位置"); }
                string movingPos = new string(new[] { (char)('A' + a), (char)('A' + b), (char)('A' + c) }), positions = model == "M4" ? "A" + movingPos : movingPos; string text = EnigmaCipher.Transform(r.Input, model, rotors, r.Get("rings"), positions, r.Get("reflector"), r.Get("plugboard")); double score = CribScore(text, crib); if (score > 0) Add(best, new Candidate { Key = rotors + " / " + positions, Text = text, Score = score });
            }
            r.ReportProgress(100, "Enigma · 完成"); if (best.Count == 0) throw new CipherException("当前范围内没有符合 Crib 的设置"); StringBuilder output = new StringBuilder(); for (int i = 0; i < best.Count; i++) output.AppendFormat(CultureInfo.InvariantCulture, "#{0}  密钥 {1}  评分 {2:0.00}\r\n{3}\r\n\r\n", i + 1, best[i].Key, best[i].Score, best[i].Text); return output.ToString().TrimEnd();
        }
        private static double CribScore(string text, string crib) { string letters = Letters(text); double best = -1; for (int p = 0; p + crib.Length <= letters.Length; p++) { int matches = 0; for (int i = 0; i < crib.Length; i++) if (letters[p + i] == crib[i]) matches++; if (matches == crib.Length) return 100000 + LanguageModels.TextScore(letters, "EN"); best = Math.Max(best, matches); } return best == crib.Length ? best : -1; }
        private static List<string> RotorSets(string text, bool search, string model) { string[] p = text.Split(new[] { ' ', ',', ';', '-', '/' }, StringSplitOptions.RemoveEmptyEntries); List<string> r = new List<string>(); if (!search || p.Length < 3) { r.Add(text); return r; } int start = model == "M4" ? 1 : 0; string prefix = model == "M4" ? p[0] + " " : string.Empty; string[] m = new[] { p[start], p[start + 1], p[start + 2] }; for (int a = 0; a < 3; a++) for (int b = 0; b < 3; b++) if (b != a) for (int c = 0; c < 3; c++) if (c != a && c != b) r.Add(prefix + m[a] + " " + m[b] + " " + m[c]); return r; }
        private static void Add(List<Candidate> list, Candidate c) { list.Add(c); list.Sort(delegate(Candidate a, Candidate b) { return b.Score.CompareTo(a.Score); }); if (list.Count > 15) list.RemoveAt(15); }
        private static bool IsTrue(string value) { return string.Equals((value ?? string.Empty).Trim(), "1", StringComparison.OrdinalIgnoreCase) || string.Equals((value ?? string.Empty).Trim(), "true", StringComparison.OrdinalIgnoreCase) || string.Equals((value ?? string.Empty).Trim(), "yes", StringComparison.OrdinalIgnoreCase); }
        private static string Letters(string input) { StringBuilder r = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') r.Append(c); } return r.ToString(); }
    }
}
