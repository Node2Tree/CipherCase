using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class AdvancedCrackers
    {
        private sealed class Candidate { internal string Key; internal string Text; internal double Score; }
        private sealed class PeriodCandidate { internal int Period; internal Candidate Value; }
        private delegate string Decoder(string key1, string key2, int period);
        private static readonly string[] EnglishKeywords = EnglishNgramData.LoadWords();

        internal static string CrackAmsco(ToolRequest r) { return CrackPermutation(r, "AMSCO", 2, 8, delegate(string s, string k) { return AmscoCipher.Decrypt(s, k); }); }
        internal static string CrackAdfgx(ToolRequest r, bool digits)
        {
            if (!string.IsNullOrWhiteSpace(r.Get("square"))) return CrackPermutation(r, digits ? "ADFGVX" : "ADFGX", 2, 8, delegate(string s, string k) { return AdfgxCipher.Decrypt(s, r.Get("square"), k, digits); });
            string name = digits ? "ADFGVX" : "ADFGX", source = Letters(r.Input); NeedLetters(source, 48, name); int min = Read(r.Get("min"), 2, 2, 9), max = Read(r.Get("max"), 8, min, 9), iterations = Read(r.Get("iterations"), 6000, 500, 100000); string language = LanguageModels.Normalize(r.Get("language")); long total = 0, done = 0; for (int n = min; n <= max; n++) total += Fact(n); List<Candidate> columns = new List<Candidate>();
            for (int n = min; n <= max; n++)
            {
                int[] order = Identity(n); do { if ((done & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 45 / Math.Max(1, total)), name + " · 筛选列序"); } string key = Keyword(order); string text = AdfgxCipher.Decrypt(source, string.Empty, key, digits); Add(columns, new Candidate { Key = key, Text = text, Score = FrequencyShape(text, digits ? 36 : 25) }, 24); done++; } while (Next(order));
            }
            string alphabet = digits ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" : "ABCDEFGHIKLMNOPQRSTUVWXYZ"; List<Candidate> best = new List<Candidate>();
            for (int ci = 0; ci < columns.Count; ci++)
            {
                int[] symbols = SymbolIndices(columns[ci].Text, alphabet); string mappingAlphabet = alphabet; if (digits) symbols = CompactSymbols(symbols, out mappingAlphabet); Candidate mapped = DirectMappingCandidate(symbols, mappingAlphabet, iterations, 2, 941 + ci * 17, language, r, name, 45 + ci * 55 / columns.Count, 45 + (ci + 1) * 55 / columns.Count); mapped.Key += " / " + columns[ci].Key; Add(best, mapped, 15);
            }
            r.ReportProgress(100, name + " · 完成"); return Format(best);
        }

        internal static string CrackMyszkowski(ToolRequest r)
        {
            string source = r.Input ?? string.Empty; NeedLetters(source, 30, "Myszkowski"); int min = Read(r.Get("min"), 3, 2, 8), max = Read(r.Get("max"), 7, min, 8); string language = LanguageModels.Normalize(r.Get("language")); List<Candidate> best = new List<Candidate>(); long total = 0, done = 0; for (int n = min; n <= max; n++) total += OrderedBell(n);
            for (int n = min; n <= max; n++)
            {
                int[] ranks = new int[n]; long localDone = done; EnumerateMyszkowski(ranks, 1, 0, delegate(int[] values) { if (((localDone++) & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(localDone * 100 / Math.Max(1, total)), "Myszkowski · 宽度 " + n); } string key = RankKey(values), text = MyszkowskiCipher.Decrypt(source, key); Add(best, new Candidate { Key = key, Text = text, Score = Score(text, language) }, 20); }); done = localDone;
            }
            r.ReportProgress(100, "Myszkowski · 完成"); return Format(best);
        }

        internal static string CrackTurningGrille(ToolRequest r)
        {
            string source = r.Input ?? string.Empty; int size = Read(r.Get("size"), 4, 4, 6); if (size != 4 && size != 6) throw new CipherException("破解支持 4×4 或 6×6 方阵"); if (source.Length == 0 || source.Length % (size * size) != 0) throw new CipherException("密文长度须为方阵大小的倍数");
            List<int[]> orbits = GrilleOrbits(size); long total = 1; for (int i = 0; i < orbits.Count; i++) total *= 4; string language = LanguageModels.Normalize(r.Get("language")); List<Candidate> best = new List<Candidate>();
            for (long mask = 0; mask < total; mask++)
            {
                if ((mask & 511) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(mask * 100 / total), "Turning Grille · " + size + "×" + size); }
                long value = mask; List<int> holes = new List<int>(); for (int i = 0; i < orbits.Count; i++) { holes.Add(orbits[i][value % 4]); value /= 4; } holes.Sort(); StringBuilder key = new StringBuilder(); foreach (int h in holes) { if (key.Length > 0) key.Append(','); key.Append(h + 1); }
                string plain = TurningGrilleCipher.Decrypt(source, size.ToString(CultureInfo.InvariantCulture), key.ToString()); Add(best, new Candidate { Key = key.ToString(), Text = plain, Score = Score(plain, language) }, 15);
            }
            r.ReportProgress(100, "Turning Grille · 完成"); return Format(best);
        }

        internal static string CrackAutokey(ToolRequest r)
        {
            string cipher = Letters(r.Input); NeedLetters(cipher, 40, "Autokey"); int min = Read(r.Get("min"), 2, 1, 20), max = Read(r.Get("max"), 12, min, 20), rounds = Read(r.Get("iterations"), 8, 1, 100); string language = LanguageModels.Normalize(r.Get("language")); List<Candidate> best = new List<Candidate>(); AutokeyCipher algorithm = new AutokeyCipher();
            for (int length = min; length <= max; length++)
            {
                char[] key = new string('A', length).ToCharArray(); for (int p = 0; p < length; p++) { double top = double.NegativeInfinity; char chosen = 'A'; for (int s = 0; s < 26; s++) { key[p] = (char)('A' + s); string plain = algorithm.Decrypt(cipher, new string(key)); double value = ColumnScore(plain, p, length, language); if (value > top) { top = value; chosen = key[p]; } } key[p] = chosen; }
                for (int round = 0; round < rounds; round++) for (int p = 0; p < length; p++) { char keep = key[p]; double top = Score(algorithm.Decrypt(cipher, new string(key)), language); for (int s = 0; s < 26; s++) { key[p] = (char)('A' + s); double value = Score(algorithm.Decrypt(cipher, new string(key)), language); if (value > top) { top = value; keep = key[p]; } } key[p] = keep; }
                string text = algorithm.Decrypt(cipher, new string(key)); Add(best, new Candidate { Key = new string(key), Text = text, Score = Score(text, language) }, 15); r.ReportProgress((length - min + 1) * 100 / (max - min + 1), "Autokey · 长度 " + length); r.ThrowIfCancellationRequested();
            }
            return Format(best);
        }

        internal static string CrackPlayfair(ToolRequest r) { PlayfairCipher c = new PlayfairCipher(); return Anneal(r, "Playfair", "ABCDEFGHIKLMNOPQRSTUVWXYZ", 1, delegate(string a, string b, int p) { return c.Decrypt(r.Input, a); }); }
        internal static string CrackFractionatedMorse(ToolRequest r) { return Anneal(r, "Fractionated Morse", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", 1, delegate(string a, string b, int p) { return FractionatedMorseCipher.Decrypt(r.Input, a); }); }
        internal static string CrackPolybius(ToolRequest r)
        {
            string digits = Digits15(r.Input); if (digits.Length < 60 || digits.Length % 2 != 0) throw new CipherException("keyed Polybius 破解需要至少 30 组有效坐标"); int[] symbols = new int[digits.Length / 2]; for (int i = 0; i < symbols.Length; i++) symbols[i] = (digits[i * 2] - '1') * 5 + digits[i * 2 + 1] - '1'; int iterations = Read(r.Get("iterations"), 30000, 500, 1000000), restarts = Read(r.Get("restarts"), 6, 1, 30); string language = LanguageModels.Normalize(r.Get("language")); Candidate best = DirectMappingCandidate(symbols, "ABCDEFGHIKLMNOPQRSTUVWXYZ", iterations, restarts, 821, language, r, "keyed Polybius", 0, 100); r.ReportProgress(100, "keyed Polybius · 完成"); return Format(new List<Candidate> { best });
        }
        internal static string CrackTwoSquare(ToolRequest r) { return Anneal(r, "Two-square", "ABCDEFGHIKLMNOPQRSTUVWXYZ", 2, delegate(string a, string b, int p) { return TwoSquareCipher.Transform(r.Input, a, b, true); }); }
        internal static string CrackFourSquare(ToolRequest r) { return Anneal(r, "Four-square", "ABCDEFGHIKLMNOPQRSTUVWXYZ", 2, delegate(string a, string b, int p) { return FourSquareCipher.Transform(r.Input, a, b, true); }); }
        internal static string CrackBifid(ToolRequest r) { return AnnealPeriods(r, "Bifid", "ABCDEFGHIKLMNOPQRSTUVWXYZ", delegate(string a, string b, int p) { return BifidCipher.Decrypt(r.Input, a, p.ToString(CultureInfo.InvariantCulture)); }); }
        internal static string CrackTrifid(ToolRequest r) { return AnnealPeriods(r, "Trifid", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", delegate(string a, string b, int p) { return TrifidCipher.Decrypt(r.Input, a, p.ToString(CultureInfo.InvariantCulture)); }); }

        internal static string CrackCheckerboard(ToolRequest r)
        {
            List<Candidate> layouts = new List<Candidate>(); int iterations = Read(r.Get("iterations"), 12000, 500, 500000); string language = LanguageModels.Normalize(r.Get("language")); const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int a = 0; a < 10; a++) for (int b = a + 1; b < 10; b++)
            {
                string blanks = a.ToString() + b.ToString(), text = StraddlingCheckerboardCipher.Decrypt(r.Input, alphabet, blanks); int unknown = 0; foreach (char c in text) if (c == '?') unknown++; Add(layouts, new Candidate { Key = blanks, Text = text, Score = FrequencyShape(text, 26) - unknown }, 8);
            }
            List<Candidate> all = new List<Candidate>(); for (int i = 0; i < layouts.Count; i++) { r.ThrowIfCancellationRequested(); int[] symbols = SymbolIndices(layouts[i].Text, alphabet); Candidate c = DirectMappingCandidate(symbols, alphabet, iterations, 2, 611 + i * 29, language, r, "跨行棋盘", i * 100 / layouts.Count, (i + 1) * 100 / layouts.Count); c.Key = layouts[i].Key + " / " + c.Key; Add(all, c, 15); }
            r.ReportProgress(100, "跨行棋盘 · 完成"); return Format(all);
        }

        internal static string CrackNihilist(ToolRequest r)
        {
            string[] parts = (r.Input ?? string.Empty).Split(new[] { ' ', ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries); List<int> numbers = new List<int>(); foreach (string part in parts) { int n; if (!int.TryParse(part, out n)) throw new CipherException("Nihilist 密文须为数字序列"); numbers.Add(n); } if (numbers.Count < 30) throw new CipherException("Nihilist 破解至少需要 30 组数字");
            string square = PolybiusCipher.BuildSquare(r.Get("square")), language = LanguageModels.Normalize(r.Get("language")); int min = Read(r.Get("min"), 2, 1, 15), max = Read(r.Get("max"), 10, min, 15); List<Candidate> best = new List<Candidate>();
            for (int length = min; length <= max; length++)
            {
                char[] key = new char[length]; bool valid = true; for (int p = 0; p < length; p++) { double top = double.NegativeInfinity; char chosen = 'A'; for (int q = 0; q < 25; q++) { string plain = DecodeNihilist(numbers, square, p, length, q); if (plain.Length == 0) continue; double value = Score(plain, language); if (value > top) { top = value; chosen = square[q]; } } if (double.IsNegativeInfinity(top)) { valid = false; break; } key[p] = chosen; } if (!valid) { r.ReportProgress((length - min + 1) * 100 / (max - min + 1), "Nihilist · 跳过长度 " + length); continue; }
                string text; try { text = NihilistCipher.Decrypt(r.Input, square, new string(key)); } catch (CipherException) { continue; } for (int round = 0; round < 5; round++) for (int p = 0; p < length; p++) { char keep = key[p]; double top = Score(text, language); for (int q = 0; q < 25; q++) { key[p] = square[q]; string trial; try { trial = NihilistCipher.Decrypt(r.Input, square, new string(key)); } catch (CipherException) { continue; } double value = Score(trial, language); if (value > top) { top = value; keep = key[p]; text = trial; } } key[p] = keep; }
                Add(best, new Candidate { Key = new string(key), Text = text, Score = Score(text, language) }, 15); r.ReportProgress((length - min + 1) * 100 / (max - min + 1), "Nihilist · 长度 " + length); r.ThrowIfCancellationRequested();
            }
            return Format(best);
        }

        internal static string CrackHomophonic(ToolRequest r)
        {
            string[] parts = (r.Input ?? string.Empty).Split(new[] { ' ', ',', ';', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries); List<int> codes = new List<int>(); foreach (string part in parts) { int n; if (!int.TryParse(part, out n)) throw new CipherException("同音替换密文须为空格分隔的数字"); codes.Add(n); } if (codes.Count < 40) throw new CipherException("同音替换破解至少需要 40 组数字");
            List<int> unique = new List<int>(); foreach (int code in codes) if (!unique.Contains(code)) unique.Add(code); int iterations = Read(r.Get("iterations"), 200000, 1000, 1000000), restarts = Read(r.Get("restarts"), 12, 1, 30); string language = LanguageModels.Normalize(r.Get("language")); Random random = new Random(117); List<Candidate> best = new List<Candidate>(); int[] codeCounts = new int[unique.Count]; foreach (int code in codes) codeCounts[unique.IndexOf(code)]++; List<int> codeOrder = new List<int>(); for (int i = 0; i < unique.Count; i++) codeOrder.Add(i); codeOrder.Sort(delegate(int a, int b) { return codeCounts[b].CompareTo(codeCounts[a]); }); List<int> letterOrder = new List<int>(); for (int i = 0; i < 26; i++) letterOrder.Add(i); double[] frequencies = LanguageModels.GetFrequencies(language); letterOrder.Sort(delegate(int a, int b) { return frequencies[b].CompareTo(frequencies[a]); }); char[] seedMap = new char[unique.Count]; for (int i = 0; i < codeOrder.Count; i++) seedMap[codeOrder[i]] = (char)('A' + letterOrder[Math.Min(25, i / 3)]);
            for (int restart = 0; restart < restarts; restart++)
            {
                char[] map = (char[])seedMap.Clone(); for (int i = 0; i < restart * 4; i++) SwapRandom(map, random); string text = DecodeHomophonic(codes, unique, map); double score = Score(text, language), top = score; char[] topMap = (char[])map.Clone(); string heuristic = Heuristic(r); double[] late = LateHistory(score);
                for (int step = 0; step < iterations; step++) { if ((step & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((restart * iterations + step) * 100 / (restarts * iterations), "同音替换 · " + HeuristicLabel(heuristic) + " · 重启 " + (restart + 1)); } int a = random.Next(map.Length), b = -1; char oldA = map[a], oldB = '\0'; if (random.Next(4) == 0) { char proposed; int attempts = 0; do { proposed = (char)('A' + random.Next(26)); attempts++; } while (MappingCount(map, proposed) >= 3 && attempts < 50); map[a] = proposed; } else { b = random.Next(map.Length); oldB = map[b]; map[a] = oldB; map[b] = oldA; } string trial = DecodeHomophonic(codes, unique, map); double value = Score(trial, language); if (AcceptMove(heuristic, value, score, step, iterations, 10.0, 1.0, random, late)) { text = trial; score = value; if (value > top) { top = value; topMap = (char[])map.Clone(); } } else { map[a] = oldA; if (b >= 0) map[b] = oldB; } }
                text = DecodeHomophonic(codes, unique, topMap); Add(best, new Candidate { Key = HomophonicMap(unique, topMap), Text = text, Score = top }, 15);
            }
            r.ReportProgress(100, "同音替换 · 完成"); return Format(best);
        }

        internal static string CrackDoubleColumnar(ToolRequest r) { return CrackOrderAnneal(r, "双重列换位", true, false); }
        internal static string CrackUbchi(ToolRequest r)
        {
            string source = r.Input ?? string.Empty; NeedLetters(source, 35, "Ubchi"); int min = Read(r.Get("min"), 2, 2, 9), max = Read(r.Get("max"), 8, min, 9), nullMax = Read(r.Get("nullmax"), 3, 0, 20); string language = LanguageModels.Normalize(r.Get("language")); long total = 0, done = 0; for (int n = min; n <= max; n++) total += Fact(n) * (nullMax + 1); List<Candidate> best = new List<Candidate>();
            for (int n = min; n <= max; n++) { int[] order = Identity(n); do { string key = Keyword(order); for (int nulls = 0; nulls <= nullMax; nulls++) { if ((done++ & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), "Ubchi · 宽度 " + n); } string text = UbchiCipher.Decrypt(source, key, nulls.ToString(CultureInfo.InvariantCulture)); string letters = Letters(text); Add(best, new Candidate { Key = key + " / nulls=" + nulls, Text = text, Score = Score(text, language) / Math.Max(1, letters.Length) }, 20); } } while (Next(order)); }
            r.ReportProgress(100, "Ubchi · 完成"); return Format(best);
        }

        private static string CrackOrderAnneal(ToolRequest r, string name, bool twoKeys, bool ubchi)
        {
            string source = r.Input ?? string.Empty; NeedLetters(source, 35, name); int min = Read(r.Get("min"), 2, 2, 10), max = Read(r.Get("max"), twoKeys ? 6 : 8, min, 10), iterations = Read(r.Get("iterations"), twoKeys ? 60000 : 30000, 1000, 1000000), nullMax = Read(r.Get("nullmax"), 3, 0, 20); string language = LanguageModels.Normalize(r.Get("language")), heuristic = Heuristic(r); Random random = new Random(919); List<Candidate> best = new List<Candidate>();
            for (int width = min; width <= max; width++)
            {
                int[] a = Identity(width), b = Identity(width); string ka = Keyword(a), kb = Keyword(b); int currentNull = 0; string text = OrderDecrypt(source, ka, kb, ubchi, currentNull); double score = Score(text, language), top = score; string topA = ka, topB = kb, topText = text; int topNull = currentNull; double[] late = LateHistory(score);
                for (int step = 0; step < iterations; step++)
                {
                    if ((step & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress(((width - min) * iterations + step) * 100 / ((max - min + 1) * iterations), name + " · " + HeuristicLabel(heuristic) + " · 宽度 " + width); }
                    int[] target = twoKeys && random.Next(2) == 1 ? b : a; int x = random.Next(width), y = random.Next(width); int v = target[x]; target[x] = target[y]; target[y] = v; int proposedNull = ubchi && random.Next(4) == 0 ? random.Next(nullMax + 1) : currentNull; ka = Keyword(a); kb = Keyword(b); string trial = OrderDecrypt(source, ka, kb, ubchi, proposedNull); double value = Score(trial, language);
                    if (AcceptMove(heuristic, value, score, step, iterations, 8.0, Math.Max(1, source.Length), random, late)) { score = value; text = trial; currentNull = proposedNull; if (value > top) { top = value; topA = ka; topB = kb; topText = trial; topNull = currentNull; } } else { v = target[x]; target[x] = target[y]; target[y] = v; }
                }
                Add(best, new Candidate { Key = ubchi ? topA + " / nulls=" + topNull : topA + " / " + topB, Text = topText, Score = top }, 15);
            }
            r.ReportProgress(100, name + " · 完成"); return Format(best);
        }

        private static string CrackPermutation(ToolRequest r, string name, int defaultMin, int defaultMax, Func<string, string, string> decrypt)
        {
            string source = r.Input ?? string.Empty; NeedLetters(source, 24, name); int min = Read(r.Get("min"), defaultMin, 2, 9), max = Read(r.Get("max"), defaultMax, min, 9); string language = LanguageModels.Normalize(r.Get("language")); long total = 0; for (int n = min; n <= max; n++) total += Fact(n); long done = 0; List<Candidate> best = new List<Candidate>();
            for (int n = min; n <= max; n++) { int[] order = Identity(n); do { if ((done & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress((int)(done * 100 / Math.Max(1, total)), name + " · 宽度 " + n); } string key = Keyword(order); try { string text = decrypt(source, key); Add(best, new Candidate { Key = key, Text = text, Score = Score(text, language) }, 20); } catch (CipherException) { } done++; } while (Next(order)); }
            r.ReportProgress(100, name + " · 完成"); return Format(best);
        }

        private static string Anneal(ToolRequest r, string name, string alphabet, int keys, Decoder decoder) { int strong = name == "Playfair" || name == "Two-square" || name == "Four-square" ? 100000 : 30000, restartDefault = strong > 30000 ? 10 : 4; int iterations = Read(r.Get("iterations"), strong, 500, 1000000), restarts = Read(r.Get("restarts"), restartDefault, 1, 30); return AnnealCore(r, name, alphabet, keys, 0, iterations, restarts, decoder); }
        private static string AnnealPeriods(ToolRequest r, string name, string alphabet, Decoder decoder)
        {
            if (Letters(r.Input).Length < 30) throw new CipherException(name + " 破解至少需要 30 个字母"); int min = Read(r.Get("minperiod"), 2, 1, 50), max = Read(r.Get("maxperiod"), 12, min, 50), iterations = Read(r.Get("iterations"), 100000, 500, 1000000), restarts = Read(r.Get("restarts"), 6, 1, 30), stageIterations = Math.Min(12000, Math.Max(2000, iterations / 5)); string language = LanguageModels.Normalize(r.Get("language")); List<Candidate> best = new List<Candidate>(); AddDictionaryCandidates(r, alphabet, 1, min, max, decoder, language, best, name);
            if (min == max)
            {
                for (int restart = 0; restart < restarts; restart++) Add(best, AnnealOne(r, name, alphabet, iterations, 1, 1301 + restart * 37, decoder, language, restart * 100 / restarts, min), 15); r.ReportProgress(100, name + " · 完成"); return Format(best);
            }
            List<PeriodCandidate> periods = new List<PeriodCandidate>();
            for (int period = min; period <= max; period++)
            {
                Candidate top = null; for (int restart = 0; restart < 2; restart++) { Candidate value = AnnealOne(r, name, alphabet, stageIterations, 1, 1501 + period * 53 + restart * 17, decoder, language, (period - min) * 45 / (max - min + 1), period); if (top == null || value.Score > top.Score) top = value; Add(best, value, 15); }
                periods.Add(new PeriodCandidate { Period = period, Value = top });
            }
            periods.Sort(delegate(PeriodCandidate a, PeriodCandidate b) { return b.Value.Score.CompareTo(a.Value.Score); }); int finalists = Math.Min(3, periods.Count);
            for (int i = 0; i < finalists; i++) for (int restart = 0; restart < restarts; restart++) Add(best, AnnealOne(r, name, alphabet, iterations, 1, 1901 + periods[i].Period * 71 + restart * 19, decoder, language, 50 + (i * restarts + restart) * 49 / Math.Max(1, finalists * restarts), periods[i].Period), 15);
            r.ReportProgress(100, name + " · 完成"); return Format(best);
        }
        private static string AnnealCore(ToolRequest r, string name, string alphabet, int keys, int periodCode, int iterations, int restarts, Decoder decoder)
        {
            if (Letters(r.Input).Length < 30 && (r.Input ?? string.Empty).Length < 60) throw new CipherException(name + " 破解需要更长的密文"); string language = LanguageModels.Normalize(r.Get("language")); int minPeriod = periodCode == 0 ? 0 : periodCode / 1000, maxPeriod = periodCode == 0 ? 0 : periodCode % 1000; List<Candidate> best = new List<Candidate>(); int periods = periodCode == 0 ? 1 : maxPeriod - minPeriod + 1, job = 0; AddDictionaryCandidates(r, alphabet, keys, minPeriod, maxPeriod, decoder, language, best, name);
            for (int period = minPeriod; period <= maxPeriod; period++) for (int restart = 0; restart < restarts; restart++) { Candidate c = AnnealOne(r, name, alphabet, iterations, keys, 177 + job * 31, decoder, language, job * 100 / Math.Max(1, periods * restarts), period); Add(best, c, 15); job++; }
            r.ReportProgress(100, name + " · 完成"); return Format(best);
        }
        private static Candidate AnnealOne(ToolRequest r, string name, string alphabet, int iterations, int keys, int seed, Decoder decoder, string language, int baseProgress) { return AnnealOne(r, name, alphabet, iterations, keys, seed, decoder, language, baseProgress, 0); }
        private static Candidate AnnealOne(ToolRequest r, string name, string alphabet, int iterations, int keys, int seed, Decoder decoder, string language, int baseProgress, int period)
        {
            Random random = new Random(seed); char[] a = alphabet.ToCharArray(), b = alphabet.ToCharArray(); for (int i = 0; i < (seed / 31) % 9; i++) { SwapRandom(a, random); if (keys == 2) SwapRandom(b, random); } string text = decoder(new string(a), new string(b), period); double score = Score(text, language), top = score; char[] topA = (char[])a.Clone(), topB = (char[])b.Clone(); string heuristic = Heuristic(r); double[] late = LateHistory(score);
            for (int step = 0; step < iterations; step++) { if ((step & 255) == 0) { r.ThrowIfCancellationRequested(); r.ReportProgress(Math.Min(99, baseProgress + step * 10 / Math.Max(1, iterations)), name + " · " + HeuristicLabel(heuristic)); } bool second = keys == 2 && random.Next(2) == 1; char[] trialA = (char[])a.Clone(), trialB = (char[])b.Clone(), target = second ? trialB : trialA; MutateKey(target, random); string trial = decoder(new string(trialA), new string(trialB), period); double value = Score(trial, language); if (AcceptMove(heuristic, value, score, step, iterations, 12.0, 1.0, random, late)) { a = trialA; b = trialB; text = trial; score = value; if (value > top) { top = value; topA = (char[])a.Clone(); topB = (char[])b.Clone(); } } }
            text = decoder(new string(topA), new string(topB), period); return new Candidate { Key = new string(topA) + (keys == 2 ? " / " + new string(topB) : string.Empty) + (period > 0 ? " / period=" + period : string.Empty), Text = text, Score = top };
        }

        private static void AddDictionaryCandidates(ToolRequest request, string alphabet, int keys, int minPeriod, int maxPeriod, Decoder decoder, string language, List<Candidate> best, string name)
        {
            if (language != "EN") return; int first = minPeriod, last = maxPeriod; if (first == 0 && last == 0) { first = 0; last = 0; }
            for (int period = first; period <= last; period++)
            {
                request.ThrowIfCancellationRequested();
                if (keys == 1)
                {
                    for (int i = 0; i < EnglishKeywords.Length; i++) { string key = KeywordAlphabet(EnglishKeywords[i], alphabet), text = decoder(key, alphabet, period); Add(best, new Candidate { Key = EnglishKeywords[i] + " / " + key + (period > 0 ? " / period=" + period : string.Empty), Text = text, Score = Score(text, language) + 20 }, 15); }
                }
                else
                {
                    List<Candidate> left = new List<Candidate>(), right = new List<Candidate>();
                    for (int i = 0; i < EnglishKeywords.Length; i++) { string key = KeywordAlphabet(EnglishKeywords[i], alphabet), textLeft = decoder(key, alphabet, period), textRight = decoder(alphabet, key, period); Add(left, new Candidate { Key = EnglishKeywords[i], Text = textLeft, Score = Score(textLeft, language) }, 30); Add(right, new Candidate { Key = EnglishKeywords[i], Text = textRight, Score = Score(textRight, language) }, 30); }
                    int direct = Math.Min(100, EnglishKeywords.Length); for (int i = 0; i < direct; i++) for (int j = 0; j < direct; j++) { string ka = KeywordAlphabet(EnglishKeywords[i], alphabet), kb = KeywordAlphabet(EnglishKeywords[j], alphabet), text = decoder(ka, kb, period); Add(best, new Candidate { Key = EnglishKeywords[i] + " / " + EnglishKeywords[j], Text = text, Score = Score(text, language) + 30 }, 15); }
                    foreach (Candidate a in left) foreach (Candidate b in right) { string ka = KeywordAlphabet(a.Key, alphabet), kb = KeywordAlphabet(b.Key, alphabet), text = decoder(ka, kb, period); Add(best, new Candidate { Key = a.Key + " / " + b.Key, Text = text, Score = Score(text, language) + 30 }, 15); }
                }
                request.ReportProgress((period - first + 1) * 8 / Math.Max(1, last - first + 1), name + " · 词典密钥");
            }
        }
        private static string KeywordAlphabet(string word, string alphabet) { return alphabet.Length == 25 ? PolybiusCipher.BuildSquare(word) : CipherUtilities.KeyedAlphabet(word, true); }

        private static string OrderDecrypt(string source, string a, string b, bool ubchi, int nulls) { if (ubchi) return UbchiCipher.Decrypt(source, a, nulls.ToString(CultureInfo.InvariantCulture)); return ColumnarTranspositionCipher.DecryptText(ColumnarTranspositionCipher.DecryptText(source, b), a); }
        private static string DecodeNihilist(List<int> values, string square, int column, int length, int keyIndex) { int add = (keyIndex / 5 + 1) * 10 + keyIndex % 5 + 1; StringBuilder s = new StringBuilder(); for (int i = column; i < values.Count; i += length) { int c = values[i] - add, row = c / 10 - 1, col = c % 10 - 1; if (row < 0 || row >= 5 || col < 0 || col >= 5) return string.Empty; s.Append(square[row * 5 + col]); } return s.ToString(); }
        private static string DecodeHomophonic(List<int> codes, List<int> unique, char[] map) { StringBuilder s = new StringBuilder(codes.Count); foreach (int code in codes) s.Append(map[unique.IndexOf(code)]); return s.ToString(); }
        private static string HomophonicMap(List<int> codes, char[] map) { StringBuilder s = new StringBuilder(); for (int i = 0; i < codes.Count; i++) { if (i > 0) s.Append(','); s.Append(codes[i]).Append('=').Append(map[i]); } return s.ToString(); }
        private static int MappingCount(char[] map, char value) { int count = 0; foreach (char c in map) if (c == value) count++; return count; }
        private static double ColumnScore(string text, int column, int width, string language) { StringBuilder s = new StringBuilder(); for (int i = column; i < text.Length; i += width) s.Append(text[i]); return Score(s.ToString(), language); }
        private static double FrequencyShape(string text, int size) { int[] counts = new int[size]; string basis = size == 36 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" : "ABCDEFGHIKLMNOPQRSTUVWXYZ"; foreach (char c in (text ?? string.Empty).ToUpperInvariant()) { int p = basis.IndexOf(c); if (p >= 0) counts[p]++; } Array.Sort(counts); double[] expected = { .127, .091, .082, .075, .070, .067, .063, .061, .060, .043, .040, .028, .028, .024, .024, .022, .020, .020, .019, .015, .010, .008, .002, .002, .001, .001 }; double score = 0, total = Math.Max(1, text.Length); for (int i = 0; i < size; i++) { double observed = counts[size - 1 - i] / total, target = i < expected.Length ? expected[i] : 0; double d = observed - target; score -= d * d; } return score; }
        private static double Score(string text, string language) { return LanguageModels.TextScore(Letters(text), language); }
        private static Candidate DirectMappingCandidate(int[] symbols, string alphabet, int iterations, int restarts, int seed, string language, ToolRequest request, string name, int progressStart, int progressEnd)
        {
            int[] counts = new int[alphabet.Length]; foreach (int symbol in symbols) if (symbol >= 0 && symbol < counts.Length) counts[symbol]++; List<int> cipherOrder = new List<int>(), plainOrder = new List<int>(); for (int i = 0; i < alphabet.Length; i++) { cipherOrder.Add(i); plainOrder.Add(i); }
            cipherOrder.Sort(delegate(int a, int b) { return counts[b].CompareTo(counts[a]); }); double[] frequencies = LanguageModels.GetFrequencies(language); plainOrder.Sort(delegate(int a, int b) { double fa = alphabet[a] >= 'A' && alphabet[a] <= 'Z' ? frequencies[alphabet[a] - 'A'] : -1, fb = alphabet[b] >= 'A' && alphabet[b] <= 'Z' ? frequencies[alphabet[b] - 'A'] : -1; return fb.CompareTo(fa); }); char[] seedMap = new char[alphabet.Length]; for (int i = 0; i < alphabet.Length; i++) seedMap[cipherOrder[i]] = alphabet[plainOrder[i]];
            Random random = new Random(seed); char[] bestMap = (char[])seedMap.Clone(); string bestText = DecodeSymbols(symbols, bestMap); double bestScore = DirectScore(bestText, language);
            for (int restart = 0; restart < restarts; restart++)
            {
                char[] map = (char[])seedMap.Clone(); for (int i = 0; i < restart * 3; i++) SwapRandom(map, random); string text = DecodeSymbols(symbols, map); double score = DirectScore(text, language); string heuristic = Heuristic(request); double[] late = LateHistory(score);
                for (int step = 0; step < iterations; step++) { if ((step & 255) == 0) { request.ThrowIfCancellationRequested(); int done = restart * iterations + step, total = restarts * iterations; request.ReportProgress(progressStart + done * (progressEnd - progressStart) / Math.Max(1, total), name + " · " + HeuristicLabel(heuristic)); } int a = random.Next(map.Length), b = random.Next(map.Length); char c = map[a]; map[a] = map[b]; map[b] = c; string trial = DecodeSymbols(symbols, map); double value = DirectScore(trial, language); if (AcceptMove(heuristic, value, score, step, iterations, 10.0, 1.0, random, late)) { text = trial; score = value; if (value > bestScore) { bestScore = value; bestMap = (char[])map.Clone(); bestText = trial; } } else { c = map[a]; map[a] = map[b]; map[b] = c; } }
            }
            return new Candidate { Key = new string(bestMap), Text = bestText, Score = bestScore };
        }
        private static string DecodeSymbols(int[] symbols, char[] map) { StringBuilder s = new StringBuilder(symbols.Length); foreach (int symbol in symbols) if (symbol >= 0 && symbol < map.Length) s.Append(map[symbol]); return s.ToString(); }
        private static int[] SymbolIndices(string text, string alphabet) { List<int> values = new List<int>(); foreach (char c in (text ?? string.Empty).ToUpperInvariant()) { int p = alphabet.IndexOf(c); if (p >= 0) values.Add(p); } return values.ToArray(); }
        private static int[] CompactSymbols(int[] symbols, out string alphabet)
        {
            List<int> unique = new List<int>(); foreach (int symbol in symbols) if (!unique.Contains(symbol)) unique.Add(symbol); int size = Math.Max(26, unique.Count); alphabet = size <= 26 ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; int[] result = new int[symbols.Length]; for (int i = 0; i < symbols.Length; i++) result[i] = unique.IndexOf(symbols[i]); return result;
        }
        private static double DirectScore(string text, string language) { string letters = Letters(text); return LanguageModels.TextScore(letters, language) - (text.Length - letters.Length) * 8.0; }
        private static string Digits15(string input) { StringBuilder s = new StringBuilder(); foreach (char c in input ?? string.Empty) if (c >= '1' && c <= '5') s.Append(c); return s.ToString(); }
        private static void NeedLetters(string text, int count, string name) { if (Letters(text).Length < count) throw new CipherException(name + " 破解至少需要 " + count + " 个字母"); }
        private static string Letters(string input) { StringBuilder s = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') s.Append(c); } return s.ToString(); }
        private static int Read(string text, int fallback, int min, int max) { int value; if (!int.TryParse(text, out value)) value = fallback; return Math.Max(min, Math.Min(max, value)); }
        private static int[] Identity(int n) { int[] a = new int[n]; for (int i = 0; i < n; i++) a[i] = i; return a; }
        private static string Keyword(int[] order) { char[] key = new char[order.Length]; for (int rank = 0; rank < order.Length; rank++) key[order[rank]] = (char)('A' + rank); return new string(key); }
        private static string RankKey(int[] ranks) { char[] key = new char[ranks.Length]; for (int i = 0; i < ranks.Length; i++) key[i] = (char)('A' + ranks[i]); return new string(key); }
        private static void NormalizeRanks(int[] ranks) { int[] values = (int[])ranks.Clone(); Array.Sort(values); Dictionary<int, int> map = new Dictionary<int, int>(); int n = 0; foreach (int v in values) if (!map.ContainsKey(v)) map[v] = n++; for (int i = 0; i < ranks.Length; i++) ranks[i] = map[ranks[i]]; }
        private static void EnumerateMyszkowski(int[] partition, int position, int maximum, Action<int[]> visit)
        {
            if (position == partition.Length) { int groups = maximum + 1; int[] order = Identity(groups); do { int[] ranks = new int[partition.Length]; for (int i = 0; i < ranks.Length; i++) ranks[i] = order[partition[i]]; visit(ranks); } while (Next(order)); return; }
            for (int rank = 0; rank <= maximum + 1; rank++) { partition[position] = rank; EnumerateMyszkowski(partition, position + 1, Math.Max(maximum, rank), visit); }
        }
        private static long OrderedBell(int n) { long[] values = { 1, 1, 3, 13, 75, 541, 4683, 47293, 545835 }; return values[Math.Max(0, Math.Min(8, n))]; }
        private static bool Next(int[] a) { int i = a.Length - 2; while (i >= 0 && a[i] >= a[i + 1]) i--; if (i < 0) return false; int j = a.Length - 1; while (a[j] <= a[i]) j--; int v = a[i]; a[i] = a[j]; a[j] = v; Array.Reverse(a, i + 1, a.Length - i - 1); return true; }
        private static long Fact(int n) { long v = 1; for (int i = 2; i <= n; i++) v *= i; return v; }
        private static void Shuffle(char[] values, Random random) { for (int i = values.Length - 1; i > 0; i--) { int j = random.Next(i + 1); char c = values[i]; values[i] = values[j]; values[j] = c; } }
        private static void SwapRandom(char[] values, Random random) { int a = random.Next(values.Length), b = random.Next(values.Length); char c = values[a]; values[a] = values[b]; values[b] = c; }
        private static void MutateKey(char[] values, Random random)
        {
            if (values.Length != 25 || random.Next(3) == 0) { SwapRandom(values, random); return; }
            int kind = random.Next(5), a = random.Next(5), b = random.Next(5); if (a == b) b = (b + 1) % 5;
            if (kind == 0) for (int i = 0; i < 5; i++) { char c = values[a * 5 + i]; values[a * 5 + i] = values[b * 5 + i]; values[b * 5 + i] = c; }
            else if (kind == 1) for (int i = 0; i < 5; i++) { char c = values[i * 5 + a]; values[i * 5 + a] = values[i * 5 + b]; values[i * 5 + b] = c; }
            else if (kind == 2) Array.Reverse(values, a * 5, 5);
            else if (kind == 3) for (int i = 0; i < 2; i++) { char c = values[i * 5 + a]; values[i * 5 + a] = values[(4 - i) * 5 + a]; values[(4 - i) * 5 + a] = c; }
            else Array.Reverse(values);
        }
        private static string Heuristic(ToolRequest request)
        {
            string value = (request.Get("heuristic") ?? string.Empty).Trim().ToUpperInvariant();
            if (value == "爬山" || value == "HILL" || value == "HILLCLIMB") return "HILL";
            if (value == "延迟接受" || value == "LATE" || value == "LATE_ACCEPTANCE") return "LATE";
            if (value == "再加热退火" || value == "REHEAT") return "REHEAT";
            return "ANNEAL";
        }
        private static string HeuristicLabel(string value)
        {
            if (value == "HILL") return "爬山"; if (value == "LATE") return "延迟接受"; if (value == "REHEAT") return "再加热退火"; return "模拟退火";
        }
        private static double[] LateHistory(double score) { double[] values = new double[64]; for (int i = 0; i < values.Length; i++) values[i] = score; return values; }
        private static bool AcceptMove(string heuristic, double candidate, double current, int step, int iterations, double startTemperature, double scale, Random random, double[] late)
        {
            if (heuristic == "HILL") return candidate >= current;
            if (heuristic == "LATE") { int slot = step % late.Length; bool accept = candidate >= current || candidate >= late[slot]; late[slot] = accept ? candidate : current; return accept; }
            double fraction;
            if (heuristic == "REHEAT") { int cycle = Math.Max(512, iterations / 5); fraction = (step % cycle) / (double)cycle; }
            else fraction = step / (double)Math.Max(1, iterations);
            double temperature = startTemperature * (1.0 - fraction) + .12;
            return candidate >= current || random.NextDouble() < Math.Exp((candidate - current) / (Math.Max(.0001, scale) * temperature));
        }
        private static void Add(List<Candidate> list, Candidate c, int limit) { list.Add(c); list.Sort(delegate(Candidate x, Candidate y) { return y.Score.CompareTo(x.Score); }); if (list.Count > limit) list.RemoveAt(list.Count - 1); }
        private static string Format(List<Candidate> list) { list.Sort(delegate(Candidate x, Candidate y) { return y.Score.CompareTo(x.Score); }); StringBuilder s = new StringBuilder(); for (int i = 0; i < Math.Min(15, list.Count); i++) s.AppendFormat(CultureInfo.InvariantCulture, "#{0}  密钥 {1}  评分 {2:0.00}\r\n{3}\r\n\r\n", i + 1, list[i].Key, list[i].Score, list[i].Text); return s.ToString().TrimEnd(); }
        private static List<int[]> GrilleOrbits(int n) { List<int[]> result = new List<int[]>(); HashSet<int> seen = new HashSet<int>(); for (int p = 0; p < n * n; p++) { if (seen.Contains(p)) continue; int[] orbit = new int[4]; int v = p; for (int i = 0; i < 4; i++) { orbit[i] = v; seen.Add(v); int row = v / n, col = v % n; v = col * n + n - 1 - row; } result.Add(orbit); } return result; }
    }
}
