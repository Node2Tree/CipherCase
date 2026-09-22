using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class SemaphoreCode
    {
        // Directions as seen by the observer; shared by the text codec and the drawing.
        private static readonly string[] Pairs = {
            "↓↙", "↓←", "↓↖", "↓↑", "↗↓", "→↓", "↘↓", "↙←", "↙↖", "↑→",
            "↙↑", "↙↗", "↙→", "↙↘", "←↖", "←↑", "←↗", "←→", "←↘", "↖↑",
            "↑↘", "↘↗", "→↖", "↖↗", "↖↘", "↗→"
        };

        internal static string Directions(char letter)
        {
            char value = char.ToUpperInvariant(letter);
            return value >= 'A' && value <= 'Z' ? Pairs[value - 'A'] : string.Empty;
        }

        internal static char Letter(string pair)
        {
            int index = Array.IndexOf(Pairs, pair);
            return index < 0 ? '?' : (char)('A' + index);
        }

        internal static string Transform(string input, bool decode)
        {
            if (!decode)
            {
                List<string> result = new List<string>();
                foreach (char raw in input ?? string.Empty)
                {
                    string pair = Directions(raw);
                    result.Add(pair.Length == 0 ? raw.ToString() : pair);
                }
                return string.Join(" / ", result.ToArray());
            }
            StringBuilder output = new StringBuilder();
            foreach (string token in (input ?? string.Empty).Split(new[] { '/', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                output.Append(Letter(token));
            return output.ToString();
        }
    }
}
