using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class EnigmaCipher
    {
        private sealed class Rotor
        {
            internal string Wiring; internal string Notches; internal int Ring; internal int Position; internal bool Moving;
            internal int Forward(int value) { int shifted = Alphabet.Mod(value + Position - Ring, 26), wired = Wiring[shifted] - 'A'; return Alphabet.Mod(wired - Position + Ring, 26); }
            internal int Backward(int value) { int shifted = Alphabet.Mod(value + Position - Ring, 26), wired = Wiring.IndexOf((char)('A' + shifted)); return Alphabet.Mod(wired - Position + Ring, 26); }
            internal bool AtNotch { get { return Moving && Notches.IndexOf((char)('A' + Position)) >= 0; } }
        }

        private static readonly Dictionary<string, string> Wirings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"I","EKMFLGDQVZNTOWYHXUSPAIBRCJ"},{"II","AJDKSIRUXBLHWTMCQGZNPYFVOE"},{"III","BDFHJLCPRTXVZNYEIWGAKMUSQO"},
            {"IV","ESOVPZJAYQUIRHXLNFTGKDCMWB"},{"V","VZBRGITYUPSDNHLXAWMJQOFECK"},{"VI","JPGVOUMFYQBENHZRDKASXLICTW"},
            {"VII","NZJHGRCXMYSWBOUFAIVLPEKQDT"},{"VIII","FKQHTLXOCBJSPDZRAMEWNIUYGV"},{"BETA","LEYJVCNIXWPBQMDRTAKZGFUHOS"},{"GAMMA","FSOKANUERHMBTIYCWLQPZXVGJD"}
        };
        private static readonly Dictionary<string, string> Notches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { {"I","Q"},{"II","E"},{"III","V"},{"IV","J"},{"V","Z"},{"VI","ZM"},{"VII","ZM"},{"VIII","ZM"},{"BETA",""},{"GAMMA",""} };
        private static readonly Dictionary<string, string> Reflectors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { {"A","EJMZALYXVBWFCRQUONTSPIKHGD"},{"B","YRUHQSLDPXNGOKMIEBFZCWVJAT"},{"C","FVPJIAOYEDRZXWGCTKUQSBNMHL"},{"BTHIN","ENKQAUYWJICOPBLMDXZVFTHRGS"},{"CTHIN","RDOBJNTKVEHMLFCWZAXGYIPSUQ"} };

        internal static string Transform(string input, string model, string rotorText, string ringText, string positionText, string reflectorText, string plugboardText)
        {
            Rotor[] rotors = BuildRotors(model, rotorText, ringText, positionText); string reflector = SelectReflector(model, reflectorText); int[] plugboard = ParsePlugboard(plugboardText); StringBuilder result = new StringBuilder();
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                Step(rotors); int value = plugboard[char.ToUpperInvariant(raw) - 'A']; for (int i = rotors.Length - 1; i >= 0; i--) value = rotors[i].Forward(value); value = reflector[value] - 'A'; for (int i = 0; i < rotors.Length; i++) value = rotors[i].Backward(value); value = plugboard[value]; char output = (char)('A' + value); result.Append(char.IsLower(raw) ? char.ToLowerInvariant(output) : output);
            }
            return result.ToString();
        }

        internal static string NormalizeModel(string model) { string v = (model ?? string.Empty).Trim().ToUpperInvariant(); return v == "I" || v == "M4" ? v : "M3"; }

        private static Rotor[] BuildRotors(string model, string rotorText, string ringText, string positionText)
        {
            model = NormalizeModel(model); int count = model == "M4" ? 4 : 3; string[] defaults = model == "M4" ? new[] { "BETA", "I", "II", "III" } : new[] { "I", "II", "III" };
            string[] names = Split(rotorText); if (names.Length == 0) names = defaults; if (names.Length != count) throw new CipherException(model + " 需要 " + count + " 个转子名称");
            int[] rings = ParseSettings(ringText, count, 1), positions = ParsePositions(positionText, count); Rotor[] result = new Rotor[count]; HashSet<string> moving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++) { string name = names[i].ToUpperInvariant(); if (!Wirings.ContainsKey(name)) throw new CipherException("未知转子：" + name); bool isMoving = !(name == "BETA" || name == "GAMMA"); if (isMoving && !moving.Add(name)) throw new CipherException("移动转子不能重复"); result[i] = new Rotor { Wiring = Wirings[name], Notches = Notches[name], Ring = rings[i] - 1, Position = positions[i], Moving = isMoving }; }
            if (model == "M4" && result[0].Moving) throw new CipherException("M4 最左侧须使用 Beta 或 Gamma"); return result;
        }
        private static void Step(Rotor[] rotors)
        {
            int right = rotors.Length - 1, middle = right - 1, left = right - 2; bool rightNotch = rotors[right].AtNotch, middleNotch = rotors[middle].AtNotch;
            if (middleNotch) rotors[left].Position = (rotors[left].Position + 1) % 26; if (rightNotch || middleNotch) rotors[middle].Position = (rotors[middle].Position + 1) % 26; rotors[right].Position = (rotors[right].Position + 1) % 26;
        }
        private static string SelectReflector(string model, string value) { string name = (value ?? string.Empty).Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty); if (name.Length == 0) name = NormalizeModel(model) == "M4" ? "BTHIN" : NormalizeModel(model) == "I" ? "A" : "B"; if (!Reflectors.ContainsKey(name)) throw new CipherException("未知反射器：" + name); return Reflectors[name]; }
        private static int[] ParsePlugboard(string text) { int[] map = new int[26]; for (int i = 0; i < 26; i++) map[i] = i; string[] pairs = Split(text); foreach (string raw in pairs) { string pair = CipherUtilities.Letters(raw); if (pair.Length != 2) throw new CipherException("插线板格式示例：AV BS CG"); int a = pair[0] - 'A', b = pair[1] - 'A'; if (a == b || map[a] != a || map[b] != b) throw new CipherException("插线板字母不能重复"); map[a] = b; map[b] = a; } return map; }
        private static string[] Split(string text) { return (text ?? string.Empty).Split(new[] { ' ', ',', ';', '-', '/', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); }
        private static int[] ParseSettings(string text, int count, int fallback) { string[] parts = Split(text); int[] result = new int[count]; for (int i = 0; i < count; i++) { int n; if (parts.Length == 0) n = fallback; else if (i >= parts.Length || !int.TryParse(parts[i], out n) || n < 1 || n > 26) throw new CipherException("环位须为 1–26，共 " + count + " 项"); result[i] = n; } return result; }
        private static int[] ParsePositions(string text, int count) { string letters = CipherUtilities.Letters(text); int[] result = new int[count]; if (letters.Length == 0) return result; if (letters.Length != count) throw new CipherException("初始位置须为 " + count + " 个字母"); for (int i = 0; i < count; i++) result[i] = letters[i] - 'A'; return result; }
    }
}
