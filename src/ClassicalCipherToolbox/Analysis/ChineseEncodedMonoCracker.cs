using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Analysis
{
    internal static class ChineseEncodedMonoCracker
    {
        private sealed class Candidate { internal char[] Key; internal double Score; internal PinyinSegmentation Segmentation; }
        private sealed class HexCandidate { internal int[] Key; internal double Score; internal string Text; internal double ChineseRatio; }

        internal static string Crack(string input, string iterationsText, Action<int, string> progress, Func<bool> cancellation)
        {
            List<string> symbols = new List<string>(); string encoded = Encode(input, symbols); if (encoded.Length < 40) throw new CipherException("中文编码单表破解至少需要 40 个符号"); if (symbols.Count > 26) throw new CipherException("中文编码单表破解最多支持 26 个不同符号"); int budget; if (!int.TryParse(iterationsText, out budget) || budget < 1000) budget = 100000; budget = Math.Min(5000000, budget); List<HexCandidate> hex = symbols.Count == 16 && encoded.Length % 4 == 0 ? SearchUnicodeHex(encoded, Math.Max(1000, budget * 3 / 5), progress, cancellation) : new List<HexCandidate>(); int pinyinBudget = hex.Count > 0 && hex[0].ChineseRatio >= .72 ? Math.Max(1000, budget / 10) : budget - (hex.Count > 0 ? budget * 3 / 5 : 0); int[] counts = new int[26]; foreach (char c in encoded) counts[c - 'A']++; char[] seed = FrequencySeed(counts); List<Candidate> best = new List<Candidate>(); Random random = new Random(24691 + encoded.Length); int chains = pinyinBudget < 10000 ? 4 : 10, perChain = Math.Max(1, pinyinBudget / chains);
            for (int chain = 0; chain < chains; chain++)
            {
                char[] key = (char[])seed.Clone(); for (int i = 0; i < 20 + chain * 11; i++) Swap(key, random.Next(symbols.Count), random.Next(26)); double score = Score(encoded, key), chainBest = score; char[] chainKey = (char[])key.Clone();
                for (int iteration = 0; iteration < perChain; iteration++)
                {
                    if ((iteration & 511) == 0) { if (cancellation != null && cancellation()) throw new OperationCanceledException(); if (progress != null && (iteration & 4095) == 0) progress(Math.Min(99, 60 + (chain * perChain + iteration) * 39 / Math.Max(1, pinyinBudget)), "单表替换 · 中文拼音 " + (chain * perChain + iteration).ToString("N0", CultureInfo.InvariantCulture) + "/" + pinyinBudget.ToString("N0", CultureInfo.InvariantCulture)); }
                    int a = random.Next(symbols.Count), b; do { b = random.Next(26); } while (a == b); bool cycle = random.Next(100) < 18; int c = -1; if (cycle) do { c = random.Next(26); } while (c == a || c == b); if (cycle) Cycle(key, a, b, c); else Swap(key, a, b); double next = Score(encoded, key); double temperature = 18.0 * (1.0 - iteration / (double)perChain) + .08; if (next >= score || random.NextDouble() < Math.Exp(Math.Max(-700, (next - score) / temperature))) score = next; else { if (cycle) Uncycle(key, a, b, c); else Swap(key, a, b); } if (score > chainBest) { chainBest = score; chainKey = (char[])key.Clone(); }
                }
                Add(best, encoded, chainKey, chainBest);
            }
            best.Sort(delegate(Candidate a, Candidate b) { return b.Score.CompareTo(a.Score); }); if (progress != null) progress(100, "单表替换 · 中文编码完成"); return Format(hex, best, symbols, encoded);
        }

        private static List<HexCandidate> SearchUnicodeHex(string encoded, int budget, Action<int, string> progress, Func<bool> cancellation)
        {
            int[] seed = HexSeed(encoded); Random random = new Random(97531 + encoded.Length); int chains = budget < 10000 ? 4 : 12, perChain = Math.Max(1, budget / chains); List<HexCandidate> best = new List<HexCandidate>();
            for (int chain = 0; chain < chains; chain++)
            {
                int[] key = (int[])seed.Clone(); for (int i = 0; i < chain * 7 + 5; i++) Swap(key, random.Next(16), random.Next(16)); string text; double ratio; double score = HexScore(encoded, key, out text, out ratio), chainBest = score, chainRatio = ratio; int[] chainKey = (int[])key.Clone(); string chainText = text;
                for (int iteration = 0; iteration < perChain; iteration++)
                {
                    if ((iteration & 511) == 0) { if (cancellation != null && cancellation()) throw new OperationCanceledException(); if (progress != null && (iteration & 4095) == 0) progress(Math.Min(59, (chain * perChain + iteration) * 60 / Math.Max(1, budget)), "单表替换 · Unicode 十六进制 " + (chain * perChain + iteration).ToString("N0", CultureInfo.InvariantCulture) + "/" + budget.ToString("N0", CultureInfo.InvariantCulture)); }
                    int a = random.Next(16), b; do { b = random.Next(16); } while (a == b); Swap(key, a, b); double next = HexScore(encoded, key, out text, out ratio); double temperature = 28.0 * (1.0 - iteration / (double)perChain) + .05; if (next >= score || random.NextDouble() < Math.Exp(Math.Max(-700, (next - score) / temperature))) score = next; else Swap(key, a, b); if (score > chainBest) { chainBest = score; chainKey = (int[])key.Clone(); chainText = text; chainRatio = ratio; }
                }
                AddHex(best, chainKey, chainBest, chainText, chainRatio);
            }
            best.Sort(delegate(HexCandidate a, HexCandidate b) { return b.Score.CompareTo(a.Score); }); return best;
        }

        private static double HexScore(string encoded, int[] key, out string text, out double ratio)
        {
            StringBuilder result = new StringBuilder(encoded.Length / 4); double score = 0; int chinese = 0; char previous = '\0'; for (int i = 0; i + 3 < encoded.Length; i += 4) { int code = key[encoded[i] - 'A'] * 4096 + key[encoded[i + 1] - 'A'] * 256 + key[encoded[i + 2] - 'A'] * 16 + key[encoded[i + 3] - 'A']; char value = (char)code; result.Append(value); double weight; if (ChineseWeights.TryGetValue(value, out weight)) { score += 18 + weight; chinese++; } else if (IsChinese(value)) { score += 15; chinese++; } else if (IsChinesePunctuation(value)) score += 7; else if (char.IsWhiteSpace(value)) score += 3; else if (!char.IsControl(value) && !char.IsSurrogate(value)) score -= 7; else score -= 30; if (previous != '\0' && CommonChinesePairs.Contains(new string(new[] { previous, value }))) score += 5.5; previous = value; } text = result.ToString(); ratio = result.Length == 0 ? 0 : chinese / (double)result.Length; return score;
        }

        private static int[] HexSeed(string encoded)
        {
            int[] firstCounts = new int[16], totalCounts = new int[16]; for (int i = 0; i < encoded.Length; i++) { int symbol = encoded[i] - 'A'; totalCounts[symbol]++; if ((i & 3) == 0) firstCounts[symbol]++; } List<int> symbols = new List<int>(); for (int i = 0; i < 16; i++) symbols.Add(i); symbols.Sort(delegate(int a, int b) { int first = firstCounts[b].CompareTo(firstCounts[a]); return first != 0 ? first : totalCounts[b].CompareTo(totalCounts[a]); }); int[] digitOrder = { 4, 5, 6, 7, 8, 9, 3, 15, 2, 0, 1, 10, 11, 12, 13, 14 }, key = new int[16]; for (int i = 0; i < 16; i++) key[symbols[i]] = digitOrder[i]; return key;
        }

        private static void AddHex(List<HexCandidate> values, int[] key, double score, string text, double ratio) { string signature = string.Join(",", key); foreach (HexCandidate item in values) if (string.Join(",", item.Key) == signature) return; values.Add(new HexCandidate { Key = key, Score = score, Text = text, ChineseRatio = ratio }); }
        private static bool IsChinese(char value) { return value >= '\u3400' && value <= '\u9FFF'; }
        private static bool IsChinesePunctuation(char value) { return "，。！？；：、“”‘’（）《》【】—…·".IndexOf(value) >= 0; }
        private static readonly string CommonChinesePairs = "我们人民中国中华文化社会生活学习工作时间世界发展国家语言文字知道可以没有一个什么这个那个因为所以如果但是还是自己他们你们今天现在已经需要喜欢希望问题事情地方时候这样非常重要进行使用通过成为发现一定可能应该不能不会大家孩子朋友老师学生";
        private static readonly Dictionary<char, double> ChineseWeights = BuildChineseWeights();
        private static Dictionary<char, double> BuildChineseWeights() { const string common = "的一是不了在人有我他这中大来上个国到说们为子和你地出道也时年得就那要下以生会自着去之过家学对可她里后小么心多天而能好都然没日于起还发成事只作当想看文无开手十用主行方又如前所本见经头面公同三已老从动两长知民样现分将外但身些与高意进把法此实回二理力它应女种教工使便度明性先名情加化太战此间真话利因很定表最向全相点新内数正心反你明看原又么利比或但质气第向道命此变条只没结解问意建月公无系军很情者最立代想已通并提直题党程展五果料象员革位入常文总次品式活设及管特件长求老头基资边流路级少图山统接知较将组见计别她手角期根论运农指几九区强放决西被干做必先回则任取据处理世车社步万约各目达走积示议声报斗完类八离华名确才科张信马节话米整空元况今集温传土许步群广石记需段研界拉林律叫且究观越织装影算低持音众书布复容儿须际商非验连断深难近矿千周委素技备半办青省列习响约支般史感劳便团往酸历市克何除消构府称太准精值号率族维划选标写存候毛亲快效斯院查江型眼王按格养易置派层片始却专状育厂京识适属圆包火住调满县局照参红细引听该铁价严首底液官德调随病苏失尔死讲配女黄推显谈罪神艺呢席含企望密批营项防举球英氧势告李台落木帮轮破亚师围注远字材排供河态封另施减树溶怎止案言士均武固叶鱼波视仅费紧爱左章早朝害续轻服试食充兵源判护司足某练差致板田降黑犯负击范继兴似余坚曲输修故城夫够送笔船占右财吃富春职觉汉画功巴跟虽杂飞检吸助升阳互初创抗考投坏策古径换未跑留钢曾端责站简述钱副尽帝射草冲承独令限阿宣环双请超微让控州良轴找否纪益依优顶础载倒房突坐粉敌略客袁冷胜绝析块剂测丝协重诉念陈仍罗盐友洋错苦夜刑移频逐靠混母短皮终聚汽村云哪既距卫停烈央察烧迅境若印洲刻括激孔搞甚室待核校散侵吧甲游久菜味旧模湖货损预阻毫普稳乙妈植息扩银语挥酒守拿序纸医熟缺雨吗针刘啊急唱误训愿审附获茶鲜粮斤孩脱硫肥善龙演父渐血欢械掌歌沙刚攻谓盾讨晚粒乱燃矛乎杀药宁鲁贵钟煤读班伯香介迫句丰培握兰担弦蛋沉假穿执答乐谁顺烟缩征脸喜松脚困异免背星福买染井概慢怕磁倍祖皇促静补评翻肉践尼衣宽扬棉希伤操垂秋宜氢套督振架亮末宪庆编牛触映雷销诗座居抓裂胞呼娘景威绿晶厚盟衡鸡孙延危胶屋乡临陆顾掉呀灯岁措束耐剧玉赵跳哥季课凯胡额款绍卷齐伟蒸殖永宗苗川炉岩弱零杨奏沿露杆探滑镇饭浓航怀赶库夺伊灵税途灭赛归召鼓播盘裁险康唯录菌纯借糖盖横符私努堂域枪润幅哈竟泽脑壤碳欧遍侧寨敢彻虑斜薄庭都纳弹饲伸折麦湿暗荷瓦塞床筑恶户访塔奇透梁刀旋迹卡氯遇份毒泥退洗摆灰彩卖耗夏择忙铜献硬予繁圈雪函亦抽篇阵阴丁尺追堆雄迎泛爸楼避谋吨野猪旗累偏典馆索秦脂潮爷豆忽托惊塑遗愈朱替纤粗倾尚痛楚谢奋购磨君池旁碎骨监捕弟暴割贯殊释词亡壁顿宝午尘闻揭炮残冬桥妇警综招吴付浮遭徐您摇谷赞箱隔订男吹园柱唐纷败宋玻巨耕坦荣闭湾键凡驻锅救恩剥凝碱齿截炼麻纺禁废盛版缓净睛昌婚涉筒嘴插岸朗庄街藏姑贸腐奴啦惯乘伙恢匀纱扎辩耳彪臣亿璃抵脉秀萨俄网舞店喷纵寸汗挂洪贺闪柬爆烯勒津稻墙软勇像滚厘蒙芳肯坡柱腿仪旅尾轧冰贡登黎削钻勒逃障氨郭峰币港伏轨亩毕擦莫刺浪秘援株健售股岛甘泡睡童铸汤阀休汇舍牧绕炸哲磷绩朋淡尖启陷柴呈徒颜泪稍忘泵蓝拖洞授镜辛壮锋贫虚弯摩泰幼廷尊窗纲弄隶疑氏宫姐震瑞怪尤琴循描膜违夹腰缘珠穷森枝竹沟催绳忆邦剩幸浆栏拥牙贮礼滤钠纹罢拍咱喊袖埃勤罚焦潜伍墨欲缝姓刊饱仿奖铝鬼丽跨默挖链扫喝袋炭污幕诸弧励梅奶洁灾舟鉴苯讼抱毁懂寒智埔寄届跃渡挑丹艰贝碰拔爹戴码梦芽熔赤渔哭敬颗奔铅仲虎稀妹乏珍申桌遵允隆螺仓魏锐晓氮兼隐碍赫拨忠肃缸牵抢博巧壳兄杜讯诚碧祥柯页巡矩悲灌龄伦票寻桂铺圣恐恰郑趣抬荒腾贴柔滴猛阔辆妻填撤储签闹扰紫砂递戏吊陶伐喂疗瓶婆抚臂摸忍虾蜡邻胸巩挤偶弃槽劲乳邓吉仁烂砖租乌舰伴瓜浅丙暂燥橡柳迷暖牌秧胆详簧踏瓷谱呆宾糊洛辉愤竞隙怒粘乃绪肩籍敏涂熙皆侦悬掘享纠醒狂锁淀恨牲霸爬赏逆玩陵祝秒浙貌役彼悉鸭趋凤晨畜辈秩卵署梯炎滩棋驱筛峡冒啥寿译浸泉帽迟硅疆贷漏稿冠嫩胁芯牢叛蚀奥鸣岭羊凭串塘绘酵融盆锡庙筹冻辅摄袭筋拒僚旱钾鸟漆沈眉疏添棒穗硝韩逼扭侨凉挺碗栽炒杯患馏劝豪辽勃鸿旦吏拜狗埋辊掩饮搬骂辞勾扣估蒋绒雾丈朵姆拟宇辑陕雕偿蓄崇剪倡厅咬驶薯刷斥番赋奉佛浇漫曼扇钙桃扶仔返俗亏腔鞋棱覆框悄叔撞骗勘旺沸孤吐孟渠屈疾妙惜仰狠胀谐抛霉桑岗嘛衰盗渗脏赖涌甜曹阅肌哩厉烃纬毅昨伪症煮叹钉搭茎笼酷偷弓锥恒杰坑鼻翼纶叙狭暮撒宿访臂析权鸟射齐凉掌立"; Dictionary<char, double> result = new Dictionary<char, double>(); for (int i = 0; i < common.Length; i++) if (!result.ContainsKey(common[i])) result[common[i]] = Math.Max(.2, 7.5 - Math.Log(2 + i)); return result; }

        private static double Score(string encoded, char[] key) { StringBuilder plain = new StringBuilder(encoded.Length); foreach (char c in encoded) plain.Append(char.ToLowerInvariant(key[c - 'A'])); return PinyinLanguageModel.Score(plain.ToString()); }
        private static void Add(List<Candidate> values, string encoded, char[] key, double score) { string signature = new string(key); foreach (Candidate item in values) if (new string(item.Key) == signature) return; StringBuilder plain = new StringBuilder(encoded.Length); foreach (char c in encoded) plain.Append(char.ToLowerInvariant(key[c - 'A'])); values.Add(new Candidate { Key = key, Score = score, Segmentation = PinyinLanguageModel.Segment(plain.ToString()) }); }
        private static string Format(List<HexCandidate> hex, List<Candidate> candidates, List<string> symbols, string encoded)
        {
            StringBuilder cipherTable = new StringBuilder(); for (int i = 0; i < symbols.Count; i++) { if (i > 0) cipherTable.Append(' '); cipherTable.Append(symbols[i]); } StringBuilder result = new StringBuilder(); int rank = 0, hexLimit = Math.Min(5, hex.Count); for (int h = 0; h < hexLimit; h++) { HexCandidate item = hex[h]; if (rank++ > 0) result.Append("\r\n\r\n"); StringBuilder table = new StringBuilder(); for (int i = 0; i < 16; i++) { if (i > 0) table.Append(' '); table.Append("0123456789ABCDEF"[item.Key[i]]); } result.Append('#').Append(rank).Append("  中文编码单表 / Unicode 十六进制  评分 ").Append(item.Score.ToString("0.00", CultureInfo.InvariantCulture)).Append("  汉字率 ").Append((item.ChineseRatio * 100).ToString("0", CultureInfo.InvariantCulture)).Append("%\r\n密文表：").Append(cipherTable).Append("\r\n十六进制表：").Append(table).Append("\r\n文本：").Append(item.Text); }
            int pinyinLimit = Math.Min(8 - rank, candidates.Count); for (int i = 0; i < pinyinLimit; i++) { Candidate item = candidates[i]; if (rank++ > 0) result.Append("\r\n\r\n"); StringBuilder plainTable = new StringBuilder(); for (int j = 0; j < symbols.Count; j++) { if (j > 0) plainTable.Append(' '); plainTable.Append(char.ToLowerInvariant(item.Key[j])); } StringBuilder raw = new StringBuilder(); foreach (char c in encoded) raw.Append(char.ToLowerInvariant(item.Key[c - 'A'])); result.Append('#').Append(rank).Append("  中文编码单表 / 拼音  评分 ").Append(item.Score.ToString("0.00", CultureInfo.InvariantCulture)).Append("  音节覆盖 ").Append((item.Segmentation.Coverage * 100).ToString("0", CultureInfo.InvariantCulture)).Append("%\r\n密文表：").Append(cipherTable).Append("\r\n拼音表：").Append(plainTable).Append("\r\n分词：").Append(item.Segmentation.Text).Append("\r\n原串：").Append(raw); } return result.ToString();
        }

        private static char[] FrequencySeed(int[] counts) { List<int> cipher = new List<int>(), plain = new List<int>(); for (int i = 0; i < 26; i++) { cipher.Add(i); plain.Add(i); } cipher.Sort(delegate(int a, int b) { return counts[b].CompareTo(counts[a]); }); double[] frequencies = PinyinLanguageModel.LetterFrequencies(); plain.Sort(delegate(int a, int b) { return frequencies[b].CompareTo(frequencies[a]); }); char[] result = new char[26]; for (int i = 0; i < 26; i++) result[cipher[i]] = (char)('A' + plain[i]); return result; }
        private static string Encode(string input, List<string> symbols) { StringBuilder result = new StringBuilder(); TextElementEnumerator scan = StringInfo.GetTextElementEnumerator((input ?? string.Empty).Normalize(NormalizationForm.FormC)); while (scan.MoveNext()) { string value = scan.GetTextElement(); if (char.IsWhiteSpace(value, 0)) continue; int index = symbols.IndexOf(value); if (index < 0) { symbols.Add(value); index = symbols.Count - 1; } if (index >= 26) throw new CipherException("中文编码单表破解最多支持 26 个不同符号"); result.Append((char)('A' + index)); } return result.ToString(); }
        private static void Swap(char[] key, int a, int b) { char value = key[a]; key[a] = key[b]; key[b] = value; }
        private static void Swap(int[] key, int a, int b) { int value = key[a]; key[a] = key[b]; key[b] = value; }
        private static void Cycle(char[] key, int a, int b, int c) { char value = key[a]; key[a] = key[b]; key[b] = key[c]; key[c] = value; }
        private static void Uncycle(char[] key, int a, int b, int c) { char value = key[c]; key[c] = key[b]; key[b] = key[a]; key[a] = value; }
    }
}
