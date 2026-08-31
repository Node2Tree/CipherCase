using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class ClassicalAnalysis
    {
        private static readonly double[] English =
        {
            8.167,1.492,2.782,4.253,12.702,2.228,2.015,6.094,6.966,0.153,0.772,4.025,2.406,
            6.749,7.507,1.929,0.095,5.987,6.327,9.056,2.758,0.978,2.360,0.150,1.974,0.074
        };

        internal static string CrackCaesar(string input) { return CrackCaesar(input, "EN"); }
        internal static string CrackCaesar(string input, string language)
        {
            CaesarCipher cipher = new CaesarCipher();
            List<Candidate> candidates = new List<Candidate>();
            for (int shift = 0; shift < 26; shift++)
            {
                string text = cipher.Decrypt(input, shift.ToString(CultureInfo.InvariantCulture));
                candidates.Add(new Candidate(shift.ToString(CultureInfo.InvariantCulture), ScoreText(text, language), text));
            }
            return FormatCandidates(candidates, 26);
        }

        internal static string CrackAffine(string input) { return CrackAffine(input, "EN"); }
        internal static string CrackAffine(string input, string language)
        {
            AffineCipher cipher = new AffineCipher();
            List<Candidate> candidates = new List<Candidate>();
            for (int a = 1; a < 26; a++)
            {
                if (GreatestCommonDivisor(a, 26) != 1) continue;
                for (int b = 0; b < 26; b++)
                {
                    string key = a.ToString(CultureInfo.InvariantCulture) + "," + b.ToString(CultureInfo.InvariantCulture);
                    string text = cipher.Decrypt(input, key);
                    candidates.Add(new Candidate(key, ScoreText(text, language), text));
                }
            }
            return FormatCandidates(candidates, 20);
        }

        internal static string Frequency(string input)
        {
            return UnicodeAnalysis.Frequency(input);
        }

        internal static string Ngrams(string input, string nText)
        {
            int n;
            if (!int.TryParse(nText, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) || n < 1 || n > 8)
                throw new CipherException("N 须为 1–8");
            return UnicodeAnalysis.Ngrams(input, n);
        }

        internal static string IndexOfCoincidence(string input)
        {
            List<string> units = UnicodeAnalysis.Units(input); if (units.Count < 2) throw new CipherException("至少需要 2 个可分析字符"); double index = UnicodeAnalysis.Coincidence(units); string latin = UnicodeAnalysis.LatinLetters(input);
            return string.Format(CultureInfo.InvariantCulture,
                "字符数：{0}\r\n重合指数：{1:0.000000}\r\n文字体系：{2}{3}", units.Count, index, UnicodeAnalysis.ScriptSummary(input), latin.Length == units.Count ? "\r\n\r\n英语参考：约 0.066 单表，约 0.038 随机或多表" : "\r\n参考值随文字体系与有效字符集变化");
        }

        internal static string Kasiski(string input, string lengthText)
        {
            int length;
            if (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out length) || length < 3 || length > 8)
                throw new CipherException("序列长度须为 3–8");
            return UnicodeAnalysis.Kasiski(input, length);
        }

        private static string FormatCandidates(List<Candidate> candidates, int limit)
        {
            candidates.Sort(delegate(Candidate left, Candidate right) { return left.Score.CompareTo(right.Score); });
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < Math.Min(limit, candidates.Count); i++)
            {
                Candidate item = candidates[i];
                result.AppendFormat(CultureInfo.InvariantCulture, "#{0}  密钥 {1}  评分 {2:0.00}\r\n{3}\r\n\r\n", i + 1, item.Key, item.Score, item.Text);
            }
            return result.ToString().TrimEnd();
        }

        internal static string CrackVigenere(string input, string language)
        {
            return CrackVigenere(input, language, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        internal static string CrackVigenere(string input, string language, string minimumText, string maximumText, string knownLengthText, string partialKey, string crib)
        {
            string letters = Normalize(input);
            if (letters.Length < 30) throw new CipherException("自动破解至少需要 30 个英文字母");
            int maximum = Math.Min(20, Math.Max(1, letters.Length / 4)), minimum = 1, knownLength;
            int parsed; if (int.TryParse(minimumText, out parsed)) minimum = Math.Max(1, parsed); if (int.TryParse(maximumText, out parsed)) maximum = Math.Min(40, Math.Max(minimum, parsed));
            if (int.TryParse(knownLengthText, out knownLength) && knownLength > 0) { minimum = knownLength; maximum = knownLength; }
            string partial = NormalizePartialKey(partialKey); if (partial.Length > 0) { minimum = Math.Max(minimum, partial.Length); if (!string.IsNullOrWhiteSpace(knownLengthText)) maximum = minimum; }
            Dictionary<int, int> kasiski = KasiskiVotes(letters, maximum);
            List<KeyLengthCandidate> lengths = new List<KeyLengthCandidate>();
            double target = 0;
            foreach (double frequency in LanguageModels.GetFrequencies(language)) target += frequency * frequency / 10000.0;
            for (int length = minimum; length <= maximum; length++)
            {
                double average = 0;
                for (int column = 0; column < length; column++) average += ColumnIc(letters, column, length);
                average /= length;
                int votes = kasiski.ContainsKey(length) ? kasiski[length] : 0;
                lengths.Add(new KeyLengthCandidate(length, Math.Abs(average - target) - votes * 0.002));
            }
            lengths.Sort(delegate(KeyLengthCandidate a, KeyLengthCandidate b) { return a.Score.CompareTo(b.Score); });
            VigenereCipher cipher = new VigenereCipher();
            List<Candidate> results = new List<Candidate>();
            for (int i = 0; i < lengths.Count; i++)
            {
                int length = lengths[i].Length;
                StringBuilder key = new StringBuilder(length);
                for (int column = 0; column < length; column++) key.Append((char)('A' + BestColumnShift(letters, column, length, language)));
                string refined = RefineVigenereKey(letters, key.ToString(), language);
                if (partial.Length > 0) { char[] constrained = refined.ToCharArray(); for (int p = 0; p < Math.Min(partial.Length, constrained.Length); p++) if (partial[p] != '?') constrained[p] = partial[p]; refined = new string(constrained); }
                string plain = cipher.Decrypt(input, refined);
                double ranking = -LanguageModels.TextScore(Normalize(plain), language) + length * 4.0;
                string cleanCrib = Normalize(crib); if (cleanCrib.Length > 0 && Normalize(plain).IndexOf(cleanCrib, StringComparison.Ordinal) >= 0) ranking -= 500;
                results.Add(new Candidate(refined + " / 长度 " + length, ranking, plain));
            }
            return FormatCandidates(results, Math.Min(10, results.Count));
        }

        internal static string CrackMonoalphabetic(string input, string language)
        {
            return CrackMonoalphabetic(input, language, string.Empty, string.Empty);
        }

        internal static string CrackMonoalphabetic(string input, string language, string locksText, string iterationsText)
        {
            return CrackMonoalphabetic(input, language, locksText, iterationsText, null, null);
        }

        internal static string CrackMonoalphabetic(string input, string language, string locksText, string iterationsText, Action<int, string> progress, Func<bool> cancellation)
        {
            return CrackMonoalphabetic(input, language, locksText, iterationsText, "AUTO", progress, cancellation);
        }

        internal static string CrackMonoalphabetic(string input, string language, string locksText, string iterationsText, string matchMethod, Action<int, string> progress, Func<bool> cancellation)
        {
            string requestedLanguage = (language ?? string.Empty).Trim().ToUpperInvariant(); if (requestedLanguage == "ZH" || requestedLanguage == "ZH-CN" || requestedLanguage == "中文" || requestedLanguage == "CHINESE") return ChineseEncodedMonoCracker.Crack(input, iterationsText, progress, cancellation);
            if (UnicodeMonoalphabeticCracker.RequiresUnicodePath(input)) return UnicodeMonoalphabeticCracker.Crack(input, language, locksText, iterationsText, matchMethod, progress, cancellation);
            string cipher = Normalize(input);
            if (cipher.Length < 40) throw new CipherException("单表破解至少需要 40 个英文字母");
            int[] counts = new int[26];
            foreach (char value in cipher) counts[value - 'A']++;
            language = ResolveMonoLanguage(cipher, counts, language, matchMethod); List<int> cipherOrder = new List<int>();
            List<int> plainOrder = new List<int>();
            for (int i = 0; i < 26; i++) { cipherOrder.Add(i); plainOrder.Add(i); }
            cipherOrder.Sort(delegate(int a, int b) { return counts[b].CompareTo(counts[a]); });
            double[] frequencies = LanguageModels.GetFrequencies(language);
            plainOrder.Sort(delegate(int a, int b) { return frequencies[b].CompareTo(frequencies[a]); });
            char[] seed = new char[26];
            for (int i = 0; i < 26; i++) seed[cipherOrder[i]] = (char)('A' + plainOrder[i]);
            bool[] locked = new bool[26]; ApplyLocks(seed, locked, locksText);
            Random random = new Random(173);
            char[] bestKey = (char[])seed.Clone();
            double bestScore = LanguageModels.SequenceScore(ApplySubstitution(cipher, bestKey), language);
            int iterations; if (!int.TryParse(iterationsText, out iterations) || iterations < 500) iterations = 30000; iterations = Math.Min(250000, iterations);
            for (int restart = 0; restart < 10; restart++)
            {
                if (cancellation != null && cancellation()) throw new OperationCanceledException();
                if (progress != null) progress(restart * 10, "单表替换 · 搜索 " + (restart + 1) + "/10");
                char[] key = (char[])seed.Clone();
                for (int s = 0; s < restart * 7; s++) SwapRandom(key, locked, random);
                double score = LanguageModels.SequenceScore(ApplySubstitution(cipher, key), language);
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    if ((iteration & 511) == 0 && cancellation != null && cancellation()) throw new OperationCanceledException();
                    int a = random.Next(26), b = random.Next(26);
                    if (locked[a] || locked[b]) continue;
                    char swap = key[a]; key[a] = key[b]; key[b] = swap;
                    double next = LanguageModels.SequenceScore(ApplySubstitution(cipher, key), language);
                    double temperature = 8.0 * (1.0 - iteration / (double)iterations) + 0.08;
                    if (next >= score || random.NextDouble() < Math.Exp((next - score) / temperature)) score = next;
                    else { swap = key[a]; key[a] = key[b]; key[b] = swap; }
                    if (score > bestScore) { bestScore = score; bestKey = (char[])key.Clone(); }
                }
            }
            if (progress != null) progress(100, "单表替换 · 完成");
            string plainText = ApplySubstitutionPreserving(input, bestKey);
            return "#1  密钥 " + new string(bestKey) + " / " + language + " / " + LanguageModels.MatchMethodLabel(matchMethod, cipher.Length) + "  评分 " + (-bestScore).ToString("0.00", CultureInfo.InvariantCulture) + "\r\n密文表：ABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n明文表：" + new string(bestKey) + "\r\n" + plainText;
        }

        internal static string CrackScytale(string input, string language)
        {
            ScytaleCipher cipher = new ScytaleCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int width = 2; width <= Math.Min(40, Math.Max(2, (input ?? string.Empty).Length)); width++) { string plain = cipher.Decrypt(input, width.ToString(CultureInfo.InvariantCulture)); candidates.Add(new Candidate(width.ToString(CultureInfo.InvariantCulture), -LanguageModels.TextScore(Normalize(plain), language), plain)); }
            return FormatCandidates(candidates, 15);
        }

        internal static string CrackRedefence(string input, string language)
        {
            List<Candidate> candidates = new List<Candidate>(); for (int rails = 2; rails <= Math.Min(12, (input ?? string.Empty).Length); rails++) for (int offset = 0; offset < 2 * rails - 2; offset++) { string plain = RedefenceCipher.Decrypt(input, rails.ToString(), offset.ToString()); candidates.Add(new Candidate(rails + "," + offset, -LanguageModels.TextScore(Normalize(plain), language), plain)); } return FormatCandidates(candidates, 15);
        }

        internal static string CrackProgressiveCaesar(string input, string language)
        {
            ProgressiveCaesarCipher cipher = new ProgressiveCaesarCipher(); List<Candidate> candidates = new List<Candidate>(); for (int start = 0; start < 26; start++) { string plain = cipher.Decrypt(input, start.ToString()); candidates.Add(new Candidate(start.ToString(), -LanguageModels.TextScore(Normalize(plain), language), plain)); } return FormatCandidates(candidates, 15);
        }

        internal static string CrackVariantBeaufort(string input, string language)
        {
            string letters = Normalize(input); if (letters.Length < 30) throw new CipherException("破解至少需要 30 个字母"); VariantBeaufortCipher cipher = new VariantBeaufortCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int length = 1; length <= Math.Min(15, letters.Length / 4); length++) { StringBuilder key = new StringBuilder(); for (int column = 0; column < length; column++) key.Append((char)('A' + BestAdditiveShift(letters, column, length, language))); string plain = cipher.Decrypt(input, key.ToString()); candidates.Add(new Candidate(key + " / 长度 " + length, -LanguageModels.TextScore(Normalize(plain), language) + length * 3, plain)); } return FormatCandidates(candidates, 10);
        }

        internal static string CrackPorta(string input, string language)
        {
            string letters = Normalize(input); if (letters.Length < 30) throw new CipherException("Porta 破解至少需要 30 个字母"); PortaCipher cipher = new PortaCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int length = 1; length <= Math.Min(12, letters.Length / 4); length++) { StringBuilder key = new StringBuilder(); for (int column = 0; column < length; column++) { double best = double.MaxValue; int bestGroup = 0; for (int group = 0; group < 13; group++) { int[] counts = new int[26]; int total = 0; string trialKey = new string((char)('A' + group * 2), length); string plain = Normalize(cipher.Decrypt(input, trialKey)); for (int i = column; i < plain.Length; i += length) { counts[plain[i] - 'A']++; total++; } double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestGroup = group; } } key.Append((char)('A' + bestGroup * 2)); } string text = cipher.Decrypt(input, key.ToString()); candidates.Add(new Candidate(key + " / 长度 " + length, -LanguageModels.TextScore(Normalize(text), language) + length * 3, text)); } return FormatCandidates(candidates, 10);
        }

        internal static string CrackRotN(string input, string language)
        {
            RotNCipher cipher = new RotNCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int shift = 0; shift < 26; shift++)
            {
                string text = cipher.Decrypt(input, shift.ToString(CultureInfo.InvariantCulture));
                candidates.Add(new Candidate(shift.ToString(CultureInfo.InvariantCulture), -LanguageModels.TextScore(Normalize(text), language), text));
            }
            foreach (string variant in new[] { "47", "5", "18" })
            {
                string text = cipher.Decrypt(input, variant);
                candidates.Add(new Candidate("ROT" + variant, -LanguageModels.TextScore(Normalize(text), language), text));
            }
            return FormatCandidates(candidates, 15);
        }

        internal static string CrackRailFence(string input, string language)
        {
            RailFenceCipher cipher = new RailFenceCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int rails = 2; rails <= Math.Min(30, Math.Max(2, (input ?? string.Empty).Length)); rails++)
            {
                string text = cipher.Decrypt(input, rails.ToString(CultureInfo.InvariantCulture));
                candidates.Add(new Candidate(rails.ToString(CultureInfo.InvariantCulture), -LanguageModels.TextScore(Normalize(text), language), text));
            }
            return FormatCandidates(candidates, 15);
        }

        internal static string CrackRoute(string input, string language)
        {
            List<Candidate> candidates = new List<Candidate>(); string source = input ?? string.Empty;
            for (int width = 2; width <= Math.Min(40, source.Length); width++)
            {
                if (source.Length % width != 0) continue;
                string text = RouteCipher.Decrypt(source, width.ToString(CultureInfo.InvariantCulture));
                candidates.Add(new Candidate(width.ToString(CultureInfo.InvariantCulture), -LanguageModels.TextScore(Normalize(text), language), text));
            }
            if (candidates.Count == 0) throw new CipherException("没有可用的矩阵宽度");
            return FormatCandidates(candidates, 15);
        }

        internal static string CrackGronsfeld(string input, string language)
        {
            string letters = Normalize(input); if (letters.Length < 30) throw new CipherException("Gronsfeld 破解至少需要 30 个字母");
            GronsfeldCipher cipher = new GronsfeldCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int length = 1; length <= Math.Min(12, letters.Length / 4); length++)
            {
                char[] key = new char[length];
                for (int column = 0; column < length; column++) key[column] = (char)('0' + BestRestrictedShift(letters, column, length, language, 10, false));
                RefineGronsfeld(letters, key, language);
                string plain = cipher.Decrypt(input, new string(key));
                double score = -LanguageModels.TextScore(Normalize(plain), language) + length * 3;
                candidates.Add(new Candidate(new string(key) + " / 长度 " + length, score, plain));
            }
            return FormatCandidates(candidates, 10);
        }

        internal static string CrackBeaufort(string input, string language)
        {
            string letters = Normalize(input); if (letters.Length < 30) throw new CipherException("Beaufort 破解至少需要 30 个字母");
            BeaufortCipher cipher = new BeaufortCipher(); List<Candidate> candidates = new List<Candidate>();
            for (int length = 1; length <= Math.Min(15, letters.Length / 4); length++)
            {
                StringBuilder key = new StringBuilder();
                for (int column = 0; column < length; column++) key.Append((char)('A' + BestRestrictedShift(letters, column, length, language, 26, true)));
                string plain = cipher.Decrypt(input, key.ToString());
                double score = -LanguageModels.TextScore(Normalize(plain), language) + length * 3;
                candidates.Add(new Candidate(key + " / 长度 " + length, score, plain));
            }
            return FormatCandidates(candidates, 10);
        }

        private static int BestRestrictedShift(string text, int column, int step, string language, int limit, bool beaufort)
        {
            double best = double.MaxValue; int bestShift = 0;
            for (int shift = 0; shift < limit; shift++)
            {
                int[] counts = new int[26]; int total = 0;
                for (int i = column; i < text.Length; i += step)
                {
                    int cipher = text[i] - 'A'; int plain = beaufort ? Alphabet.Mod(shift - cipher, 26) : Alphabet.Mod(cipher - shift, 26);
                    counts[plain]++; total++;
                }
                double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestShift = shift; }
            }
            return bestShift;
        }

        private static void RefineGronsfeld(string cipherText, char[] key, string language)
        {
            for (int pass = 0; pass < 2; pass++) for (int position = 0; position < key.Length; position++)
            {
                char best = key[position]; double bestScore = double.MinValue;
                for (int shift = 0; shift < 10; shift++)
                {
                    key[position] = (char)('0' + shift); StringBuilder plain = new StringBuilder(cipherText.Length);
                    for (int i = 0; i < cipherText.Length; i++) plain.Append(Alphabet.FromIndex(cipherText[i] - 'A' - (key[i % key.Length] - '0'), false));
                    double score = LanguageModels.TextScore(plain.ToString(), language); if (score > bestScore) { bestScore = score; best = key[position]; }
                }
                key[position] = best;
            }
        }

        private static double ScoreText(string input, string language)
        {
            int[] counts = new int[26];
            int total = 0;
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (value >= 'A' && value <= 'Z') { counts[value - 'A']++; total++; }
            }
            return LanguageModels.ChiSquare(counts, total, language);
        }

        private static int BestColumnShift(string text, int column, int step, string language)
        {
            double best = double.MaxValue; int bestShift = 0;
            for (int shift = 0; shift < 26; shift++)
            {
                int[] counts = new int[26]; int total = 0;
                for (int i = column; i < text.Length; i += step) { counts[Alphabet.Mod(text[i] - 'A' - shift, 26)]++; total++; }
                double score = LanguageModels.ChiSquare(counts, total, language);
                if (score < best) { best = score; bestShift = shift; }
            }
            return bestShift;
        }
        private static int BestAdditiveShift(string text, int column, int step, string language)
        {
            double best = double.MaxValue; int bestShift = 0;
            for (int shift = 0; shift < 26; shift++) { int[] counts = new int[26]; int total = 0; for (int i = column; i < text.Length; i += step) { counts[Alphabet.Mod(text[i] - 'A' + shift, 26)]++; total++; } double score = LanguageModels.ChiSquare(counts, total, language); if (score < best) { best = score; bestShift = shift; } }
            return bestShift;
        }
        private static string RefineVigenereKey(string cipherText, string initialKey, string language)
        {
            char[] key = initialKey.ToCharArray();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int position = 0; position < key.Length; position++)
                {
                    char original = key[position], best = original; double bestScore = double.MinValue;
                    for (int shift = 0; shift < 26; shift++)
                    {
                        key[position] = (char)('A' + shift);
                        StringBuilder plain = new StringBuilder(cipherText.Length);
                        for (int i = 0; i < cipherText.Length; i++) plain.Append(Alphabet.FromIndex(cipherText[i] - 'A' - (key[i % key.Length] - 'A'), false));
                        double score = LanguageModels.TextScore(plain.ToString(), language);
                        if (score > bestScore) { bestScore = score; best = key[position]; }
                    }
                    key[position] = best;
                }
            }
            return new string(key);
        }
        private static double ColumnIc(string text, int column, int step)
        {
            int[] counts = new int[26]; int total = 0;
            for (int i = column; i < text.Length; i += step) { counts[text[i] - 'A']++; total++; }
            if (total < 2) return 0;
            double value = 0; foreach (int count in counts) value += count * (count - 1);
            return value / (total * (total - 1.0));
        }
        private static Dictionary<int, int> KasiskiVotes(string text, int maximum)
        {
            Dictionary<int, int> votes = new Dictionary<int, int>();
            Dictionary<string, int> last = new Dictionary<string, int>();
            for (int i = 0; i <= text.Length - 3; i++)
            {
                string gram = text.Substring(i, 3);
                if (last.ContainsKey(gram))
                {
                    int distance = i - last[gram];
                    for (int factor = 2; factor <= maximum; factor++) if (distance % factor == 0) votes[factor] = votes.ContainsKey(factor) ? votes[factor] + 1 : 1;
                }
                last[gram] = i;
            }
            return votes;
        }
        private static string ApplySubstitution(string input, char[] key)
        {
            StringBuilder result = new StringBuilder(input.Length); foreach (char value in input) result.Append(key[value - 'A']); return result.ToString();
        }
        private static string ResolveMonoLanguage(string cipher, int[] counts, string language, string method)
        {
            string requested = (language ?? string.Empty).Trim(); if (requested.Length > 0 && !string.Equals(requested, "AUTO", StringComparison.OrdinalIgnoreCase)) return LanguageModels.Normalize(requested); string selected = LanguageModels.NormalizeMatchMethod(method, cipher.Length); if (selected != "NGRAM") return LanguageModels.DetectSubstitutionLanguage(counts, cipher.Length, selected); string best = "EN"; double bestScore = double.NegativeInfinity; foreach (string candidate in LanguageModels.SupportedLanguages()) { char[] seed = MonoFrequencySeed(counts, candidate); double score = LanguageModels.SequenceScore(ApplySubstitution(cipher, seed), candidate) / Math.Max(1.0, cipher.Length); if (score > bestScore) { bestScore = score; best = candidate; } } return best;
        }
        private static char[] MonoFrequencySeed(int[] counts, string language)
        {
            List<int> cipher = new List<int>(), plain = new List<int>(); for (int i = 0; i < 26; i++) { cipher.Add(i); plain.Add(i); } cipher.Sort(delegate(int a, int b) { return counts[b].CompareTo(counts[a]); }); double[] frequencies = LanguageModels.GetFrequencies(language); plain.Sort(delegate(int a, int b) { return frequencies[b].CompareTo(frequencies[a]); }); char[] seed = new char[26]; for (int i = 0; i < 26; i++) seed[cipher[i]] = (char)('A' + plain[i]); return seed;
        }
        private static string ApplySubstitutionPreserving(string input, char[] key)
        {
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) result.Append(value);
                else { char mapped = key[char.ToUpperInvariant(value) - 'A']; result.Append(char.IsLower(value) ? char.ToLowerInvariant(mapped) : mapped); }
            }
            return result.ToString();
        }
        private static void SwapRandom(char[] key, Random random) { SwapRandom(key, new bool[26], random); }
        private static void SwapRandom(char[] key, bool[] locked, Random random) { int available = 0; for (int i = 0; i < locked.Length; i++) if (!locked[i]) available++; if (available < 2) return; int a, b; do { a = random.Next(26); } while (locked[a]); do { b = random.Next(26); } while (locked[b] || b == a); char value = key[a]; key[a] = key[b]; key[b] = value; }

        private static void ApplyLocks(char[] key, bool[] locked, string text)
        {
            string[] parts = (text ?? string.Empty).ToUpperInvariant().Split(new[] {',', ';', ' ', '\r', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries); HashSet<char> usedPlain = new HashSet<char>();
            foreach (string part in parts) { string[] pair = part.Split('='); if (pair.Length != 2 || pair[0].Length != 1 || pair[1].Length != 1 || pair[0][0] < 'A' || pair[0][0] > 'Z' || pair[1][0] < 'A' || pair[1][0] > 'Z') throw new CipherException("锁定映射格式示例：X=E,Q=T"); int cipher = pair[0][0] - 'A'; char plain = pair[1][0]; if (locked[cipher] || !usedPlain.Add(plain)) throw new CipherException("锁定映射存在冲突"); int other = Array.IndexOf(key, plain); char old = key[cipher]; key[cipher] = plain; key[other] = old; locked[cipher] = true; }
        }

        private static string NormalizePartialKey(string input)
        {
            StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if ((c >= 'A' && c <= 'Z') || c == '?') result.Append(c); } return result.ToString();
        }

        private static string Normalize(string input)
        {
            StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                char value = char.ToUpperInvariant(raw);
                if (value >= 'A' && value <= 'Z') result.Append(value);
            }
            return result.ToString();
        }

        private static int GreatestCommonDivisor(int left, int right)
        {
            while (right != 0) { int value = left % right; left = right; right = value; }
            return Math.Abs(left);
        }

        private sealed class Candidate
        {
            internal Candidate(string key, double score, string text) { Key = key; Score = score; Text = text; }
            internal string Key { get; private set; }
            internal double Score { get; private set; }
            internal string Text { get; private set; }
        }
        private sealed class KeyLengthCandidate
        {
            internal KeyLengthCandidate(int length, double score) { Length = length; Score = score; }
            internal int Length { get; private set; }
            internal double Score { get; private set; }
        }
    }
}
