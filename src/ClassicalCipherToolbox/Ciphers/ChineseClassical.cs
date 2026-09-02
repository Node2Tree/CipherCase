using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class ChineseLanguageScoring
    {
        internal static readonly string[] ModelChoices = { "自动", "现代汉语", "文言文" };
        private const string ModernCharacters = "的一是在不了有人我他这中来上大为和国地到以说时要就出会可也你对生能而子那得于着下自之年过发后作里用道行所然家种事成方多经去法学如都同现当没动面起看定天分还进好小部其些主样理心她本前开但因只从想实日军者意无力它与长把机十民第公此已工使情明性知全三又关点正业外将两高间由问很最重并物手应战向头文体政美相见被利二等产或新己制身果加月话合回特代内信表化老给世位次度门任常先海通教儿原东声提立及比员解水名真论处走义各入口认条平系气题活更别打女变四神总何电数安少报才结反受目太量再感建务做接必场件计管期市直德资命山金指许统区保至队形社便空决治展马科司五基眼书非则听白界达光放强难且权思王完设式色路记南品住告类求据程北边死张该规万取望觉术领共确传师观清今院让识候带导争运笑飞风步改收根干造言联持组每济车亲极林服快办议往元英士证近失转夫令准布始存未远叫台单影具字爱击流备兵连调深商算质团集百需价花华城石级整府离况请技际约示复病息究线官火断精满支视消越器容照须增研写称企功包史委查轻易早曾除农找装广显李标谈吃图念引历首医局突专费号尽另周较注语仅考落青随选列武红响推势参希古众构房半节土投某案维革划敌致陈律足态护兴派孩验责营星够章音跟志底站严例防族供效续施留讲型料终答紧绝奇察母京段依批群项故按河米围江织害斗双境客纪采举攻父苏密低朝友诉止细愿千值仍男钱破网热助倒育属坐帝限船脸职速刻乐否刚威毛状率独球般普怕校苦创假久错承印晚兰试股拿脑预谁益阳若微继送急血惊伤素药适波夜省初喜卫源食险待述陆习置居劳财环排福纳欢雷警获模充负云停木游龙树疑层冷洲冲射略范竟句室异激汉村策演简卡罪判担州静退既衣宗积余痛检差富灵协角占配征修皮挥胜降阶审沉坚善读超免压银买皇养怀执副乱抗犯追帮宣岁航优怪香著田铁控税左份穿艺背阵草脚概恶块顿敢守酒岛托央户烈洋索胡款靠评版宝座释景顾弟登货互付慢欧换闻危忙核暗介坏讨良序升监临亮露永呼味野架域沙掉括舰鱼杂误湾减编肯测败屋跑梦散温困剑渐封救贵枪缺楼县尚毫移画班智亦耳恩短掌恐遗固席松秘谢遇康虑幸均销钟诗藏赶剧票损忽巨旧端探湖录叶春乡附吸予礼港雨板庭归睛饭额含顺输摇招婚脱补谓督毒油疗旅泽材灭逐莫笔亡鲜词圣择寻厂睡烟授诺伦岸奥唐卖炸载健堂旁宫喝借君禁阴园谋宋避抓荣逃牙束跳顶玉镇雪午练迫篇馆遍凡础洞卷坦牛宁纸诸训私庄祖丝翻暴森塔默握戏隐熟骨访弱蒙店软典欲遭盘扩盖雄稳忘刺拥徒齐赛趣曲刀床迎冰虚玩析窗醒透购替塞休虎扬途侵刑绿迅套贸毕唯谷轮库迹尤竞街促延震弃甲伟麻川申缓潜闪售灯针哲络抵朱埃抱鼓植纯夏忍页杰筑折郑贝尊吴秀混臣雅振染盛怒舞圆搞狂措姓残秋培迷诚宽宇猛摆梅毁伸摩盟末乃悲拍丁赵硬麦蒋操耶阻订彩抽赞魔纷沿喊违浪汇币丰蓝殊献桌瓦莱援译夺汽烧距裁偏符勇触课懂墙袭召罚厅拜巧侧韩冒债曼融惯享戴童犹乘挂奖绍厚纵障讯涉彻刊丈爆乌役描洗玛患妙镜唱烦签彼弗症仿倾牌陷鸟轰菜闭奋庆撤泪茶疾缘播朗杜季丹尾仪奔珠虫驻孔宜桥淡翼恨繁寒伴叹旦愈潮粮缩罢聚径恰挑袋灰捕徐珍幕映裂泰隔启尖忠累炎暂估泛荒偿横拒瑞忆孤鼻闹羊厉衡胞零穷舍码赫魂灾洪腿胆津俗辩胸晓劲贫仁偶辑";
        private const string ClassicalCharacters = "之其者也矣焉乎哉兮乃曰云吾余予汝尔若彼此斯夫盖故然则而以于为与及或亦既未莫无弗勿非所孰何安岂胡奚曷虽且苟诚惟唯凡诸皆各每有无上下左右前后内外大小多少长短高下天地日月山川江河风云雨雪水火木金土君臣父子兄弟夫妇朋友民国天下王侯将相士农工商兵军师道德仁义礼智信忠孝廉耻圣贤神鬼生死古今春秋朝夕岁时行止出入往来言语视听闻见思虑喜怒哀乐善恶美丑荣辱治乱兴亡成败利害得失真伪同异本末始终轻重缓急进退存亡知不知可不可人心性命文章诗书礼乐易春秋经史子集帝皇后妃太子公卿大夫百姓黎庶匹夫寡人孤臣妾仆犬马舟车宫室城郭道路田野草木鸟兽虫鱼衣食器用";
        private static readonly string[] ModernPhrases = { "我们", "你们", "他们", "中国", "人民", "国家", "社会", "可以", "没有", "一个", "这个", "为了", "因为", "所以", "如果", "但是", "已经", "还是", "以及", "进行", "发展", "工作", "问题", "生活", "时间", "现在", "需要", "通过", "世界", "重要", "可能", "应该", "知道", "自己", "什么", "怎么", "中华人民共和国", "密码", "信息", "文本", "语言", "历史" };
        private static readonly string[] ClassicalPhrases = { "天地", "玄黄", "宇宙", "洪荒", "日月", "盈昃", "辰宿", "列张", "之乎者也", "不可", "可以", "所谓", "故曰", "子曰", "君子", "小人", "天下", "圣人", "其人", "其事", "是以", "于是", "而后", "然后", "何以", "无为", "有道", "古者", "昔者", "今者", "若夫", "嗟乎", "诚哉", "民为贵", "社稷", "王侯", "将相" };

        internal static string Analyze(string text, string requested)
        {
            string selected; double score = Score(text, requested, out selected); int han = HanCount(text), total = UnicodeAnalysis.Units(text ?? string.Empty).Count;
            StringBuilder result = new StringBuilder(); result.Append("模型：").Append(selected).Append("\r\n匹配：").Append(score.ToString("0.0", CultureInfo.InvariantCulture)).Append(" / 98\r\n汉字：").Append(han).Append(" / ").Append(total).Append("\r\n现代汉语：").Append(Score(text, "现代汉语", out selected).ToString("0.0", CultureInfo.InvariantCulture)).Append("\r\n文言文：").Append(Score(text, "文言文", out selected).ToString("0.0", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        internal static double Score(string text, string requested, out string selected)
        {
            string value = requested ?? string.Empty; if (value == "现代" || value.Equals("MODERN", StringComparison.OrdinalIgnoreCase)) value = "现代汉语"; if (value == "古文" || value == "古汉语" || value.Equals("CLASSICAL", StringComparison.OrdinalIgnoreCase)) value = "文言文";
            double modern = ScoreModel(text, ModernCharacters, ModernPhrases), classical = ScoreModel(text, ClassicalCharacters, ClassicalPhrases);
            if (value == "现代汉语") { selected = value; return Normalize(modern, HanCount(text)); }
            if (value == "文言文") { selected = value; return Normalize(classical, HanCount(text)); }
            selected = modern >= classical ? "现代汉语" : "文言文"; return Normalize(Math.Max(modern, classical), HanCount(text));
        }

        internal static double Raw(string text, string requested, out string selected)
        {
            string value = requested ?? string.Empty; double modern = ScoreModel(text, ModernCharacters, ModernPhrases), classical = ScoreModel(text, ClassicalCharacters, ClassicalPhrases);
            if (value == "现代汉语" || value == "现代") { selected = "现代汉语"; return modern; }
            if (value == "文言文" || value == "古文" || value == "古汉语") { selected = "文言文"; return classical; }
            selected = modern >= classical ? "现代汉语" : "文言文"; return Math.Max(modern, classical);
        }

        private static double ScoreModel(string text, string common, string[] phrases)
        {
            double score = 0; int han = 0; foreach (string unit in UnicodeAnalysis.Units(text ?? string.Empty)) { int cp = char.ConvertToUtf32(unit, 0); if (!IsHan(cp)) continue; han++; int rank = common.IndexOf(unit, StringComparison.Ordinal); score += rank < 0 ? -1.15 : 1.8 - Math.Min(1.1, rank / (double)Math.Max(1, common.Length)); }
            foreach (string phrase in phrases) { int at = 0; while ((at = (text ?? string.Empty).IndexOf(phrase, at, StringComparison.Ordinal)) >= 0) { score += 2.4 + phrase.Length * .85; at += phrase.Length; } }
            return han == 0 ? -1000 : score;
        }

        private static double Normalize(double raw, int count)
        {
            if (count == 0) return 0; double per = raw / count, value = 24 + (per + 1.2) * 27; double cap = 60 + 38 * (1 - Math.Exp(-count / 55.0)); return Math.Max(0, Math.Min(98, Math.Min(value, cap)));
        }
        private static int HanCount(string text) { int count = 0; foreach (string unit in UnicodeAnalysis.Units(text ?? string.Empty)) if (IsHan(char.ConvertToUtf32(unit, 0))) count++; return count; }
        private static bool IsHan(int cp) { return (cp >= 0x3400 && cp <= 0x9FFF) || (cp >= 0x20000 && cp <= 0x323AF); }
    }

    internal static class ChineseTelegraphCipher
    {
        private sealed class Candidate { internal int Key; internal string Plain; internal string Model; internal double Score; internal int Valid; }

        internal static string Transform(string input, string keyText, bool decrypt)
        {
            int key = ParseKey(keyText); return decrypt ? Decode(input, key, false, out key) : Encode(input, key);
        }

        internal static string Crack(ToolRequest request)
        {
            List<int> groups = Groups(request.Input); if (groups.Count < 2) throw new CipherException("请输入至少两个四位中文电码组"); string model = request.Get("model"); List<Candidate> best = new List<Candidate>();
            for (int key = 0; key < 10000; key++)
            {
                int ignored; string plain = DecodeGroups(groups, key, out ignored); string selected; double language = ChineseLanguageScoring.Raw(plain, model, out selected); double score = language + ignored * 2.2 - (groups.Count - ignored) * 4.5; Candidate candidate = new Candidate { Key = key, Plain = plain, Model = selected, Score = score, Valid = ignored };
                Insert(best, candidate, 16); if ((key & 127) == 0) { request.ThrowIfCancellationRequested(); request.ReportProgress(key / 100, "中文电码加密 · 尝试密钥数"); }
            }
            request.ReportProgress(100, "中文电码加密 · 完成"); StringBuilder result = new StringBuilder(); for (int i = 0; i < best.Count; i++) { if (i > 0) result.Append("\r\n\r\n"); string selected; double match = ChineseLanguageScoring.Score(best[i].Plain, model, out selected); result.Append('#').Append(i + 1).Append("  密钥 ").Append(best[i].Key.ToString("D4", CultureInfo.InvariantCulture)).Append("  模型 ").Append(best[i].Model).Append("  匹配 ").Append(match.ToString("0.0", CultureInfo.InvariantCulture)).Append("  有效 ").Append(best[i].Valid).Append('/').Append(groups.Count).Append("\r\n明文：").Append(best[i].Plain); }
            return result.ToString();
        }

        private static string Encode(string input, int key)
        {
            List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) { string code; if (ChineseTelegraphCode.TryGetCode(unit, out code)) result.Add(((int.Parse(code, CultureInfo.InvariantCulture) + key) % 10000).ToString("D4", CultureInfo.InvariantCulture)); else if (!string.IsNullOrWhiteSpace(unit)) result.Add(unit); } return string.Join(" ", result.ToArray());
        }
        private static string Decode(string input, int key, bool tolerate, out int valid) { List<int> groups = Groups(input); if (groups.Count == 0) throw new CipherException("请输入四位中文电码组"); string value = DecodeGroups(groups, key, out valid); if (!tolerate && valid != groups.Count) throw new CipherException("部分密钥还原值不在中文电码表中"); return value; }
        private static string DecodeGroups(List<int> groups, int key, out int valid)
        {
            StringBuilder output = new StringBuilder(); valid = 0; foreach (int group in groups) { int original = (group - key + 10000) % 10000; string character; if (ChineseTelegraphCode.TryGetCharacter(original.ToString("D4", CultureInfo.InvariantCulture), out character)) { output.Append(character); valid++; } else output.Append('□'); } return output.ToString();
        }
        private static List<int> Groups(string input)
        {
            string compact = Regex.Replace(input ?? string.Empty, "[^0-9]", string.Empty); if (compact.Length == 0 || compact.Length % 4 != 0) throw new CipherException("电码应由四位十进制数组成；可使用空格、逗号或连字符分隔"); List<int> result = new List<int>(); for (int i = 0; i < compact.Length; i += 4) result.Add(int.Parse(compact.Substring(i, 4), CultureInfo.InvariantCulture)); return result;
        }
        private static int ParseKey(string value) { int key; if (!int.TryParse((value ?? string.Empty).Trim(), out key) || key < 0 || key > 9999) throw new CipherException("密钥数应为 0–9999"); return key; }
        private static void Insert(List<Candidate> values, Candidate candidate, int limit) { int at = values.FindIndex(delegate(Candidate item) { return candidate.Score > item.Score; }); if (at < 0) at = values.Count; values.Insert(at, candidate); if (values.Count > limit) values.RemoveAt(values.Count - 1); }
    }

    internal static class FanqieWorkbench
    {
        private static readonly Dictionary<string, string> InitialToCharacter = Map("=安,b=帮,p=滂,m=明,f=非,d=端,t=透,n=泥,l=来,g=见,k=溪,h=晓,j=精,q=清,x=心,zh=知,ch=彻,sh=审,r=日,z=资,c=雌,s=思,y=以,w=乌");
        private static readonly Dictionary<string, string> FinalToCharacter = Map("a=阿,o=喔,e=鹅,ai=哀,ei=欸,ao=熬,ou=欧,an=安,en=恩,ang=昂,eng=亨,ong=翁,er=儿,i=衣,ia=压,ie=耶,iao=腰,iu=忧,ian=烟,in=因,iang=央,ing=英,iong=雍,u=乌,ua=蛙,uo=窝,uai=歪,ui=威,uan=弯,un=温,uang=汪,v=迂,ve=约,van=冤,vn=晕");
        private static readonly Dictionary<string, string> CharacterToInitial = Reverse(InitialToCharacter), CharacterToFinal = Reverse(FinalToCharacter);

        internal static string Transform(string input, bool decode) { return decode ? Decode(input) : Encode(input); }
        internal static int MatchCount(string input, out int total)
        {
            string[] tokens = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，', '/', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries); total = tokens.Length; int matched = 0; foreach (string raw in tokens) { List<string> units = new List<string>(UnicodeAnalysis.Units(raw)); if (units.Count > 0 && units[units.Count - 1].Length == 1 && units[units.Count - 1][0] >= '1' && units[units.Count - 1][0] <= '5') units.RemoveAt(units.Count - 1); if (units.Count == 2 && CharacterToInitial.ContainsKey(units[0]) && CharacterToFinal.ContainsKey(units[1])) matched++; } return matched;
        }
        private static string Encode(string input)
        {
            List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) { IList<string> readings = ChineseInputCode.CodesFor(unit, "汉语拼音（数字声调）"); if (readings.Count == 0) { if (!string.IsNullOrWhiteSpace(unit)) result.Add("[" + unit + "]"); continue; } string syllable = readings[0], plain; int tone; SplitTone(syllable, out plain, out tone); string initial, final; SplitSyllable(plain, out initial, out final); string first, second; if (!InitialToCharacter.TryGetValue(initial, out first) || !FinalToCharacter.TryGetValue(final, out second)) result.Add("[" + unit + ":" + syllable + "]"); else result.Add(first + second + (tone > 0 ? tone.ToString(CultureInfo.InvariantCulture) : string.Empty)); } return string.Join(" ", result.ToArray());
        }
        private static string Decode(string input)
        {
            string[] tokens = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，', '/', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries); if (tokens.Length == 0) throw new CipherException("请输入反切对，例如 知翁1"); StringBuilder output = new StringBuilder(); foreach (string raw in tokens) { List<string> units = new List<string>(UnicodeAnalysis.Units(raw)); int tone = 0; if (units.Count > 0 && units[units.Count - 1].Length == 1 && units[units.Count - 1][0] >= '1' && units[units.Count - 1][0] <= '5') { tone = units[units.Count - 1][0] - '0'; units.RemoveAt(units.Count - 1); } if (units.Count != 2) { if (output.Length > 0) output.Append("\r\n"); output.Append(raw).Append(" → 格式应为两个反切字和可选声调"); continue; } string initial, final; if (!CharacterToInitial.TryGetValue(units[0], out initial) || !CharacterToFinal.TryGetValue(units[1], out final)) { if (output.Length > 0) output.Append("\r\n"); output.Append(raw).Append(" → 未收录反切字"); continue; } string pinyin = initial + final + (tone > 0 ? tone.ToString(CultureInfo.InvariantCulture) : string.Empty); if (output.Length > 0) output.Append("\r\n"); output.Append(raw).Append(" → ").Append(pinyin).Append(" → ").Append(ChineseInputCode.LookupSummary(pinyin, tone > 0 ? "汉语拼音（数字声调）" : "汉语拼音")); } return output.ToString();
        }
        private static void SplitTone(string value, out string plain, out int tone) { tone = 0; plain = value ?? string.Empty; if (plain.Length > 0 && plain[plain.Length - 1] >= '1' && plain[plain.Length - 1] <= '5') { tone = plain[plain.Length - 1] - '0'; plain = plain.Substring(0, plain.Length - 1); } plain = plain.Replace('ü', 'v'); }
        private static void SplitSyllable(string value, out string initial, out string final) { string[] initials = { "zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "r", "z", "c", "s", "y", "w" }; initial = string.Empty; final = value; foreach (string item in initials) if (value.StartsWith(item, StringComparison.Ordinal)) { initial = item; final = value.Substring(item.Length); break; } if ((initial == "j" || initial == "q" || initial == "x") && final.StartsWith("u", StringComparison.Ordinal)) final = "v" + final.Substring(1); }
        private static Dictionary<string, string> Map(string source) { Dictionary<string, string> result = new Dictionary<string, string>(); foreach (string item in source.Split(',')) { int at = item.IndexOf('='); result[item.Substring(0, at)] = item.Substring(at + 1); } return result; }
        private static Dictionary<string, string> Reverse(Dictionary<string, string> source) { Dictionary<string, string> result = new Dictionary<string, string>(); foreach (KeyValuePair<string, string> pair in source) if (!result.ContainsKey(pair.Value)) result[pair.Value] = pair.Key; return result; }
    }

    internal static class ChineseCodebookCipher
    {
        private sealed class Book { internal readonly Dictionary<string, List<string>> Encode = new Dictionary<string, List<string>>(); internal readonly Dictionary<string, string> Decode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); internal readonly List<string> Phrases = new List<string>(); }
        internal static string Transform(string input, string map, bool decrypt) { Book book = Parse(map); return decrypt ? Decode(input, book) : Encode(input, book); }
        internal static string Statistics(string map) { Book book = Parse(map); return "词条：" + book.Encode.Count + "\r\n代码：" + book.Decode.Count + "\r\n最长词组：" + (book.Phrases.Count == 0 ? 0 : UnicodeAnalysis.Units(book.Phrases[0]).Count); }
        private static string Encode(string input, Book book)
        {
            string source = input ?? string.Empty; StringBuilder result = new StringBuilder(); int at = 0; while (at < source.Length) { string found = null; foreach (string phrase in book.Phrases) if (at + phrase.Length <= source.Length && string.CompareOrdinal(source, at, phrase, 0, phrase.Length) == 0) { found = phrase; break; } if (found != null) { if (result.Length > 0 && result[result.Length - 1] != ' ') result.Append(' '); result.Append(book.Encode[found][0]).Append(' '); at += found.Length; } else { result.Append(source[at]); at++; } } return Regex.Replace(result.ToString().Trim(), "[ ]{2,}", " ");
        }
        private static string Decode(string input, Book book) { StringBuilder result = new StringBuilder(); foreach (string token in Regex.Split((input ?? string.Empty).Trim(), "(\\s+|[,，;；/]+)")) { if (token.Length == 0 || Regex.IsMatch(token, "^\\s+$") || Regex.IsMatch(token, "^[,，;；/]+$")) continue; string phrase; result.Append(book.Decode.TryGetValue(token, out phrase) ? phrase : "[" + token + "]"); } return result.ToString(); }
        private static Book Parse(string map)
        {
            if (string.IsNullOrWhiteSpace(map)) throw new CipherException("请填写或导入中文代码本"); Book book = new Book(); foreach (string raw in Regex.Split(map.Replace(";", "\n").Replace("；", "\n"), "[\\r\\n]+")) { string line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue; string[] pair = line.Split(new[] { '=', '＝', '\t', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries); if (pair.Length != 2) continue; string phrase = pair[0].Trim(), codes = pair[1].Trim(); if (!ContainsHan(phrase) && ContainsHan(codes)) { string swap = phrase; phrase = codes; codes = swap; } foreach (string code in codes.Split(new[] { '/', ',', '，', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries)) { List<string> values; if (!book.Encode.TryGetValue(phrase, out values)) { values = new List<string>(); book.Encode[phrase] = values; book.Phrases.Add(phrase); } if (!values.Contains(code)) values.Add(code); if (!book.Decode.ContainsKey(code)) book.Decode[code] = phrase; } } if (book.Encode.Count == 0) throw new CipherException("代码本没有可用词条；每行填写“词组=代码”"); book.Phrases.Sort(delegate(string a, string b) { int length = b.Length.CompareTo(a.Length); return length != 0 ? length : string.CompareOrdinal(a, b); }); return book;
        }
        private static bool ContainsHan(string value) { foreach (string unit in UnicodeAnalysis.Units(value ?? string.Empty)) { int cp = char.ConvertToUtf32(unit, 0); if ((cp >= 0x3400 && cp <= 0x9FFF) || (cp >= 0x20000 && cp <= 0x323AF)) return true; } return false; }
    }

    internal static class ChineseSteganalysis
    {
        private sealed class Candidate { internal string Path, Text, Model; internal double Match, Rank; }
        internal static readonly string[] PathChoices = { "自动", "藏头/藏尾", "间隔取字", "方阵路线" };
        internal static string Analyze(ToolRequest request)
        {
            string source = request.Input ?? string.Empty, path = request.Get("path"), model = request.Get("model"); int width; int.TryParse(request.Get("width"), out width); List<Candidate> candidates = new List<Candidate>(); HashSet<string> seen = new HashSet<string>();
            string[] lines = source.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); if (path.Length == 0 || path == "自动" || path == "藏头/藏尾") AddLineCandidates(lines, model, candidates, seen);
            List<string> units = CompactUnits(source); if (path.Length == 0 || path == "自动" || path == "间隔取字") AddIntervalCandidates(units, model, candidates, seen);
            if (path.Length == 0 || path == "自动" || path == "方阵路线") AddGridCandidates(units, width, model, candidates, seen);
            candidates.Sort(delegate(Candidate a, Candidate b) { return b.Rank.CompareTo(a.Rank); }); if (candidates.Count == 0) throw new CipherException("文本太短，至少需要两行或四个连续字符"); StringBuilder output = new StringBuilder(); int count = Math.Min(30, candidates.Count); for (int i = 0; i < count; i++) { if (i > 0) output.Append("\r\n\r\n"); Candidate item = candidates[i]; output.Append('#').Append(i + 1).Append("  路径 ").Append(item.Path).Append("  模型 ").Append(item.Model).Append("  匹配 ").Append(item.Match.ToString("0.0", CultureInfo.InvariantCulture)).Append("\r\n文本：").Append(item.Text); } return output.ToString();
        }
        private static void AddLineCandidates(string[] lines, string model, List<Candidate> values, HashSet<string> seen) { if (lines.Length < 2) return; Add(values, seen, "每行首字", Ends(lines, true, false), model); Add(values, seen, "每行尾字", Ends(lines, false, false), model); Add(values, seen, "每行首个汉字", Ends(lines, true, true), model); Add(values, seen, "每行末个汉字", Ends(lines, false, true), model); int maximum = int.MaxValue; foreach (string line in lines) maximum = Math.Min(maximum, UnicodeAnalysis.Units(line.Trim()).Count); for (int n = 1; n < Math.Min(10, maximum); n++) Add(values, seen, "每行第 " + (n + 1) + " 字", Nth(lines, n), model); }
        private static void AddIntervalCandidates(List<string> units, string model, List<Candidate> values, HashSet<string> seen) { for (int step = 2; step <= Math.Min(16, units.Count / 2); step++) for (int offset = 0; offset < step; offset++) { StringBuilder text = new StringBuilder(); for (int i = offset; i < units.Count; i += step) text.Append(units[i]); Add(values, seen, "每 " + step + " 字 · 起点 " + (offset + 1), text.ToString(), model); } }
        private static void AddGridCandidates(List<string> units, int requestedWidth, string model, List<Candidate> values, HashSet<string> seen) { int min = requestedWidth >= 2 ? requestedWidth : 2, max = requestedWidth >= 2 ? requestedWidth : Math.Min(20, units.Count / 2); for (int width = min; width <= max; width++) { int height = (units.Count + width - 1) / width; if (height < 2) continue; Add(values, seen, width + " 列 · 逐列向下", Columns(units, width, false, false), model); Add(values, seen, width + " 列 · 逐列向上", Columns(units, width, true, false), model); Add(values, seen, width + " 列 · 蛇形逐列", Columns(units, width, false, true), model); if (units.Count == width * height) { Add(values, seen, width + " 列 · 顺时针螺旋", Spiral(units, width, false), model); Add(values, seen, width + " 列 · 逆时针螺旋", Spiral(units, width, true), model); Add(values, seen, width + " 列 · 主对角线", Diagonal(units, width, false), model); Add(values, seen, width + " 列 · 副对角线", Diagonal(units, width, true), model); } } }
        private static void Add(List<Candidate> values, HashSet<string> seen, string path, string text, string model) { if (string.IsNullOrEmpty(text) || text.Length < 2 || !seen.Add(text)) return; string selected; double match = ChineseLanguageScoring.Score(text, model, out selected); double lengthWeight = Math.Min(10, UnicodeAnalysis.Units(text).Count) * .65; values.Add(new Candidate { Path = path, Text = text, Model = selected, Match = match, Rank = match + lengthWeight }); }
        private static string Ends(string[] lines, bool first, bool hanOnly) { StringBuilder result = new StringBuilder(); foreach (string line in lines) { List<string> units = new List<string>(UnicodeAnalysis.Units(line.Trim())); if (first) { foreach (string unit in units) if (!hanOnly || IsHan(unit)) { result.Append(unit); break; } } else for (int i = units.Count - 1; i >= 0; i--) if (!hanOnly || IsHan(units[i])) { result.Append(units[i]); break; } } return result.ToString(); }
        private static string Nth(string[] lines, int n) { StringBuilder result = new StringBuilder(); foreach (string line in lines) { IList<string> units = UnicodeAnalysis.Units(line.Trim()); if (n < units.Count) result.Append(units[n]); } return result.ToString(); }
        private static List<string> CompactUnits(string text) { List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(text ?? string.Empty)) if (!string.IsNullOrWhiteSpace(unit)) result.Add(unit); return result; }
        private static string Columns(List<string> units, int width, bool upward, bool snake) { int height = (units.Count + width - 1) / width; StringBuilder result = new StringBuilder(); for (int x = 0; x < width; x++) { bool reverse = upward || (snake && (x & 1) == 1); for (int k = 0; k < height; k++) { int y = reverse ? height - 1 - k : k, at = y * width + x; if (at < units.Count) result.Append(units[at]); } } return result.ToString(); }
        private static string Diagonal(List<string> units, int width, bool reverse) { int height = units.Count / width, count = Math.Min(width, height); StringBuilder result = new StringBuilder(); for (int i = 0; i < count; i++) result.Append(units[i * width + (reverse ? width - 1 - i : i)]); return result.ToString(); }
        private static string Spiral(List<string> units, int width, bool counterClockwise) { int height = units.Count / width, left = 0, right = width - 1, top = 0, bottom = height - 1; StringBuilder result = new StringBuilder(); while (left <= right && top <= bottom) { if (!counterClockwise) { for (int x = left; x <= right; x++) result.Append(units[top * width + x]); top++; for (int y = top; y <= bottom; y++) result.Append(units[y * width + right]); right--; if (top <= bottom) { for (int x = right; x >= left; x--) result.Append(units[bottom * width + x]); bottom--; } if (left <= right) { for (int y = bottom; y >= top; y--) result.Append(units[y * width + left]); left++; } } else { for (int y = top; y <= bottom; y++) result.Append(units[y * width + left]); left++; for (int x = left; x <= right; x++) result.Append(units[bottom * width + x]); bottom--; if (left <= right) { for (int y = bottom; y >= top; y--) result.Append(units[y * width + right]); right--; } if (top <= bottom) { for (int x = right; x >= left; x--) result.Append(units[top * width + x]); top++; } } } return result.ToString(); }
        private static bool IsHan(string unit) { int cp = char.ConvertToUtf32(unit, 0); return (cp >= 0x3400 && cp <= 0x9FFF) || (cp >= 0x20000 && cp <= 0x323AF); }
    }
}

