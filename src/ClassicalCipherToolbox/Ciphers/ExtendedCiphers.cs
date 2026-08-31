using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal sealed class PortaCipher : ICipher
    {
        public string Name { get { return "Porta"; } } public bool RequiresKey { get { return true; } } public string KeyHint { get { return "KEY"; } }
        public string Encrypt(string input, string key) { return Transform(input, key); } public string Decrypt(string input, string key) { return Transform(input, key); }
        private static string Transform(string input, string key)
        {
            string clean = CipherUtilities.Letters(key); if (clean.Length == 0) throw new CipherException("密钥请输入英文字母");
            StringBuilder result = new StringBuilder(); int position = 0;
            foreach (char raw in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(raw)) { result.Append(raw); continue; }
                int p = Alphabet.IndexOf(raw), group = (clean[position++ % clean.Length] - 'A') / 2;
                int c = p < 13 ? 13 + Alphabet.Mod(p + group, 13) : Alphabet.Mod(p - group, 13);
                result.Append(Alphabet.FromIndex(c, char.IsLower(raw)));
            }
            return result.ToString();
        }
    }

    internal sealed class GronsfeldCipher : ICipher
    {
        public string Name { get { return "Gronsfeld"; } } public bool RequiresKey { get { return true; } } public string KeyHint { get { return "数字密钥，例如 31415"; } }
        public string Encrypt(string input, string key) { return Transform(input, key, false); } public string Decrypt(string input, string key) { return Transform(input, key, true); }
        private static string Transform(string input, string key, bool decrypt)
        {
            key = (key ?? string.Empty).Trim(); if (key.Length == 0) throw new CipherException("请输入数字密钥");
            foreach (char value in key) if (!char.IsDigit(value)) throw new CipherException("密钥只能包含数字");
            StringBuilder result = new StringBuilder(); int position = 0;
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int shift = key[position++ % key.Length] - '0'; result.Append(Alphabet.Shift(value, decrypt ? -shift : shift));
            }
            return result.ToString();
        }
    }

    internal sealed class RunningKeyCipher : ICipher
    {
        public string Name { get { return "Running Key"; } } public bool RequiresKey { get { return true; } } public string KeyHint { get { return "与文本等长的密钥文本"; } }
        public string Encrypt(string input, string key) { return Transform(input, key, false); } public string Decrypt(string input, string key) { return Transform(input, key, true); }
        private static string Transform(string input, string key, bool decrypt)
        {
            string clean = CipherUtilities.Letters(key); int needed = CipherUtilities.Letters(input).Length;
            if (clean.Length < needed) throw new CipherException("密钥文本的字母数不足");
            StringBuilder result = new StringBuilder(); int position = 0;
            foreach (char value in input ?? string.Empty)
            {
                if (!Alphabet.IsAsciiLetter(value)) { result.Append(value); continue; }
                int shift = clean[position++] - 'A'; result.Append(Alphabet.Shift(value, decrypt ? -shift : shift));
            }
            return result.ToString();
        }
    }

    internal static class MyszkowskiCipher
    {
        internal static string Encrypt(string input, string key)
        {
            int[] ranks = Ranks(key); input = input ?? string.Empty; StringBuilder result = new StringBuilder(input.Length);
            int max = 0; foreach (int rank in ranks) if (rank > max) max = rank;
            for (int rank = 0; rank <= max; rank++)
            {
                List<int> columns = new List<int>(); for (int c = 0; c < ranks.Length; c++) if (ranks[c] == rank) columns.Add(c);
                if (columns.Count == 1) for (int i = columns[0]; i < input.Length; i += ranks.Length) result.Append(input[i]);
                else for (int row = 0; row * ranks.Length < input.Length; row++) foreach (int column in columns) { int i = row * ranks.Length + column; if (i < input.Length) result.Append(input[i]); }
            }
            return result.ToString();
        }
        internal static string Decrypt(string input, string key)
        {
            int[] ranks = Ranks(key); input = input ?? string.Empty; int max = 0; foreach (int rank in ranks) if (rank > max) max = rank;
            char[] plain = new char[input.Length]; int position = 0;
            for (int rank = 0; rank <= max; rank++)
            {
                List<int> columns = new List<int>(); for (int c = 0; c < ranks.Length; c++) if (ranks[c] == rank) columns.Add(c);
                if (columns.Count == 1) for (int i = columns[0]; i < input.Length; i += ranks.Length) plain[i] = input[position++];
                else for (int row = 0; row * ranks.Length < input.Length; row++) foreach (int column in columns) { int i = row * ranks.Length + column; if (i < input.Length) plain[i] = input[position++]; }
            }
            return new string(plain);
        }
        private static int[] Ranks(string key)
        {
            key = CipherUtilities.Letters(key); if (key.Length < 2) throw new CipherException("关键词至少 2 个字母");
            char[] distinct = key.ToCharArray(); Array.Sort(distinct); string order = string.Empty; foreach (char c in distinct) if (order.IndexOf(c) < 0) order += c;
            int[] ranks = new int[key.Length]; for (int i = 0; i < key.Length; i++) ranks[i] = order.IndexOf(key[i]); return ranks;
        }
    }

    internal static class RouteCipher
    {
        internal static string Encrypt(string input, string widthText)
        {
            int width = ReadWidth(widthText); string text = input ?? string.Empty; int rows = (text.Length + width - 1) / width;
            text = text.PadRight(rows * width, 'X'); char[,] grid = new char[rows, width]; int p = 0;
            for (int r = 0; r < rows; r++) for (int c = 0; c < width; c++) grid[r, c] = text[p++];
            return ReadSpiral(grid, rows, width);
        }
        internal static string Decrypt(string input, string widthText)
        {
            int width = ReadWidth(widthText); string text = input ?? string.Empty; if (text.Length % width != 0) throw new CipherException("路线密文长度须为宽度的倍数");
            int rows = text.Length / width; char[,] grid = new char[rows, width]; FillSpiral(grid, rows, width, text);
            StringBuilder result = new StringBuilder(text.Length); for (int r = 0; r < rows; r++) for (int c = 0; c < width; c++) result.Append(grid[r, c]); return result.ToString();
        }
        private static int ReadWidth(string text) { int value; if (!int.TryParse(text, out value) || value < 2 || value > 100) throw new CipherException("宽度须为 2–100"); return value; }
        private static string ReadSpiral(char[,] grid, int rows, int cols) { StringBuilder result = new StringBuilder(rows * cols); Walk(rows, cols, delegate(int r, int c, int i) { result.Append(grid[r, c]); }); return result.ToString(); }
        private static void FillSpiral(char[,] grid, int rows, int cols, string text) { Walk(rows, cols, delegate(int r, int c, int i) { grid[r, c] = text[i]; }); }
        private static void Walk(int rows, int cols, Action<int,int,int> visit)
        {
            int top=0,bottom=rows-1,left=0,right=cols-1,index=0;
            while(top<=bottom&&left<=right){for(int c=left;c<=right;c++)visit(top,c,index++);top++;for(int r=top;r<=bottom;r++)visit(r,right,index++);right--;if(top<=bottom){for(int c=right;c>=left;c--)visit(bottom,c,index++);bottom--;}if(left<=right){for(int r=bottom;r>=top;r--)visit(r,left,index++);left++;}}
        }
    }

    internal static class StraddlingCheckerboardCipher
    {
        internal static string Encrypt(string input, string key, string blanks) { return Transform(input, key, blanks, false); }
        internal static string Decrypt(string input, string key, string blanks) { return Transform(input, key, blanks, true); }
        private static string Transform(string input, string key, string blanks, bool decrypt)
        {
            string alphabet = CipherUtilities.KeyedAlphabet(key, true); int[] blank = ParseBlanks(blanks); Dictionary<char,string> enc = new Dictionary<char,string>(); Dictionary<string,char> dec = new Dictionary<string,char>(); int ai=0;
            for(int d=0;d<10&&ai<26;d++)if(d!=blank[0]&&d!=blank[1]){string code=d.ToString();enc[alphabet[ai]]=code;dec[code]=alphabet[ai++];}
            for(int row=0;row<2;row++)for(int d=0;d<10&&ai<26;d++){string code=blank[row].ToString()+d;enc[alphabet[ai]]=code;dec[code]=alphabet[ai++];}
            if(!decrypt){StringBuilder r=new StringBuilder();foreach(char raw in input??string.Empty){char v=char.ToUpperInvariant(raw);if(enc.ContainsKey(v))r.Append(enc[v]);else r.Append(raw);}return r.ToString();}
            StringBuilder result=new StringBuilder(); string source=input??string.Empty; for(int i=0;i<source.Length;){if(!char.IsDigit(source[i])){result.Append(source[i++]);continue;}string code=source[i].ToString();if((source[i]-'0'==blank[0]||source[i]-'0'==blank[1])&&i+1<source.Length)code+=source[++i];if(dec.ContainsKey(code))result.Append(dec[code]);else result.Append('?');i++;}return result.ToString();
        }
        private static int[] ParseBlanks(string value){value=(value??string.Empty).Trim();if(value.Length!=2||!char.IsDigit(value[0])||!char.IsDigit(value[1])||value[0]==value[1])throw new CipherException("空位请输入两个不同数字，例如 37");return new[]{value[0]-'0',value[1]-'0'};}
    }

    internal static class CipherUtilities
    {
        internal static string Letters(string input) { StringBuilder r=new StringBuilder();foreach(char raw in input??string.Empty){char c=char.ToUpperInvariant(raw);if(c>='A'&&c<='Z')r.Append(c);}return r.ToString(); }
        internal static string KeyedAlphabet(string key, bool includeJ) { string basis=includeJ?"ABCDEFGHIJKLMNOPQRSTUVWXYZ":"ABCDEFGHIKLMNOPQRSTUVWXYZ";StringBuilder r=new StringBuilder();foreach(char raw in Letters(key)+basis){char c=!includeJ&&raw=='J'?'I':raw;if(basis.IndexOf(c)>=0&&r.ToString().IndexOf(c)<0)r.Append(c);}return r.ToString(); }
    }
}
