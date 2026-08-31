using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicalCipherToolbox.Analysis
{
    internal sealed class WordSegmentation
    {
        internal string Text;
        internal double Score;
        internal double Coverage;
    }

    internal static class SpacelessWordSegmenter
    {
        private sealed class Node
        {
            internal readonly Dictionary<char, Node> Children = new Dictionary<char, Node>();
            internal double Weight;
        }

        private static readonly Node Root = Build();

        internal static WordSegmentation Segment(string input)
        {
            string text = (input ?? string.Empty).ToUpperInvariant(); int length = text.Length; if (length == 0) return new WordSegmentation { Text = string.Empty, Score = 0, Coverage = 0 };
            double[] scores = new double[length + 1]; int[] previous = new int[length + 1], matched = new int[length + 1]; bool[] word = new bool[length + 1]; for (int i = 1; i <= length; i++) scores[i] = double.NegativeInfinity;
            for (int start = 0; start < length; start++)
            {
                if (double.IsNegativeInfinity(scores[start])) continue; Update(start + 1, start, scores[start] - 2.4, matched[start], false, scores, previous, matched, word); Node node = Root;
                for (int end = start; end < length && end < start + 14; end++) { if (!node.Children.TryGetValue(text[end], out node)) break; if (node.Weight != 0) { int wordLength = end - start + 1; Update(end + 1, start, scores[start] + node.Weight, matched[start] + (wordLength >= 3 ? wordLength : 0), true, scores, previous, matched, word); } }
            }
            List<string> pieces = new List<string>(); int position = length; StringBuilder unknown = new StringBuilder(); while (position > 0) { int start = previous[position]; string piece = text.Substring(start, position - start); if (word[position]) { if (unknown.Length > 0) { pieces.Add(Reverse(unknown.ToString())); unknown.Clear(); } pieces.Add(piece); } else unknown.Append(piece); position = start; } if (unknown.Length > 0) pieces.Add(Reverse(unknown.ToString())); pieces.Reverse();
            return new WordSegmentation { Text = string.Join(" ", pieces.ToArray()), Score = scores[length], Coverage = matched[length] / (double)length };
        }

        private static void Update(int at, int from, double score, int covered, bool isWord, double[] scores, int[] previous, int[] matched, bool[] words) { if (score < scores[at] || (score == scores[at] && covered <= matched[at])) return; scores[at] = score; previous[at] = from; matched[at] = covered; words[at] = isWord; }
        private static string Reverse(string value) { char[] chars = value.ToCharArray(); Array.Reverse(chars); return new string(chars); }

        private static Node Build()
        {
            Node root = new Node(); string[] oneLetter = { "A", "I" }, twoLetter = { "AM", "AN", "AS", "AT", "BE", "BY", "DO", "GO", "HE", "IF", "IN", "IS", "IT", "ME", "MY", "NO", "OF", "ON", "OR", "SO", "TO", "UP", "US", "WE" }, threeLetter = { "THE", "AND", "FOR", "ARE", "BUT", "NOT", "YOU", "ALL", "CAN", "HER", "HIS", "ONE", "OUR", "OUT", "WHO", "HOW", "WHY" }; foreach (string value in oneLetter) Add(root, value, -1.8); foreach (string value in twoLetter) Add(root, value, -2.2); foreach (string value in threeLetter) Add(root, value, 2.2);
            string[] words = EnglishNgramData.LoadWords(); for (int rank = 0; rank < words.Length; rank++) { string value = words[rank].Trim().ToUpperInvariant(); if (value.Length < 3 || value.Length > 14) continue; Add(root, value, value.Length * 1.9 - 6.0 + Math.Log(1.0 + words.Length / (double)(rank + 1)) * .8); } return root;
        }

        private static void Add(Node root, string word, double weight) { Node node = root; foreach (char c in word) { if (c < 'A' || c > 'Z') return; Node next; if (!node.Children.TryGetValue(c, out next)) { next = new Node(); node.Children[c] = next; } node = next; } if (node.Weight == 0 || weight > node.Weight) node.Weight = weight; }
    }
}
