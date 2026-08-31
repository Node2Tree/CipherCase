using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class KeywordCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true); StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); int p = decrypt ? alphabet.IndexOf(c) : c - 'A'; if (p < 0 || p >= 26) { result.Append(raw); continue; } char mapped = decrypt ? (char)('A' + p) : alphabet[p]; result.Append(char.IsLower(raw) ? char.ToLowerInvariant(mapped) : mapped); } return result.ToString();
        }
    }

    internal static class MultiplicativeCipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            int a; if (!int.TryParse((key ?? string.Empty).Trim(), out a)) throw new CipherException("乘数必须是整数"); a = Mod(a, 26); int inverse = Inverse(a, 26); if (inverse < 0) throw new CipherException("乘数必须与 26 互质"); int factor = decrypt ? inverse : a; StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c < 'A' || c > 'Z') result.Append(raw); else { char mapped = (char)('A' + (c - 'A') * factor % 26); result.Append(char.IsLower(raw) ? char.ToLowerInvariant(mapped) : mapped); } } return result.ToString();
        }
        internal static string Crack(string input, string language)
        {
            int[] keys = { 1, 3, 5, 7, 9, 11, 15, 17, 19, 21, 23, 25 }; List<KeyValuePair<int, string>> candidates = new List<KeyValuePair<int, string>>(); foreach (int key in keys) candidates.Add(new KeyValuePair<int, string>(key, Transform(input, key.ToString(CultureInfo.InvariantCulture), true))); candidates.Sort(delegate(KeyValuePair<int, string> a, KeyValuePair<int, string> b) { return Score(b.Value, language).CompareTo(Score(a.Value, language)); }); StringBuilder result = new StringBuilder(); for (int i = 0; i < candidates.Count; i++) { if (i > 0) result.Append("\r\n\r\n"); result.Append('#').Append(i + 1).Append("  密钥 ").Append(candidates[i].Key).Append("  评分 ").Append(Score(candidates[i].Value, language).ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n").Append(candidates[i].Value); } return result.ToString();
        }
        private static double Score(string text, string language) { StringBuilder letters = new StringBuilder(); foreach (char raw in text ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') letters.Append(c); } return LanguageModels.TextScore(letters.ToString(), language); }
        private static int Mod(int value, int modulus) { int result = value % modulus; return result < 0 ? result + modulus : result; }
        private static int Inverse(int value, int modulus) { for (int i = 1; i < modulus; i++) if (value * i % modulus == 1) return i; return -1; }
    }

    internal static class ReverseCipher
    {
        internal static string Transform(string input) { List<string> units = new List<string>(); TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(input ?? string.Empty); while (elements.MoveNext()) units.Add(elements.GetTextElement()); units.Reverse(); return string.Concat(units.ToArray()); }
        internal static string Crack(string input) { return "#1  密钥 反向  评分 直接\r\n" + Transform(input); }
    }

    internal static class VatsyayanaCipher
    {
        internal static string Transform(string input, string key)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true); Dictionary<char, char> pairs = new Dictionary<char, char>(); for (int i = 0; i < 26; i += 2) { pairs[alphabet[i]] = alphabet[i + 1]; pairs[alphabet[i + 1]] = alphabet[i]; } StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw), mapped; if (!pairs.TryGetValue(c, out mapped)) result.Append(raw); else result.Append(char.IsLower(raw) ? char.ToLowerInvariant(mapped) : mapped); } return result.ToString();
        }
    }

    internal static class Hill3Cipher
    {
        internal static string Transform(string input, string key, bool decrypt)
        {
            int[,] matrix = ParseKey(key); if (decrypt) matrix = Invert(matrix); string letters = Letters(input); while (letters.Length % 3 != 0) letters += "X"; StringBuilder result = new StringBuilder(); for (int p = 0; p < letters.Length; p += 3) for (int row = 0; row < 3; row++) { int value = 0; for (int column = 0; column < 3; column++) value += matrix[row, column] * (letters[p + column] - 'A'); result.Append((char)('A' + Mod(value, 26))); } return result.ToString();
        }
        internal static string CrackKnownPlaintext(string input, string crib)
        {
            string cipher = Letters(input), plain = Letters(crib); if (plain.Length < 9 || cipher.Length < plain.Length) throw new CipherException("Hill 3×3 已知明文至少需要 9 个对齐字母"); int usable = Math.Min(cipher.Length, plain.Length); for (int offset = 0; offset + 9 <= usable; offset += 3) { int[,] p = Columns(plain, offset), inverse; try { inverse = Invert(p); } catch { continue; } int[,] c = Columns(cipher, offset), key = Multiply(c, inverse); bool valid = true; for (int i = 0; i + 2 < usable; i += 3) for (int row = 0; row < 3; row++) { int value = 0; for (int col = 0; col < 3; col++) value += key[row, col] * (plain[i + col] - 'A'); if (Mod(value, 26) != cipher[i + row] - 'A') valid = false; } if (!valid) continue; string keyText = KeyText(key), decoded = Transform(cipher, keyText, true); return "#1  密钥 " + keyText + "  评分 已知明文一致\r\n" + decoded; } throw new CipherException("已知片段中没有可逆的 3×3 明文块，或片段未从密文开头对齐");
        }
        private static int[,] ParseKey(string value) { string[] parts = (value ?? string.Empty).Split(new[] { ',', ' ', ';', '，' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length != 9) throw new CipherException("Hill 3×3 密钥需要 9 个整数"); int[,] matrix = new int[3, 3]; for (int i = 0; i < 9; i++) { int number; if (!int.TryParse(parts[i], out number)) throw new CipherException("Hill 3×3 密钥格式无效"); matrix[i / 3, i % 3] = Mod(number, 26); } Invert(matrix); return matrix; }
        private static int[,] Invert(int[,] m) { int det = Mod(m[0,0]*(m[1,1]*m[2,2]-m[1,2]*m[2,1])-m[0,1]*(m[1,0]*m[2,2]-m[1,2]*m[2,0])+m[0,2]*(m[1,0]*m[2,1]-m[1,1]*m[2,0]),26), inv = Inverse(det,26); if(inv<0)throw new CipherException("Hill 3×3 矩阵在模 26 下不可逆"); int[,] result=new int[3,3];for(int r=0;r<3;r++)for(int c=0;c<3;c++){int r1=(c+1)%3,r2=(c+2)%3,c1=(r+1)%3,c2=(r+2)%3;int minor=m[r1,c1]*m[r2,c2]-m[r1,c2]*m[r2,c1];result[r,c]=Mod(inv*minor,26);}return result; }
        private static int[,] Columns(string text,int offset){int[,]m=new int[3,3];for(int col=0;col<3;col++)for(int row=0;row<3;row++)m[row,col]=text[offset+col*3+row]-'A';return m;}private static int[,] Multiply(int[,]a,int[,]b){int[,]r=new int[3,3];for(int i=0;i<3;i++)for(int j=0;j<3;j++)for(int k=0;k<3;k++)r[i,j]=Mod(r[i,j]+a[i,k]*b[k,j],26);return r;}private static string KeyText(int[,]m){StringBuilder s=new StringBuilder();for(int i=0;i<9;i++){if(i>0)s.Append(',');s.Append(m[i/3,i%3]);}return s.ToString();}
        private static string Letters(string value){StringBuilder result=new StringBuilder();foreach(char raw in value??string.Empty){char c=char.ToUpperInvariant(raw);if(c>='A'&&c<='Z')result.Append(c);}return result.ToString();}private static int Mod(int value,int modulus){int r=value%modulus;return r<0?r+modulus:r;}private static int Inverse(int value,int modulus){for(int i=1;i<modulus;i++)if(value*i%modulus==1)return i;return-1;}
    }
}
