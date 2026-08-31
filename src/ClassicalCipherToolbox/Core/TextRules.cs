using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ClassicalCipherToolbox.Core
{
    internal sealed class TextRuleOptions
    {
        internal TextRuleOptions()
        {
            Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            PreserveCase = true;
            PreserveSpaces = true;
            PreservePunctuation = true;
        }

        internal string Alphabet { get; set; }
        internal bool PreserveCase { get; set; }
        internal bool PreserveSpaces { get; set; }
        internal bool PreservePunctuation { get; set; }
        internal bool MergeIJ { get; set; }
        internal bool RemoveDiacritics { get; set; }

        internal TextRuleOptions Copy()
        {
            return new TextRuleOptions { Alphabet = Alphabet, PreserveCase = PreserveCase, PreserveSpaces = PreserveSpaces,
                PreservePunctuation = PreservePunctuation, MergeIJ = MergeIJ, RemoveDiacritics = RemoveDiacritics };
        }
    }

    internal static class TextRules
    {
        internal const string StandardAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        internal static string ValidateAlphabet(string value)
        {
            string alphabet = string.IsNullOrWhiteSpace(value) ? StandardAlphabet : value.Trim();
            if (alphabet.Length != 26) throw new CipherException("字母表须恰好包含 26 个不同字符");
            HashSet<char> used = new HashSet<char>();
            foreach (char raw in alphabet)
            {
                char valueChar = char.ToUpperInvariant(raw);
                if (!used.Add(valueChar)) throw new CipherException("字母表不能包含重复字符");
            }
            return alphabet;
        }

        internal static string ToWorking(string input, TextRuleOptions options)
        {
            if (options == null) return input ?? string.Empty;
            string alphabet = ValidateAlphabet(options.Alphabet);
            string source = options.RemoveDiacritics ? RemoveDiacritics(input) : input ?? string.Empty;
            StringBuilder result = new StringBuilder(source.Length);
            foreach (char raw in source)
            {
                char upper = char.ToUpperInvariant(raw);
                int index = IndexOfIgnoreCase(alphabet, upper);
                if (index >= 0)
                {
                    char mapped = (char)('A' + index);
                    if (options.MergeIJ && mapped == 'J') mapped = 'I';
                    result.Append(options.PreserveCase && char.IsLower(raw) ? char.ToLowerInvariant(mapped) : mapped);
                }
                else if (char.IsWhiteSpace(raw))
                {
                    if (options.PreserveSpaces) result.Append(raw);
                }
                else if (options.PreservePunctuation) result.Append(raw);
            }
            return result.ToString();
        }

        internal static string FromWorking(string input, TextRuleOptions options)
        {
            if (options == null) return input ?? string.Empty;
            string alphabet = ValidateAlphabet(options.Alphabet);
            if (string.Equals(alphabet, StandardAlphabet, StringComparison.Ordinal))
                return options.PreserveCase ? input ?? string.Empty : (input ?? string.Empty).ToUpperInvariant();
            StringBuilder result = new StringBuilder((input ?? string.Empty).Length);
            foreach (char raw in input ?? string.Empty)
            {
                char upper = char.ToUpperInvariant(raw);
                if (upper >= 'A' && upper <= 'Z')
                {
                    char mapped = alphabet[upper - 'A'];
                    result.Append(options.PreserveCase && char.IsLower(raw) ? char.ToLowerInvariant(mapped) : mapped);
                }
                else result.Append(raw);
            }
            return result.ToString();
        }

        private static int IndexOfIgnoreCase(string alphabet, char value)
        {
            for (int i = 0; i < alphabet.Length; i++) if (char.ToUpperInvariant(alphabet[i]) == value) return i;
            return -1;
        }

        private static string RemoveDiacritics(string input)
        {
            string normalized = (input ?? string.Empty).Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();
            foreach (char value in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(value) != UnicodeCategory.NonSpacingMark) result.Append(value);
            return result.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
