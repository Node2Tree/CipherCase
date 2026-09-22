using System;
using System.Collections.Generic;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class GeneticCode
    {
        // NCBI transl_table=1, base order T,C,A,G. Internal translation, not CDS initiation rules.
        // https://www.ncbi.nlm.nih.gov/Taxonomy/Utils/wprintgc.cgi#SG1
        private const string AminoAcids = "FFLLSSSSYY**CC*WLLLLPPPPHHQQRRRRIIIMTTTTNNKKSSRRVVVVAAAADDEEGGGG";
        private const string Bases = "TCAG";
        private const string Symbols = "ACGTRYSWKMBDHVN";
        private const string Complements = "TGCAYRSWMKVHDBN";
        private static readonly string[] Expansions = { "A", "C", "G", "T", "AG", "CT", "CG", "AT", "GT", "AC", "CGT", "AGT", "ACT", "ACG", "ACGT" };
        private static readonly Dictionary<char, string> Names = new Dictionary<char, string>
        {
            {'A', "Ala"}, {'R', "Arg"}, {'N', "Asn"}, {'D', "Asp"}, {'C', "Cys"},
            {'Q', "Gln"}, {'E', "Glu"}, {'G', "Gly"}, {'H', "His"}, {'I', "Ile"},
            {'L', "Leu"}, {'K', "Lys"}, {'M', "Met"}, {'F', "Phe"}, {'P', "Pro"},
            {'S', "Ser"}, {'T', "Thr"}, {'W', "Trp"}, {'Y', "Tyr"}, {'V', "Val"},
            {'*', "Stop"}, {'X', "Xaa"}
        };

        internal static string Translate(ToolRequest request)
        {
            string sequence = Normalize(request.Input);
            if (sequence.Length == 0) return string.Empty;
            string frameText = request.Get("frame");
            int frame;
            if (frameText.Length == 0) frame = 1;
            else if (!int.TryParse(frameText, out frame) || frame == 0 || Math.Abs((long)frame) > 3)
                throw new CipherException("阅读框必须是 +1、+2、+3、-1、-2 或 -3");
            if (frame < 0) sequence = ReverseComplement(sequence);
            int offset = Math.Abs(frame) - 1;
            if (sequence.Length - offset < 3) throw new CipherException("所选阅读框中不足一个完整密码子");
            int remainder = (sequence.Length - offset) % 3;
            bool ignoreTail = request.Get("tail") == "忽略并提示";
            if (remainder != 0 && !ignoreTail) throw new CipherException("末尾剩余 " + remainder + " 个碱基；可选择“忽略并提示”或调整阅读框");
            bool threeLetters = request.Get("format") == "三字母";
            StringBuilder result = new StringBuilder();
            for (int i = offset; i + 2 < sequence.Length; i += 3)
            {
                if ((i - offset) % 768 == 0) request.ThrowIfCancellationRequested();
                char amino = TranslateCodon(sequence[i], sequence[i + 1], sequence[i + 2]);
                if (threeLetters && result.Length > 0) result.Append(' ');
                result.Append(threeLetters ? Names[amino] : amino.ToString());
            }
            if (remainder != 0) result.Append("\r\n（已忽略末尾 ").Append(remainder).Append(" 个碱基）");
            return result.ToString();
        }

        private static string Normalize(string input)
        {
            if (input.Length > 1000000) throw new CipherException("碱基输入最多 100 万字符");
            StringBuilder result = new StringBuilder();
            bool header = false;
            foreach (string raw in input.Replace("\r", string.Empty).Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith(">", StringComparison.Ordinal))
                {
                    if (header || result.Length > 0) throw new CipherException("请一次输入一条 FASTA 序列，不能拼接多个记录");
                    header = true;
                    continue;
                }
                foreach (char rawBase in line)
                {
                    if (char.IsWhiteSpace(rawBase)) continue;
                    char value = char.ToUpperInvariant(rawBase);
                    if (value == 'U') value = 'T';
                    if (Symbols.IndexOf(value) < 0) throw new CipherException("非法碱基：" + rawBase + "。支持 DNA/RNA 与 IUPAC 模糊碱基符号。");
                    result.Append(value);
                }
            }
            if (header && result.Length == 0) throw new CipherException("FASTA 标题后没有序列");
            return result.ToString();
        }

        internal static string ReverseComplement(string sequence)
        {
            char[] result = new char[sequence.Length];
            for (int i = 0; i < sequence.Length; i++) result[sequence.Length - i - 1] = Complements[Symbols.IndexOf(sequence[i])];
            return new string(result);
        }

        private static char TranslateCodon(char first, char second, char third)
        {
            char found = '\0';
            foreach (char a in Expansions[Symbols.IndexOf(first)])
                foreach (char b in Expansions[Symbols.IndexOf(second)])
                    foreach (char c in Expansions[Symbols.IndexOf(third)])
                    {
                        char value = AminoAcids[Bases.IndexOf(a) * 16 + Bases.IndexOf(b) * 4 + Bases.IndexOf(c)];
                        if (found != '\0' && found != value) return 'X';
                        found = value;
                    }
            return found;
        }
    }
}
