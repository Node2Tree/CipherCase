using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class UnicodeMonoalphabeticCracker
    {
        private sealed class Token { internal string Raw; internal int Symbol = -1; }
        private sealed class Candidate { internal char[] Key; internal string Language; internal double Score; internal double FinalScore; internal WordSegmentation Segmentation; internal int Stability; internal int Attempts; }
        private sealed class SearchChain { internal char[] Key; internal double Score; internal char[] BestKey; internal double BestScore; internal double Temperature; }

        internal static bool RequiresUnicodePath(string input)
        {
            TextElementEnumerator items = StringInfo.GetTextElementEnumerator(input ?? string.Empty); while (items.MoveNext()) { string value = items.GetTextElement(); if (IsCipherSymbol(value) && !IsAsciiLetter(value)) return true; } return false;
        }

        internal static string Crack(string input, string language, string locksText, string iterationsText, string matchMethod, Action<int, string> progress, Func<bool> cancellation)
        {
            List<string> symbols = new List<string>(); List<Token> tokens = Tokenize(input, symbols); int encodedLength = 0; foreach (Token token in tokens) if (token.Symbol >= 0) encodedLength++; if (encodedLength < 40) throw new CipherException("单表破解至少需要 40 个有效字符"); if (symbols.Count > 26) throw new CipherException("单表破解最多支持 26 个不同密文符号；当前为 " + symbols.Count);
            StringBuilder encodedBuilder = new StringBuilder(encodedLength); foreach (Token token in tokens) if (token.Symbol >= 0) encodedBuilder.Append((char)('A' + token.Symbol)); string encoded = encodedBuilder.ToString(); int[] counts = new int[26]; foreach (char c in encoded) counts[c - 'A']++;
            List<string> languages = ResolveLanguages(counts, encoded.Length, language, matchMethod); bool spaceless = IsSpaceless(tokens, encodedLength); int totalBudget; if (!int.TryParse(iterationsText, out totalBudget) || totalBudget < 500) totalBudget = 30000; totalBudget = Math.Min(5000000, totalBudget); List<Candidate> candidates = new List<Candidate>(); int consumed = 0;
            for (int languageIndex = 0; languageIndex < languages.Count; languageIndex++)
            {
                string candidateLanguage = languages[languageIndex]; int languageBudget = totalBudget / languages.Count + (languageIndex < totalBudget % languages.Count ? 1 : 0); char[] seed = FrequencySeed(counts, candidateLanguage); bool[] locked = new bool[26]; ApplyLocks(seed, locked, locksText, symbols); int unlocked = 0, activeUnlocked = 0; for (int i = 0; i < locked.Length; i++) if (!locked[i]) { unlocked++; if (i < symbols.Count) activeUnlocked++; }
                double seedScore = SearchScore(encoded, seed, candidateLanguage, spaceless); candidates.Add(new Candidate { Key = (char[])seed.Clone(), Language = candidateLanguage, Score = seedScore, FinalScore = seedScore });
                if (unlocked >= 2 && activeUnlocked >= 1) SearchParallelTempering(encoded, symbols.Count, candidateLanguage, spaceless, seed, locked, languageBudget, consumed, totalBudget, candidates, progress, cancellation);
                consumed += languageBudget;
            }
            List<Candidate> finalists = SelectFinalists(candidates, languages, symbols.Count, encoded, spaceless); if (progress != null) progress(100, "单表替换 · 完成"); StringBuilder cipherTable = new StringBuilder(); for (int i = 0; i < symbols.Count; i++) { if (i > 0) cipherTable.Append(' '); cipherTable.Append(symbols[i]); } StringBuilder result = new StringBuilder();
            for (int rank = 0; rank < finalists.Count; rank++)
            {
                Candidate item = finalists[rank]; StringBuilder plainTable = new StringBuilder(); for (int i = 0; i < symbols.Count; i++) { if (i > 0) plainTable.Append(' '); plainTable.Append(item.Key[i]); } string plain = Render(tokens, item.Key); if (rank > 0) result.Append("\r\n\r\n"); result.Append('#').Append(rank + 1).Append("  密钥 Unicode映射 / ").Append(item.Language).Append(" / ").Append(LanguageModels.MatchMethodLabel(matchMethod, encoded.Length)).Append("  评分 ").Append((-item.Score).ToString("0.00", CultureInfo.InvariantCulture)).Append("  共识 ").Append(item.Stability).Append('/').Append(item.Attempts).Append("  置信 ").Append(Confidence(item)); if (item.Segmentation != null) result.Append("  词覆盖 ").Append((item.Segmentation.Coverage * 100).ToString("0", CultureInfo.InvariantCulture)).Append('%'); result.Append("\r\n密文表：").Append(cipherTable).Append("\r\n明文表：").Append(plainTable).Append("\r\n"); if (item.Segmentation != null) result.Append("分词：").Append(item.Segmentation.Text).Append("\r\n原串："); result.Append(plain);
            }
            return result.ToString();
        }

        private static List<string> ResolveLanguages(int[] counts, int total, string requested, string method)
        {
            List<string> result = new List<string>(); if (!string.IsNullOrWhiteSpace(requested) && !string.Equals(requested.Trim(), "AUTO", StringComparison.OrdinalIgnoreCase)) { result.Add(LanguageModels.Normalize(requested)); return result; } string[] ranked = LanguageModels.RankSubstitutionLanguages(counts, total, method, 3); foreach (string value in ranked) if (!result.Contains(value)) result.Add(value); if (!result.Contains("EN")) { if (result.Count >= 3) result.RemoveAt(result.Count - 1); result.Add("EN"); } return result;
        }

        private static double SearchScore(string encoded, char[] key, string language, bool spaceless) { return spaceless ? LanguageModels.SpacelessSubstitutionScore(encoded, key, language) : LanguageModels.SubstitutionScore(Apply(encoded, key), language); }
        private static bool IsSpaceless(List<Token> tokens, int encodedLength) { if (encodedLength < 80) return false; foreach (Token token in tokens) if (token.Symbol < 0 && !string.IsNullOrEmpty(token.Raw) && char.IsWhiteSpace(token.Raw, 0)) return false; return true; }

        private static void SearchParallelTempering(string encoded, int activeCount, string language, bool spaceless, char[] seed, bool[] locked, int budget, int completedBefore, int totalBudget, List<Candidate> candidates, Action<int, string> progress, Func<bool> cancellation)
        {
            int wordBudget = spaceless && language == "EN" && budget >= 5000 ? Math.Min(40000, Math.Max(3000, budget / 20)) : 0, characterBudget = budget - wordBudget; int chainCount = characterBudget < 4000 ? 4 : 8; double[] temperatures = { .06, .16, .42, 1.1, 2.9, 7.5, 19.0, 48.0 }; SearchChain[] chains = new SearchChain[chainCount]; Random random = new Random(7919 + language[0] * 131 + encoded.Length * 17); char[] globalBest = (char[])seed.Clone(); double globalBestScore = SearchScore(encoded, globalBest, language, spaceless);
            for (int i = 0; i < chainCount; i++)
            {
                char[] key = (char[])seed.Clone(); for (int s = 0; s < i * 13 + (i == 0 ? 0 : 9); s++) SwapRandom(key, locked, random, activeCount); double score = SearchScore(encoded, key, language, spaceless); chains[i] = new SearchChain { Key = key, Score = score, BestKey = (char[])key.Clone(), BestScore = score, Temperature = temperatures[i] }; if (score > globalBestScore) { globalBestScore = score; globalBest = (char[])key.Clone(); }
            }
            int exchangeParity = 0, reportEvery = Math.Max(512, budget / 200), kickEvery = Math.Max(4096, characterBudget / 8);
            for (int proposal = 0; proposal < characterBudget; proposal++)
            {
                if ((proposal & 1023) == 0 && cancellation != null && cancellation()) throw new OperationCanceledException(); SearchChain chain = chains[proposal % chainCount]; int a = RandomUnlocked(random, locked, activeCount, -1, -1), b = RandomUnlocked(random, locked, 26, a, -1), c = -1; bool cycle = random.Next(100) < 24 && CountUnlocked(locked) >= 3; if (cycle) c = RandomUnlocked(random, locked, 26, a, b);
                if (cycle) CycleForward(chain.Key, a, b, c); else Swap(chain.Key, a, b); double next = SearchScore(encoded, chain.Key, language, spaceless); if (next >= chain.Score || random.NextDouble() < Math.Exp(Math.Max(-700.0, (next - chain.Score) / chain.Temperature))) chain.Score = next; else { if (cycle) CycleBackward(chain.Key, a, b, c); else Swap(chain.Key, a, b); }
                if (chain.Score > chain.BestScore) { chain.BestScore = chain.Score; chain.BestKey = (char[])chain.Key.Clone(); } if (chain.Score > globalBestScore) { globalBestScore = chain.Score; globalBest = (char[])chain.Key.Clone(); }
                if ((proposal + 1) % (chainCount * 32) == 0) { for (int i = exchangeParity; i + 1 < chainCount; i += 2) TryExchange(chains[i], chains[i + 1], random); exchangeParity = 1 - exchangeParity; }
                if ((proposal + 1) % kickEvery == 0 && proposal + 1 < characterBudget) { SearchChain hot = chains[chainCount - 1]; hot.Key = (char[])globalBest.Clone(); for (int s = 0; s < 18 + chainCount; s++) SwapRandom(hot.Key, locked, random, activeCount); hot.Score = SearchScore(encoded, hot.Key, language, spaceless); }
                if ((proposal + 1) % reportEvery == 0 && progress != null) { int done = completedBefore + proposal + 1; progress(Math.Min(99, done * 100 / Math.Max(1, totalBudget)), "单表替换 · " + language + " · 字符模型 " + (proposal + 1).ToString("N0", CultureInfo.InvariantCulture) + "/" + characterBudget.ToString("N0", CultureInfo.InvariantCulture)); }
            }
            for (int i = 0; i < chainCount; i++) candidates.Add(new Candidate { Key = chains[i].BestKey, Language = language, Score = chains[i].BestScore, FinalScore = chains[i].BestScore }); char[] polished = GreedyPolish(encoded, globalBest, locked, activeCount, language, spaceless); double polishedScore = SearchScore(encoded, polished, language, spaceless); candidates.Add(new Candidate { Key = polished, Language = language, Score = polishedScore, FinalScore = polishedScore });
            if (wordBudget > 0)
            {
                if (progress != null) progress(Math.Min(99, (completedBefore + characterBudget) * 100 / Math.Max(1, totalBudget)), "单表替换 · " + language + " · 词形束搜索"); char[] wordSeed = polished; WordSegmentation wordSeedSegment; double wordSeedScore = WordObjective(encoded, wordSeed, language, out wordSeedSegment); List<char[]> beamKeys = SubstitutionWordBeam.Search(encoded, seed, locked, activeCount, budget >= 500000 ? 700 : 350, cancellation);
                foreach (char[] beamKey in beamKeys) { WordSegmentation segment; double combined = WordObjective(encoded, beamKey, language, out segment); double character = SearchScore(encoded, beamKey, language, true); candidates.Add(new Candidate { Key = beamKey, Language = language, Score = character, FinalScore = combined, Segmentation = segment }); if (combined > wordSeedScore) { wordSeedScore = combined; wordSeed = beamKey; wordSeedSegment = segment; } }
                WordGuidedSearch(encoded, activeCount, language, wordSeed, locked, wordBudget, completedBefore + characterBudget, totalBudget, candidates, progress, cancellation, random);
            }
        }

        private static void WordGuidedSearch(string encoded, int activeCount, string language, char[] seed, bool[] locked, int budget, int completedBefore, int totalBudget, List<Candidate> candidates, Action<int, string> progress, Func<bool> cancellation, Random random)
        {
            const int chainCount = 4; double[] temperatures = { .35, 1.4, 6.0, 25.0 }; SearchChain[] chains = new SearchChain[chainCount]; WordSegmentation[] bestSegments = new WordSegmentation[chainCount];
            for (int i = 0; i < chainCount; i++) { char[] key = (char[])seed.Clone(); for (int s = 0; s < i * 5; s++) SwapRandom(key, locked, random, activeCount); WordSegmentation segment; double score = WordObjective(encoded, key, language, out segment); chains[i] = new SearchChain { Key = key, Score = score, BestKey = (char[])key.Clone(), BestScore = score, Temperature = temperatures[i] }; bestSegments[i] = segment; }
            int parity = 0, reportEvery = Math.Max(256, budget / 100);
            for (int proposal = 0; proposal < budget; proposal++)
            {
                if ((proposal & 255) == 0 && cancellation != null && cancellation()) throw new OperationCanceledException(); SearchChain chain = chains[proposal % chainCount]; int a = RandomUnlocked(random, locked, activeCount, -1, -1), b = RandomUnlocked(random, locked, 26, a, -1), c = -1; bool cycle = random.Next(100) < 18 && CountUnlocked(locked) >= 3; if (cycle) c = RandomUnlocked(random, locked, 26, a, b); if (cycle) CycleForward(chain.Key, a, b, c); else Swap(chain.Key, a, b); WordSegmentation segment; double next = WordObjective(encoded, chain.Key, language, out segment); if (next >= chain.Score || random.NextDouble() < Math.Exp(Math.Max(-700.0, (next - chain.Score) / chain.Temperature))) chain.Score = next; else { if (cycle) CycleBackward(chain.Key, a, b, c); else Swap(chain.Key, a, b); }
                if (chain.Score > chain.BestScore) { chain.BestScore = chain.Score; chain.BestKey = (char[])chain.Key.Clone(); bestSegments[proposal % chainCount] = segment; }
                if ((proposal + 1) % (chainCount * 24) == 0) { for (int i = parity; i + 1 < chainCount; i += 2) TryExchange(chains[i], chains[i + 1], random); parity = 1 - parity; }
                if ((proposal + 1) % reportEvery == 0 && progress != null) { int done = completedBefore + proposal + 1; progress(Math.Min(99, done * 100 / Math.Max(1, totalBudget)), "单表替换 · " + language + " · 联合分词 " + (proposal + 1).ToString("N0", CultureInfo.InvariantCulture) + "/" + budget.ToString("N0", CultureInfo.InvariantCulture)); }
            }
            for (int i = 0; i < chainCount; i++) { double characterScore = SearchScore(encoded, chains[i].BestKey, language, true); WordSegmentation segment = SpacelessWordSegmenter.Segment(Apply(encoded, chains[i].BestKey)); candidates.Add(new Candidate { Key = chains[i].BestKey, Language = language, Score = characterScore, FinalScore = chains[i].BestScore, Segmentation = segment }); }
        }

        private static double WordObjective(string encoded, char[] key, string language, out WordSegmentation segmentation) { double character = SearchScore(encoded, key, language, true); segmentation = SpacelessWordSegmenter.Segment(Apply(encoded, key)); return character + segmentation.Score * .85 + segmentation.Coverage * 110.0; }

        private static char[] GreedyPolish(string encoded, char[] start, bool[] locked, int activeCount, string language, bool spaceless)
        {
            char[] key = (char[])start.Clone(); double score = SearchScore(encoded, key, language, spaceless);
            for (int pass = 0; pass < 8; pass++)
            {
                int bestA = -1, bestB = -1; double best = score;
                for (int a = 0; a < activeCount; a++) if (!locked[a]) for (int b = 0; b < 26; b++) if (!locked[b] && b != a) { Swap(key, a, b); double next = SearchScore(encoded, key, language, spaceless); Swap(key, a, b); if (next > best) { best = next; bestA = a; bestB = b; } }
                if (bestA < 0) break; Swap(key, bestA, bestB); score = best;
            }
            return key;
        }

        private static int CountUnlocked(bool[] locked) { int count = 0; for (int i = 0; i < locked.Length; i++) if (!locked[i]) count++; return count; }
        private static int RandomUnlocked(Random random, bool[] locked, int limit, int excludedA, int excludedB) { int value; do { value = random.Next(limit); } while (locked[value] || value == excludedA || value == excludedB); return value; }
        private static void Swap(char[] key, int a, int b) { char value = key[a]; key[a] = key[b]; key[b] = value; }
        private static void CycleForward(char[] key, int a, int b, int c) { char value = key[a]; key[a] = key[b]; key[b] = key[c]; key[c] = value; }
        private static void CycleBackward(char[] key, int a, int b, int c) { char value = key[c]; key[c] = key[b]; key[b] = key[a]; key[a] = value; }
        private static void TryExchange(SearchChain colder, SearchChain hotter, Random random) { double exponent = (1.0 / colder.Temperature - 1.0 / hotter.Temperature) * (hotter.Score - colder.Score); if (exponent >= 0 || random.NextDouble() < Math.Exp(Math.Max(-700.0, exponent))) { char[] key = colder.Key; colder.Key = hotter.Key; hotter.Key = key; double score = colder.Score; colder.Score = hotter.Score; hotter.Score = score; } }

        private static List<Candidate> SelectFinalists(List<Candidate> candidates, List<string> languages, int activeCount, string encoded, bool spaceless)
        {
            Dictionary<string, List<Candidate>> groups = new Dictionary<string, List<Candidate>>(); foreach (string language in languages) groups[language] = new List<Candidate>();
            foreach (string language in languages)
            {
                List<Candidate> pool = new List<Candidate>(); foreach (Candidate candidate in candidates) if (candidate.Language == language) { if (spaceless && language == "EN") { candidate.Segmentation = SpacelessWordSegmenter.Segment(Apply(encoded, candidate.Key)); candidate.FinalScore = candidate.Score + candidate.Segmentation.Coverage * 110.0 + candidate.Segmentation.Score * .85; } else candidate.FinalScore = candidate.Score; pool.Add(candidate); } pool.Sort(delegate(Candidate a, Candidate b) { return b.FinalScore.CompareTo(a.FinalScore); }); HashSet<string> keys = new HashSet<string>();
                foreach (Candidate candidate in pool)
                {
                    string signature = new string(candidate.Key, 0, activeCount); if (!keys.Add(signature)) continue; candidate.Attempts = pool.Count; candidate.Stability = Stability(candidate, pool, activeCount); groups[language].Add(candidate); if (groups[language].Count >= 8) break;
                }
                groups[language].Sort(delegate(Candidate a, Candidate b) { return b.FinalScore.CompareTo(a.FinalScore); });
            }
            List<Candidate> result = new List<Candidate>(); for (int level = 0; result.Count < 8; level++) { bool added = false; foreach (string language in languages) { if (level >= groups[language].Count) continue; result.Add(groups[language][level]); added = true; if (result.Count >= 8) break; } if (!added) break; } return result;
        }

        private static int Stability(Candidate target, List<Candidate> candidates, int activeCount) { int count = 0; foreach (Candidate candidate in candidates) { int different = 0; for (int i = 0; i < activeCount; i++) if (candidate.Key[i] != target.Key[i] && ++different > 2) break; if (different <= 2) count++; } return count; }
        private static string Confidence(Candidate candidate) { if (candidate.Segmentation != null && candidate.Segmentation.Coverage < .55) return "低"; if (candidate.Stability >= Math.Max(4, candidate.Attempts / 3)) return "高"; if (candidate.Stability >= Math.Max(2, candidate.Attempts / 8)) return "中"; return "低"; }

        private static List<Token> Tokenize(string input, List<string> symbols)
        {
            string source = (input ?? string.Empty).Normalize(NormalizationForm.FormC); List<string> elements = new List<string>(), nonSpace = new List<string>(); TextElementEnumerator scan = StringInfo.GetTextElementEnumerator(source); while (scan.MoveNext()) { string raw = scan.GetTextElement(); elements.Add(raw); UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(raw, 0); if (!char.IsWhiteSpace(raw, 0) && category != UnicodeCategory.Control && category != UnicodeCategory.Format) nonSpace.Add(raw.ToUpperInvariant()); } List<string> unique = new List<string>(); foreach (string value in nonSpace) if (!unique.Contains(value)) unique.Add(value); bool compactSymbols = unique.Count > 0 && unique.Count <= 26 && IsPredominantlySymbolic(nonSpace);
            List<Token> result = new List<Token>(); foreach (string raw in elements) { Token token = new Token { Raw = raw }; if (IsCipherSymbol(raw, compactSymbols)) { string normalized = raw.ToUpperInvariant(); int index = symbols.IndexOf(normalized); if (index < 0) { symbols.Add(normalized); index = symbols.Count - 1; } token.Symbol = index; } result.Add(token); } return result;
        }
        private static bool IsCipherSymbol(string value) { return IsCipherSymbol(value, false); }
        private static bool IsCipherSymbol(string value, bool compact) { if (string.IsNullOrEmpty(value) || char.IsWhiteSpace(value, 0)) return false; UnicodeCategory c = CharUnicodeInfo.GetUnicodeCategory(value, 0); if (c == UnicodeCategory.Control || c == UnicodeCategory.Format || c == UnicodeCategory.LineSeparator || c == UnicodeCategory.ParagraphSeparator || c == UnicodeCategory.SpaceSeparator) return false; if (compact) return true; return c == UnicodeCategory.UppercaseLetter || c == UnicodeCategory.LowercaseLetter || c == UnicodeCategory.TitlecaseLetter || c == UnicodeCategory.ModifierLetter || c == UnicodeCategory.OtherLetter || c == UnicodeCategory.DecimalDigitNumber || c == UnicodeCategory.LetterNumber || c == UnicodeCategory.OtherNumber || c == UnicodeCategory.MathSymbol || c == UnicodeCategory.CurrencySymbol || c == UnicodeCategory.ModifierSymbol || c == UnicodeCategory.OtherSymbol; }
        private static bool IsPredominantlySymbolic(List<string> values) { if (values.Count == 0) return false; int nonAscii = 0; foreach (string value in values) if (!IsAsciiLetter(value)) nonAscii++; return nonAscii >= values.Count * .8; }
        private static bool IsAsciiLetter(string value) { return value.Length == 1 && ((value[0] >= 'A' && value[0] <= 'Z') || (value[0] >= 'a' && value[0] <= 'z')); }
        private static string Apply(string encoded, char[] key) { StringBuilder r = new StringBuilder(encoded.Length); foreach (char c in encoded) r.Append(key[c - 'A']); return r.ToString(); }
        private static string Render(List<Token> tokens, char[] key) { StringBuilder r = new StringBuilder(); foreach (Token token in tokens) { if (token.Symbol < 0) r.Append(token.Raw); else { char c = key[token.Symbol]; r.Append(IsLower(token.Raw) ? char.ToLowerInvariant(c) : c); } } return r.ToString(); }
        private static bool IsLower(string value) { if (string.IsNullOrEmpty(value)) return false; UnicodeCategory c = CharUnicodeInfo.GetUnicodeCategory(value, 0); return c == UnicodeCategory.LowercaseLetter; }
        private static char[] FrequencySeed(int[] counts, string language) { List<int> cipher = new List<int>(), plain = new List<int>(); for (int i = 0; i < 26; i++) { cipher.Add(i); plain.Add(i); } cipher.Sort(delegate(int a, int b) { return counts[b].CompareTo(counts[a]); }); double[] frequencies = LanguageModels.GetFrequencies(language); plain.Sort(delegate(int a, int b) { return frequencies[b].CompareTo(frequencies[a]); }); char[] seed = new char[26]; for (int i = 0; i < 26; i++) seed[cipher[i]] = (char)('A' + plain[i]); return seed; }
        private static void SwapRandom(char[] key, bool[] locked, Random random, int activeCount) { int available = 0; for (int i = 0; i < activeCount; i++) if (!locked[i]) available++; if (available < 1) return; int a, b; do { a = random.Next(activeCount); } while (locked[a]); do { b = random.Next(26); } while (locked[b] || b == a); char value = key[a]; key[a] = key[b]; key[b] = value; }
        private static void ApplyLocks(char[] key, bool[] locked, string text, List<string> symbols)
        {
            string[] parts = (text ?? string.Empty).Split(new[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries); HashSet<char> usedPlain = new HashSet<char>(); foreach (string part in parts) { int equals = part.IndexOf('='); if (equals <= 0 || equals != part.LastIndexOf('=')) throw new CipherException("锁定映射格式示例：Ж=E,Ω=T"); string left = part.Substring(0, equals).Normalize(NormalizationForm.FormC).ToUpperInvariant(), right = part.Substring(equals + 1).ToUpperInvariant(); if (TextElementCount(left) != 1 || right.Length != 1 || right[0] < 'A' || right[0] > 'Z') throw new CipherException("锁定映射格式示例：Ж=E,Ω=T"); int cipher = symbols.IndexOf(left); if (cipher < 0) throw new CipherException("锁定符号未出现在密文中：" + left); char plain = right[0]; if (locked[cipher] || !usedPlain.Add(plain)) throw new CipherException("锁定映射存在冲突"); int other = Array.IndexOf(key, plain); char old = key[cipher]; key[cipher] = plain; key[other] = old; locked[cipher] = true; }
        }
        private static int TextElementCount(string value) { return new StringInfo(value ?? string.Empty).LengthInTextElements; }
    }
}
