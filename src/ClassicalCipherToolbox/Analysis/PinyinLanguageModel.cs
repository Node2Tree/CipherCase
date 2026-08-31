using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicalCipherToolbox.Analysis
{
    internal sealed class PinyinSegmentation { internal string Text; internal double Score; internal double Coverage; }

    internal static class PinyinLanguageModel
    {
        private static readonly Dictionary<string, double> Syllables = BuildSyllables();
        private static readonly Dictionary<int, double> Trigrams = BuildTrigrams();
        private const string Training = "womenzaishenghuozhongchangchanghuishiyongzhongwenjinxingjiaoliuzhongguoyouyoujiudewenhuaheshijiezuidauderenkoumeiyitianwomenxuexigongzuoshenghuoyeyaozhuyijiankangheanquanrenmindeshenghuoyuelaiyuehaokejifazhanyuerilaijinkuaisushidaidebianhuarangxinxichuanbogengjiabianjiefengyutiaoshunyiyuanmanxinxiangshichengguotaiminanjiatingxingfukuailepinganrenleiyongganmianduiweilaishijieshangmeiyoujuejuedeluozhichiyoujianchibuxiedecainengqudechenggongzhishiyegengxuyaoshijianheshi jianjianyanzhenlidebiaozhunshijianhuigaosuwomendaanxuewuzhijingqinfenkeyibuzhuotianfendushuwanshu juanxia biyourushen";

        internal static PinyinSegmentation Segment(string source)
        {
            string text = (source ?? string.Empty).ToLowerInvariant(); int n = text.Length; double[] score = new double[n + 1]; int[] previous = new int[n + 1]; bool[] word = new bool[n + 1]; int[] covered = new int[n + 1]; for (int i = 1; i <= n; i++) score[i] = double.NegativeInfinity;
            for (int start = 0; start < n; start++)
            {
                if (double.IsNegativeInfinity(score[start])) continue; Update(start + 1, start, score[start] - 5.5, covered[start], false, score, previous, covered, word);
                for (int length = 1; length <= 6 && start + length <= n; length++) { string syllable = text.Substring(start, length); double weight; if (Syllables.TryGetValue(syllable, out weight)) Update(start + length, start, score[start] + weight, covered[start] + length, true, score, previous, covered, word); }
            }
            List<string> pieces = new List<string>(); int position = n; while (position > 0) { int start = previous[position]; string part = text.Substring(start, position - start); pieces.Add(word[position] ? part : "[" + part + "]"); position = start; } pieces.Reverse(); return new PinyinSegmentation { Text = string.Join(" ", pieces.ToArray()), Score = score[n] + TrigramScore(text), Coverage = n == 0 ? 0 : covered[n] / (double)n };
        }

        internal static double Score(string source) { return Segment(source).Score; }
        internal static double[] LetterFrequencies()
        {
            double[] counts = new double[26]; double total = 0; foreach (char c in Training) if (c >= 'a' && c <= 'z') { counts[c - 'a']++; total++; } for (int i = 0; i < 26; i++) counts[i] = (counts[i] + .2) * 100.0 / (total + 5.2); return counts;
        }

        private static double TrigramScore(string text) { if (text.Length < 3) return 0; double score = 0; for (int i = 0; i + 2 < text.Length; i++) { int code = Code(text[i], text[i + 1], text[i + 2]); double weight; score += Trigrams.TryGetValue(code, out weight) ? weight : -.55; } return score; }
        private static int Code(char a, char b, char c) { return (a - 'a') * 676 + (b - 'a') * 26 + c - 'a'; }
        private static void Update(int at, int from, double value, int matched, bool isWord, double[] scores, int[] previous, int[] covered, bool[] words) { if (value < scores[at] || (value == scores[at] && matched <= covered[at])) return; scores[at] = value; previous[at] = from; covered[at] = matched; words[at] = isWord; }

        private static Dictionary<int, double> BuildTrigrams()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>(); string clean = Training.Replace(" ", string.Empty); for (int i = 0; i + 2 < clean.Length; i++) { int code = Code(clean[i], clean[i + 1], clean[i + 2]), count; counts[code] = counts.TryGetValue(code, out count) ? count + 1 : 1; } Dictionary<int, double> result = new Dictionary<int, double>(); foreach (KeyValuePair<int, int> row in counts) result[row.Key] = 1.2 + Math.Log(1 + row.Value) * .75; return result;
        }

        private static Dictionary<string, double> BuildSyllables()
        {
            const string data = "a ai an ang ao ba bai ban bang bao bei ben beng bi bian biao bie bin bing bo bu ca cai can cang cao ce cen ceng cha chai chan chang chao che chen cheng chi chong chou chu chua chuai chuan chuang chui chun chuo ci cong cou cu cuan cui cun cuo da dai dan dang dao de dei deng di dia dian diao die ding diu dong dou du duan dui dun duo e ei en eng er fa fan fang fei fen feng fo fou fu ga gai gan gang gao ge gei gen geng gong gou gu gua guai guan guang gui gun guo ha hai han hang hao he hei hen heng hong hou hu hua huai huan huang hui hun huo ji jia jian jiang jiao jie jin jing jiong jiu ju juan jue jun ka kai kan kang kao ke ken keng kong kou ku kua kuai kuan kuang kui kun kuo la lai lan lang lao le lei leng li lia lian liang liao lie lin ling liu long lou lu luan lun luo lv lve ma mai man mang mao me mei men meng mi mian miao mie min ming miu mo mou mu na nai nan nang nao ne nei nen neng ni nian niang niao nie nin ning niu nong nou nu nuan nuo nv nve o ou pa pai pan pang pao pei pen peng pi pian piao pie pin ping po pou pu qi qia qian qiang qiao qie qin qing qiong qiu qu quan que qun ran rang rao re ren reng ri rong rou ru rua ruan rui run ruo sa sai san sang sao se sen seng sha shai shan shang shao she shen sheng shi shou shu shua shuai shuan shuang shui shun shuo si song sou su suan sui sun suo ta tai tan tang tao te teng ti tian tiao tie ting tong tou tu tuan tui tun tuo wa wai wan wang wei wen weng wo wu xi xia xian xiang xiao xie xin xing xiong xiu xu xuan xue xun ya yan yang yao ye yi yin ying yo yong you yu yuan yue yun za zai zan zang zao ze zei zen zeng zha zhai zhan zhang zhao zhe zhen zheng zhi zhong zhou zhu zhua zhuai zhuan zhuang zhui zhun zhuo zi zong zou zu zuan zui zun zuo";
            Dictionary<string, double> result = new Dictionary<string, double>(); foreach (string value in data.Split(' ')) result[value] = value.Length * 2.15 - (value.Length == 1 ? 3.0 : 1.2); string[] common = { "shi", "de", "yi", "zhi", "you", "ren", "zhong", "guo", "wo", "men", "zai", "bu", "le", "he", "da", "xue", "sheng", "wen", "hua", "tian", "nian", "shi", "jian" }; for (int i = 0; i < common.Length; i++) result[common[i]] += 2.8 - i * .04; return result;
        }
    }
}
