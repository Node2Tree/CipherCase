using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Benchmarks
{
    internal static class CrackerBenchmark
    {
        private sealed class Case
        {
            internal string Group, Name, Label;
            internal Dictionary<string, string> Encrypt = new Dictionary<string, string>();
            internal Dictionary<string, string> Crack = new Dictionary<string, string>();
            internal Case(string group, string name, string label, params string[] pairs)
            {
                Group = group; Name = name; Label = label;
                for (int i = 0; i + 2 < pairs.Length; i += 3) (pairs[i] == "E" ? Encrypt : Crack)[pairs[i + 1]] = pairs[i + 2];
                Crack["language"] = "EN";
            }
        }

        private const string Plaintext = @"To be, or not to be, that is the question:
Whether 'tis nobler in the mind to suffer
The slings and arrows of outrageous fortune,
Or to take arms against a sea of troubles
And by opposing end them. To die—to sleep,
No more; and by a sleep to say we end
The heart-ache and the thousand natural shocks
That flesh is heir to: 'tis a consummation
Devoutly to be wish'd. To die, to sleep;
To sleep, perchance to dream—ay, there's the rub:
For in that sleep of death what dreams may come,
When we have shuffled off this mortal coil,
Must give us pause—there's the respect
That makes calamity of so long life.
For who would bear the whips and scorns of time,
Th'oppressor's wrong, the proud man's contumely,
The pangs of dispriz'd love, the law's delay,
The insolence of office, and the spurns
That patient merit of th'unworthy takes,
When he himself might his quietus make
With a bare bodkin? Who would fardels bear,
To grunt and sweat under a weary life,
But that the dread of something after death,
The undiscovere'd country, from whose bourn
No traveller returns, puzzles the will,
And makes us rather bear those ills we have
Than fly to others that we know not of?
Thus conscience does make cowards of us all,
And thus the native hue of resolution
Is sicklied o'er with the pale cast of thought,
And enterprises of great pitch and moment
With this regard their currents turn awry
And lose the name of action.";

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8; string group = args.Length == 0 ? "fast" : args[0]; string sample = Letters(Plaintext).Substring(0, 360); IList<ICryptoTool> tools = ToolRegistry.CreateAll(); List<Case> cases = Cases(); int exactTop = 0, exactAny = 0, errors = 0; double similarityTotal = 0; long timeTotal = 0;
            Console.WriteLine("GROUP\tCASE\tTOP_EXACT\tBEST_EXACT\tTOP_SIM\tBEST_SIM\tRANK\tMS\tCIPHER_LEN\tKEY\tPROGRESS");
            foreach (Case item in cases)
            {
                if (item.Group != group) continue; Console.Error.WriteLine("START " + item.Label); Stopwatch watch = Stopwatch.StartNew();
                try
                {
                    ICryptoTool tool = Find(tools, item.Name); string cipher = tool.Execute(new ToolRequest(ToolMode.Encrypt, sample, item.Encrypt)); string oracle = item.Name == "Ubchi" ? sample : tool.Execute(new ToolRequest(ToolMode.Decrypt, cipher, item.Encrypt)); int progress = 0; string cracked = tool.Execute(new ToolRequest(ToolMode.Crack, cipher, item.Crack, delegate(int p, string s) { progress = Math.Max(progress, p); }, delegate { return false; })); List<string> candidates = Candidates(cracked); string expected = Letters(oracle); double top = candidates.Count == 0 ? 0 : Similarity(expected, Letters(candidates[0])), best = 0; int rank = 0;
                    for (int i = 0; i < candidates.Count; i++) { double value = Similarity(expected, Letters(candidates[i])); if (value > best) best = value; if (rank == 0 && Letters(candidates[i]) == expected) rank = i + 1; }
                    bool topExact = rank == 1, anyExact = rank > 0; if (topExact) exactTop++; if (anyExact) exactAny++; similarityTotal += top; timeTotal += watch.ElapsedMilliseconds;
                    Console.WriteLine(string.Join("\t", new[] { group, item.Label, topExact ? "1" : "0", anyExact ? "1" : "0", top.ToString("0.0", CultureInfo.InvariantCulture), best.ToString("0.0", CultureInfo.InvariantCulture), rank.ToString(CultureInfo.InvariantCulture), watch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture), cipher.Length.ToString(CultureInfo.InvariantCulture), KeyText(item.Encrypt), progress.ToString(CultureInfo.InvariantCulture) }));
                }
                catch (Exception ex) { errors++; Console.WriteLine(string.Join("\t", new[] { group, item.Label, "ERR", "ERR", "0", "0", "0", watch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture), "0", KeyText(item.Encrypt), ex.GetType().Name + ": " + ex.Message })); }
            }
            int count = 0; foreach (Case item in cases) if (item.Group == group) count++; Console.WriteLine("SUMMARY\t" + group + "\t" + count + "\t" + exactTop + "\t" + exactAny + "\t" + (count == 0 ? 0 : similarityTotal / count).ToString("0.0", CultureInfo.InvariantCulture) + "\t" + timeTotal + "\t" + errors);
            Environment.ExitCode = errors == 0 && count > 0 ? 0 : 1;
        }

        private static List<Case> Cases()
        {
            return new List<Case> {
                new Case("diag","单表替换","单表替换诊断","E","key","QWERTYUIOPASDFGHJKLZXCVBNM","C","iterations","7000"),
                new Case("tune","Playfair","Playfair 强搜索","E","key","CIPHER","C","iterations","100000","C","restarts","10"),
                new Case("tune","Fractionated Morse","Fractionated Morse 强搜索","E","key","CIPHER","C","iterations","100000","C","restarts","10"),
                new Case("tune","Two-square","Two-square 强搜索","E","key1","CIPHER","E","key2","KEYWORD","C","iterations","100000","C","restarts","10"),
                new Case("tune","Four-square","Four-square 强搜索","E","key1","CIPHER","E","key2","KEYWORD","C","iterations","100000","C","restarts","10"),
                new Case("period","Bifid","Bifid 已知周期强搜索","E","key","CIPHER","E","period","7","C","minperiod","7","C","maxperiod","7","C","iterations","100000","C","restarts","10"),
                new Case("period","Trifid","Trifid 已知周期强搜索","E","key","CIPHER","E","period","7","C","minperiod","7","C","maxperiod","7","C","iterations","100000","C","restarts","10"),
                new Case("homo","同音替换","同音替换强搜索","E","key","CIPHER","C","iterations","200000","C","restarts","12"),
                new Case("quick","Ubchi","Ubchi 复核","E","key","CARGO","E","nulls","XYZ","C","min","2","C","max","8","C","nullmax","3"),
                new Case("fast","Hill 2×2","Hill 2×2","E","key","3,3,2,5"),
                new Case("fast","Morbit","Morbit","E","key","KEYWORD"),
                new Case("fast","Pollux","Pollux","E","key","SEED"),
                new Case("fast","Turning Grille","Turning Grille 4×4","E","size","4","E","holes","1,2,3,6","C","size","4"),
                new Case("fast","Turning Grille","Turning Grille 6×6","E","size","6","E","holes","1,2,3,7,8,9,13,14,15","C","size","6"),
                new Case("fast","列换位","短宽度列换位","E","key","CARGO","C","min","2","C","max","6"),
                new Case("medium","列换位","列换位","E","key","ZEBRAS","C","min","2","C","max","8"),
                new Case("medium","Myszkowski","Myszkowski","E","key","TOMATO","C","min","3","C","max","7"),
                new Case("medium","AMSCO","AMSCO","E","key","CARGO","C","min","2","C","max","8"),
                new Case("medium","Autokey","Autokey","E","key","QUEEN","C","min","2","C","max","12"),
                new Case("medium","Playfair","Playfair","E","key","CIPHER"),
                new Case("medium","ADFGX","ADFGX","E","square","CIPHER","E","column","CARGO","C","min","2","C","max","8"),
                new Case("medium","ADFGVX","ADFGVX","E","square","CIPHER","E","column","CARGO","C","min","2","C","max","8"),
                new Case("deep","Fractionated Morse","Fractionated Morse","E","key","CIPHER"),
                new Case("deep","Nihilist","Nihilist（提供方阵）","E","square","CIPHER","E","key","QUEEN","C","square","CIPHER","C","min","2","C","max","10"),
                new Case("deep","跨行棋盘","跨行棋盘","E","key","CIPHER","E","blanks","37"),
                new Case("deep","Polybius","keyed Polybius","E","key","CIPHER"),
                new Case("deep","Bifid","Bifid","E","key","CIPHER","E","period","7","C","minperiod","2","C","maxperiod","12"),
                new Case("deep","同音替换","同音替换","E","key","CIPHER"),
                new Case("deep","Two-square","Two-square","E","key1","CIPHER","E","key2","KEYWORD"),
                new Case("deep","Four-square","Four-square","E","key1","CIPHER","E","key2","KEYWORD"),
                new Case("deep","Trifid","Trifid","E","key","CIPHER","E","period","7","C","minperiod","2","C","maxperiod","12"),
                new Case("deep","双重列换位","双重列换位","E","key1","CARGO","E","key2","ZEBRA","C","min","2","C","max","6"),
                new Case("deep","Ubchi","Ubchi","E","key","CARGO","E","nulls","XYZ","C","min","2","C","max","8","C","nullmax","3")
            };
        }

        private static ICryptoTool Find(IList<ICryptoTool> tools, string name) { foreach (ICryptoTool tool in tools) if (tool.Name == name) return tool; throw new Exception("Missing tool " + name); }
        private static List<string> Candidates(string output) { List<string> result = new List<string>(); string[] lines = (output ?? string.Empty).Replace("\r", string.Empty).Split('\n'); for (int i = 0; i < lines.Length; i++) if (lines[i].StartsWith("#", StringComparison.Ordinal)) { int p = i + 1; while (p < lines.Length && (lines[p].StartsWith("密文表：", StringComparison.Ordinal) || lines[p].StartsWith("明文表：", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(lines[p]))) p++; if (p < lines.Length) result.Add(lines[p]); } return result; }
        private static double Similarity(string expected, string actual) { int length = Math.Max(expected.Length, actual.Length); if (length == 0) return 100; int same = 0; for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++) if (expected[i] == actual[i]) same++; return same * 100.0 / length; }
        private static string Letters(string input) { StringBuilder result = new StringBuilder(); foreach (char raw in input ?? string.Empty) { char c = char.ToUpperInvariant(raw); if (c >= 'A' && c <= 'Z') result.Append(c); } return result.ToString(); }
        private static string KeyText(Dictionary<string, string> values) { StringBuilder result = new StringBuilder(); foreach (KeyValuePair<string, string> pair in values) { if (result.Length > 0) result.Append(';'); result.Append(pair.Key).Append('=').Append(pair.Value); } return result.ToString(); }
    }
}
