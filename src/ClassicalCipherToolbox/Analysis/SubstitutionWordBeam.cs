using System;
using System.Collections.Generic;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class SubstitutionWordBeam
    {
        private sealed class WordEntry { internal string Text; internal double Score; }
        private sealed class BeamState { internal int Position; internal int[] Map; internal bool[] Used; internal double Score; }
        private static readonly Dictionary<string, List<WordEntry>> Words = BuildWords();
        private static readonly int[] PlainOrder = BuildPlainOrder();

        internal static List<char[]> Search(string encoded, char[] seed, bool[] locked, int activeCount, int beamWidth, Func<bool> cancellation)
        {
            List<BeamState>[] at = new List<BeamState>[encoded.Length + 1]; for (int i = 0; i < at.Length; i++) at[i] = new List<BeamState>(); int[] initialMap = new int[26]; for (int i = 0; i < 26; i++) initialMap[i] = -1; bool[] initialUsed = new bool[26]; for (int i = 0; i < activeCount; i++) if (locked[i]) { int value = seed[i] - 'A'; initialMap[i] = value; initialUsed[value] = true; } at[0].Add(new BeamState { Position = 0, Map = initialMap, Used = initialUsed, Score = 0 });
            for (int position = 0; position < encoded.Length; position++)
            {
                if ((position & 7) == 0 && cancellation != null && cancellation()) throw new OperationCanceledException(); List<BeamState> states = Prune(at[position], beamWidth); if (states.Count == 0) continue;
                foreach (BeamState state in states)
                {
                    int maximum = Math.Min(14, encoded.Length - position);
                    for (int length = 1; length <= maximum; length++)
                    {
                        List<WordEntry> entries; if (!Words.TryGetValue(PatternKey(encoded, position, length), out entries)) continue; int limit = Math.Min(entries.Count, 80);
                        for (int i = 0; i < limit; i++) { BeamState next; if (TryWord(state, encoded, position, entries[i], out next)) Add(at[position + length], next, beamWidth); }
                    }
                    AddUnknown(state, encoded[position] - 'A', at[position + 1], beamWidth);
                }
            }
            List<BeamState> finals = Prune(at[encoded.Length], Math.Max(beamWidth, 24)); finals.Sort(delegate(BeamState a, BeamState b) { return b.Score.CompareTo(a.Score); }); List<char[]> result = new List<char[]>(); HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < finals.Count && result.Count < 16; i++) { char[] key = Complete(finals[i].Map, seed); string signature = new string(key, 0, activeCount); if (seen.Add(signature)) result.Add(key); }
            return result;
        }

        private static bool TryWord(BeamState state, string encoded, int position, WordEntry word, out BeamState result)
        {
            int[] map = (int[])state.Map.Clone(); bool[] used = (bool[])state.Used.Clone(); for (int i = 0; i < word.Text.Length; i++) { int cipher = encoded[position + i] - 'A', plain = word.Text[i] - 'A'; if (map[cipher] >= 0) { if (map[cipher] != plain) { result = null; return false; } } else { if (used[plain]) { result = null; return false; } map[cipher] = plain; used[plain] = true; } } result = new BeamState { Position = position + word.Text.Length, Map = map, Used = used, Score = state.Score + word.Score }; return true;
        }

        private static void AddUnknown(BeamState state, int cipher, List<BeamState> destination, int beamWidth)
        {
            if (state.Map[cipher] >= 0) { Add(destination, new BeamState { Position = state.Position + 1, Map = state.Map, Used = state.Used, Score = state.Score - 4.8 }, beamWidth); return; }
            int added = 0; foreach (int plain in PlainOrder) { if (state.Used[plain]) continue; int[] map = (int[])state.Map.Clone(); bool[] used = (bool[])state.Used.Clone(); map[cipher] = plain; used[plain] = true; Add(destination, new BeamState { Position = state.Position + 1, Map = map, Used = used, Score = state.Score - 6.2 }, beamWidth); if (++added >= 6) break; }
        }

        private static void Add(List<BeamState> list, BeamState state, int beamWidth) { list.Add(state); if (list.Count > beamWidth * 4) { List<BeamState> reduced = Prune(list, beamWidth * 2); list.Clear(); list.AddRange(reduced); } }
        private static List<BeamState> Prune(List<BeamState> source, int limit)
        {
            source.Sort(delegate(BeamState a, BeamState b) { return b.Score.CompareTo(a.Score); }); List<BeamState> result = new List<BeamState>(); HashSet<string> signatures = new HashSet<string>(); foreach (BeamState state in source) { string signature = Signature(state.Map); if (!signatures.Add(signature)) continue; result.Add(state); if (result.Count >= limit) break; } return result;
        }

        private static char[] Complete(int[] map, char[] seed)
        {
            char[] result = new char[26]; bool[] used = new bool[26]; for (int i = 0; i < 26; i++) if (map[i] >= 0) { result[i] = (char)('A' + map[i]); used[map[i]] = true; }
            for (int i = 0; i < 26; i++) if (map[i] < 0) { int preferred = seed[i] - 'A'; if (used[preferred]) { preferred = 0; while (preferred < 26 && used[preferred]) preferred++; } result[i] = (char)('A' + preferred); used[preferred] = true; } return result;
        }

        private static string Signature(int[] map) { char[] value = new char[26]; for (int i = 0; i < 26; i++) value[i] = map[i] < 0 ? '?' : (char)('A' + map[i]); return new string(value); }
        private static string PatternKey(string value, int start, int length) { Dictionary<char, char> seen = new Dictionary<char, char>(); char next = 'A'; char[] pattern = new char[length]; for (int i = 0; i < length; i++) { char source = value[start + i], mapped; if (!seen.TryGetValue(source, out mapped)) { mapped = next++; seen[source] = mapped; } pattern[i] = mapped; } return length + ":" + new string(pattern); }
        private static int[] BuildPlainOrder() { const string order = "ETAOINSHRDLCUMWFGYPBVKJXQZ"; int[] result = new int[26]; for (int i = 0; i < 26; i++) result[i] = order[i] - 'A'; return result; }

        private static Dictionary<string, List<WordEntry>> BuildWords()
        {
            Dictionary<string, List<WordEntry>> result = new Dictionary<string, List<WordEntry>>(); HashSet<string> seen = new HashSet<string>(); List<string> all = new List<string>(); all.AddRange(new[] { "A", "I", "AM", "AN", "AS", "AT", "BE", "BY", "DO", "GO", "HE", "IF", "IN", "IS", "IT", "ME", "MY", "NO", "OF", "ON", "OR", "SO", "TO", "UP", "US", "WE", "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "HIS", "ONE", "OUR", "OUT", "WHO", "HOW", "WHY" }); all.AddRange(EnglishNgramData.LoadWords());
            for (int rank = 0; rank < all.Count; rank++) { string word = all[rank].Trim().ToUpperInvariant(); if (word.Length < 1 || word.Length > 14 || !seen.Add(word)) continue; bool valid = true; foreach (char c in word) if (c < 'A' || c > 'Z') { valid = false; break; } if (!valid) continue; string key = PatternKey(word, 0, word.Length); List<WordEntry> entries; if (!result.TryGetValue(key, out entries)) { entries = new List<WordEntry>(); result[key] = entries; } double score = word.Length * 2.2 - 5.2 + Math.Log(1.0 + all.Count / (double)(rank + 1)) * .75; entries.Add(new WordEntry { Text = word, Score = score }); }
            foreach (List<WordEntry> entries in result.Values) entries.Sort(delegate(WordEntry a, WordEntry b) { return b.Score.CompareTo(a.Score); }); return result;
        }
    }
}
