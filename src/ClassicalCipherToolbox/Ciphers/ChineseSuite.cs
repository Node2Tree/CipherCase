using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Ciphers
{
    internal static class ChineseCodeTables
    {
        internal static readonly string[] SchemeChoices = { "五笔86", "五笔98", "郑码", "二笔", "表形码", "行列30", "大易四码", "嘸蝦米", "笔顺五码", "小鹤音形", "自然码音形", "吴语拼音", "苏州吴语", "白话字 POJ", "台语注音", "台罗 TLPA" };
        private static readonly Dictionary<string, Dictionary<string, List<string>>> ByCharacter = new Dictionary<string, Dictionary<string, List<string>>>();
        private static readonly Dictionary<string, Dictionary<string, List<string>>> ByCode = new Dictionary<string, Dictionary<string, List<string>>>();
        private const string Common = "的一是在不了有人我他这个们中来上大为和国地到以说时要就出会可也你对生能而子那得于着下自之年过发后作里用道行所然家种事成方多经么去法学如都同现当没动面起看定天分还进好小部其些主样理心她本前开但因只从想实日军者意无力它与长把机十民第公此已工使情明性知全三又关点正业外将两高间由问很最重并物手应战向头文体政美相见被利什二等产或新己制身果加西斯月话合回特代内信表化老给世位次度门任常先海通教儿原东声提立及比员解水名真论处走义各入几口认条平系气题活尔更别打女变四神总何电数安少报才结反受目太量";
        private static bool loaded;

        internal static bool IsScheme(string scheme) { return Array.IndexOf(SchemeChoices, scheme) >= 0; }
        internal static IList<string> CodesFor(string character, string scheme)
        {
            Load(); Dictionary<string, List<string>> map; List<string> values;
            return ByCharacter.TryGetValue(scheme ?? string.Empty, out map) && map.TryGetValue(character ?? string.Empty, out values) ? values : new List<string>();
        }
        internal static string Transform(string input, string scheme, bool reverse)
        {
            Load(); if (!IsScheme(scheme)) throw new CipherException("请选择输入法方案");
            if (!reverse) { List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) { IList<string> values = CodesFor(unit, scheme); result.Add(values.Count > 0 ? string.Join("/", new List<string>(values).ToArray()) : "[" + unit + "]"); } return string.Join(" ", result.ToArray()); }
            Dictionary<string, List<string>> index = ByCode[scheme]; string[] tokens = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，', ';', '；', '/', '、' }, StringSplitOptions.RemoveEmptyEntries); if (tokens.Length == 0) throw new CipherException("请输入一个或多个输入码");
            StringBuilder output = new StringBuilder(); foreach (string raw in tokens) { if (output.Length > 0) output.Append("\r\n"); output.Append(raw).Append(" → "); List<string> matches = Lookup(index, raw); if (matches.Count == 0) output.Append("未收录"); else { int count = Math.Min(128, matches.Count); for (int i = 0; i < count; i++) output.Append(matches[i]); if (matches.Count > count) output.Append(" …（共 ").Append(matches.Count).Append(" 字）"); } } return output.ToString();
        }
        internal static string Statistics(string scheme)
        {
            Load(); Dictionary<string, List<string>> chars = ByCharacter.ContainsKey(scheme) ? ByCharacter[scheme] : new Dictionary<string, List<string>>(); Dictionary<string, List<string>> codes = ByCode.ContainsKey(scheme) ? ByCode[scheme] : new Dictionary<string, List<string>>(); int ambiguous = 0, maximum = 0; foreach (List<string> values in codes.Values) { if (values.Count > 1) ambiguous++; maximum = Math.Max(maximum, values.Count); } return scheme + "\r\n字符：" + chars.Count + "\r\n码数：" + codes.Count + "\r\n重码：" + ambiguous + "\r\n最大候选：" + maximum;
        }
        internal static int BestMatch(string input, out string scheme)
        {
            Load(); string[] tokens = (input ?? string.Empty).Split(new[] { ' ', '\t', '\r', '\n', ',', '，' }, StringSplitOptions.RemoveEmptyEntries); int best = 0; scheme = string.Empty; if (tokens.Length < 2 || tokens.Length > 64) return 0; foreach (string name in SchemeChoices) { int hits = 0; Dictionary<string, List<string>> index = ByCode[name]; foreach (string raw in tokens) { bool found = false; foreach (string option in raw.Split('/')) if (index.ContainsKey(option.ToLowerInvariant())) { found = true; break; } if (found) hits++; } if (hits > best) { best = hits; scheme = name; } } return best;
        }
        private static List<string> Lookup(Dictionary<string, List<string>> index, string query)
        {
            string key = (query ?? string.Empty).Trim().ToLowerInvariant(); List<string> exact; if (index.TryGetValue(key, out exact)) return new List<string>(exact); List<string> result = new List<string>(); string pattern = "^" + Regex.Escape(key).Replace("\\*", ".*").Replace("\\?", ".") + "$"; Regex regex = new Regex(pattern, RegexOptions.IgnoreCase); foreach (KeyValuePair<string, List<string>> pair in index) if (regex.IsMatch(pair.Key)) foreach (string value in pair.Value) if (!result.Contains(value)) result.Add(value); return result;
        }
        private static void Load()
        {
            if (loaded) return; loaded = true; foreach (string scheme in SchemeChoices) { ByCharacter[scheme] = new Dictionary<string, List<string>>(); ByCode[scheme] = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); }
            Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicalCipherToolbox.Analysis.ChineseCodeTables"); if (resource == null) throw new CipherException("中文码表未嵌入"); using (resource) using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress)) using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8)) { string line; while ((line = reader.ReadLine()) != null) { string[] fields = line.Split('\t'); if (fields.Length < 3 || !IsScheme(fields[0]) || fields[1].Length == 0 || fields[2].Length == 0) continue; Add(ByCharacter[fields[0]], fields[1], fields[2].ToLowerInvariant()); Add(ByCode[fields[0]], fields[2].ToLowerInvariant(), fields[1]); } }
            foreach (Dictionary<string, List<string>> index in ByCode.Values) foreach (List<string> values in index.Values) values.Sort(delegate(string a, string b) { int ar = Common.IndexOf(a, StringComparison.Ordinal), br = Common.IndexOf(b, StringComparison.Ordinal); if (ar < 0) ar = int.MaxValue; if (br < 0) br = int.MaxValue; if (ar != br) return ar.CompareTo(br); return string.CompareOrdinal(a, b); });
        }
        private static void Add(Dictionary<string, List<string>> map, string key, string value) { List<string> values; if (!map.TryGetValue(key, out values)) { values = new List<string>(); map[key] = values; } if (!values.Contains(value)) values.Add(value); }
    }

    internal static class ChineseIds
    {
        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(); private static bool loaded;
        internal static string Lookup(string character) { Load(); string value; return Values.TryGetValue(character ?? string.Empty, out value) ? value : string.Empty; }
        private static void Load() { if (loaded) return; loaded = true; Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClassicalCipherToolbox.Analysis.ChineseIds"); if (resource == null) return; using (resource) using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress)) using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8)) { string line; while ((line = reader.ReadLine()) != null) { int at = line.IndexOf('\t'); if (at > 0) Values[line.Substring(0, at)] = line.Substring(at + 1); } } }
    }

    internal static class ChineseRomanization
    {
        internal static readonly string[] TargetChoices = { "汉语拼音", "数字声调", "声调符号", "注音", "威妥玛", "国语罗马字", "通用拼音", "耶鲁拼音", "粤拼", "粤语耶鲁", "教院式粤拼", "吴语拼音", "苏州吴语", "白话字 POJ", "台罗 TLPA", "IPA（普通话）" };
        internal static readonly string[] PinyinFormatChoices = { "无声调拼音", "数字声调", "声调符号", "注音" };
        private static readonly string[] DoubleSchemes = { "自然码双拼", "智能ABC双拼", "小鹤双拼", "微软双拼", "拼音加加双拼", "四通双拼" };
        private static readonly Dictionary<string, string> NaturalFinals = Map("a=a,o=o,e=e,i=i,u=u,v=v,ai=l,ei=z,ao=k,ou=b,an=j,en=f,ang=h,eng=g,ong=s,ia=w,ie=x,iao=c,iu=q,ian=m,in=n,iang=d,ing=y,iong=s,ua=w,uo=o,uai=y,ui=v,uan=r,un=p,uang=d,ue=t,ve=t,van=r,vn=p");
        private static readonly Dictionary<string, string> FlypyFinals = Map("a=a,o=o,e=e,i=i,u=u,v=v,ai=d,ei=w,ao=c,ou=z,an=j,en=f,ang=h,eng=g,ong=s,ia=x,ie=p,iao=n,iu=q,ian=m,in=b,iang=l,ing=k,iong=s,ua=x,uo=o,uai=k,ui=v,uan=r,un=y,uang=l,ue=t,ve=t,van=r,vn=y");
        private static readonly Dictionary<string, string> AbcFinals = Map("a=a,o=o,e=e,i=i,u=u,v=v,ai=l,ei=q,ao=k,ou=b,an=j,en=f,ang=h,eng=g,ong=s,ia=d,ie=x,iao=z,iu=r,ian=w,in=c,iang=t,ing=y,iong=s,ua=d,uo=o,uai=c,ui=m,uan=p,un=n,uang=t,ue=m,ve=m,van=p,vn=n,er=r");
        private static readonly Dictionary<string, string> MspyFinals = Map("a=a,o=o,e=e,i=i,u=u,v=y,ai=l,ei=z,ao=k,ou=b,an=j,en=f,ang=h,eng=g,ong=s,ia=w,ie=x,iao=c,iu=q,ian=m,in=n,iang=d,ing=;,iong=s,ua=w,uo=o,uai=y,ui=v,uan=r,un=p,uang=d,ue=t,ve=t,van=r,vn=p,er=r");
        private static readonly Dictionary<string, string> PyjjFinals = Map("a=a,o=o,e=e,i=i,u=u,v=v,ai=s,ei=w,ao=d,ou=p,an=f,en=r,ang=g,eng=t,ong=y,ia=b,ie=m,iao=k,iu=n,ian=j,in=l,iang=h,ing=q,iong=y,ua=b,uo=o,uai=x,ui=v,uan=c,un=z,uang=h,ue=x,ve=x,van=c,vn=z,er=q");
        private static readonly Dictionary<string, string> StFinals = Map("a=a,o=o,e=e,i=i,u=u,v=x,ai=s,ei=w,ao=d,ou=p,an=f,en=r,ang=g,eng=t,ong=y,ia=b,ie=m,iao=k,iu=n,ian=j,in=l,iang=h,ing=;,iong=y,ua=b,uo=o,uai=x,ui=v,uan=c,un=z,uang=h,ue=v,ve=v,van=c,vn=z,er=q");
        internal static bool IsDoublePinyin(string scheme) { return Array.IndexOf(DoubleSchemes, scheme) >= 0; }
        internal static string DoublePinyin(string pinyin, string scheme)
        {
            string value = (pinyin ?? string.Empty).ToLowerInvariant(); if (value.Length == 0) return value; string initial = string.Empty, final = value; string[] initials = { "zh", "ch", "sh", "b", "p", "m", "f", "d", "t", "n", "l", "g", "k", "h", "j", "q", "x", "r", "z", "c", "s" }; foreach (string item in initials) if (value.StartsWith(item)) { initial = item; final = value.Substring(item.Length); break; }
            if (initial.Length == 0) { if (value.Length == 1) return value + value; initial = value.Substring(0, 1); final = value.Substring(1); }
            string first = initial == "zh" ? (scheme == "智能ABC双拼" || scheme == "四通双拼" ? "a" : "v") : initial == "ch" ? (scheme == "智能ABC双拼" ? "e" : scheme == "拼音加加双拼" || scheme == "四通双拼" ? "u" : "i") : initial == "sh" ? (scheme == "拼音加加双拼" || scheme == "四通双拼" ? "i" : scheme == "智能ABC双拼" ? "v" : "u") : initial;
            Dictionary<string, string> finals = scheme == "小鹤双拼" ? FlypyFinals : scheme == "智能ABC双拼" ? AbcFinals : scheme == "微软双拼" ? MspyFinals : scheme == "拼音加加双拼" ? PyjjFinals : scheme == "四通双拼" ? StFinals : NaturalFinals; string second; if (!finals.TryGetValue(final, out second)) second = final.Length > 0 ? final.Substring(0, 1) : first; return first + second;
        }
        internal static string Transform(string input, string target)
        {
            string selected = string.IsNullOrWhiteSpace(target) ? TargetChoices[0] : target; List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) { IList<string> values;
                string table = selected == "吴语拼音" || selected == "苏州吴语" || selected == "白话字 POJ" || selected == "台罗 TLPA" ? selected : selected == "粤语耶鲁" || selected == "教院式粤拼" ? "粤拼" : string.Empty;
                if (table.Length > 0) values = ChineseInputCode.CodesFor(unit, table); else if (selected == "粤拼") values = ChineseInputCode.CodesFor(unit, "粤拼"); else if (selected == "注音") values = ChineseInputCode.CodesFor(unit, "注音"); else if (selected == "汉语拼音") values = ChineseInputCode.CodesFor(unit, "汉语拼音"); else if (selected == "数字声调") values = ChineseInputCode.CodesFor(unit, "汉语拼音（数字声调）"); else if (selected == "声调符号") values = ChineseInputCode.CodesFor(unit, "汉语拼音（声调符号）"); else values = ChineseInputCode.CodesFor(unit, "汉语拼音（数字声调）");
                if (values.Count == 0) { result.Add("[" + unit + "]"); continue; } List<string> converted = new List<string>(); foreach (string raw in values) converted.Add(ConvertReading(raw, selected)); result.Add(string.Join("/", converted.ToArray())); }
            return string.Join(" ", result.ToArray());
        }
        internal static string FormatPinyin(string input, string target)
        {
            return Regex.Replace(input ?? string.Empty, "[A-Za-zÜüĀ-ǜ]+[1-5]?", delegate(Match match) { int tone; string plain = ChineseInputCode.NormalizePinyin(match.Value, out tone); if (target == "数字声调") return plain + (tone > 0 ? tone.ToString(CultureInfo.InvariantCulture) : string.Empty); if (target == "声调符号") return ToneMarked(plain, tone); if (target == "注音") return ChineseInputCode.ToBopomofo(plain, tone); return plain.Replace('v', 'ü'); });
        }
        private static string ToneMarked(string plain, int tone)
        {
            string value = (plain ?? string.Empty).Replace('v', 'ü'); if (tone < 1 || tone > 4) return value; int at = value.IndexOf('a'); if (at < 0) at = value.IndexOf('e'); if (at < 0 && value.IndexOf("ou", StringComparison.Ordinal) >= 0) at = value.IndexOf('o'); if (at < 0) for (int i = value.Length - 1; i >= 0; i--) if ("aeiouü".IndexOf(value[i]) >= 0) { at = i; break; } if (at < 0) return value; char mark = tone == 1 ? '\u0304' : tone == 2 ? '\u0301' : tone == 3 ? '\u030C' : '\u0300'; return (value.Substring(0, at + 1) + mark + value.Substring(at + 1)).Normalize(NormalizationForm.FormC);
        }
        private static string ConvertReading(string raw, string target)
        {
            if (target == "粤语耶鲁") return ReplaceInitial(raw, new[] { "gw=gw", "kw=kw", "ng=ng", "z=j", "c=ch", "j=y" });
            if (target == "威妥玛") return ReplaceInitial(raw, new[] { "zh=ch", "ch=ch'", "sh=sh", "q=ch'", "x=hs", "j=ch", "z=ts", "c=ts'" });
            if (target == "耶鲁拼音") return ReplaceInitial(raw, new[] { "zh=jr", "ch=chr", "sh=shr", "q=chy", "x=sy", "j=jy" });
            if (target == "通用拼音") return ReplaceInitial(raw, new[] { "zh=jh", "q=cy", "x=sy" });
            if (target == "国语罗马字") return raw.TrimEnd('1', '2', '3', '4', '5');
            if (target == "IPA（普通话）") return ReplaceInitial(raw.TrimEnd('1', '2', '3', '4', '5'), new[] { "zh=ʈʂ", "ch=ʈʂʰ", "sh=ʂ", "r=ʐ", "j=tɕ", "q=tɕʰ", "x=ɕ", "c=tsʰ", "z=ts" });
            return raw;
        }
        private static string ReplaceInitial(string input, string[] rules) { foreach (string rule in rules) { string[] pair = rule.Split('='); if (input.StartsWith(pair[0], StringComparison.OrdinalIgnoreCase)) return pair[1] + input.Substring(pair[0].Length); } return input; }
        private static Dictionary<string, string> Map(string source) { Dictionary<string, string> result = new Dictionary<string, string>(); foreach (string item in source.Split(',')) { string[] pair = item.Split('='); result[pair[0]] = pair[1]; } return result; }
    }

    internal static class ChineseWorkbench
    {
        private static readonly string[] Charsets = { "UTF-8", "UTF-16LE", "UTF-16BE", "UTF-32LE", "GB2312 / EUC-CN", "GBK / CP936", "GB18030", "Big5 / CP950", "HZ-GB-2312", "ISO-2022-CN / CP50227" };
        internal static string CharacterCard(string input)
        {
            IList<string> units = UnicodeAnalysis.Units(input ?? string.Empty); if (units.Count == 0) throw new CipherException("请输入一个字符"); return Card(units[0]);
        }
        internal static string Workbench(string input)
        {
            IList<string> units = UnicodeAnalysis.Units(input ?? string.Empty); if (units.Count == 0) throw new CipherException("请输入中文文本或编码"); StringBuilder output = new StringBuilder(); int count = Math.Min(64, units.Count); for (int i = 0; i < count; i++) { if (i > 0) output.Append("\r\n\r\n────────────────\r\n\r\n"); output.Append(Card(units[i])); } if (units.Count > count) output.Append("\r\n\r\n仅展示前 64 个字符；原文共 ").Append(units.Count).Append(" 个 Unicode 字符。"); return output.ToString();
        }
        private static string Card(string unit)
        {
            int codepoint = char.ConvertToUtf32(unit, 0); StringBuilder output = new StringBuilder(); output.Append(unit).Append("  U+").Append(codepoint.ToString("X4", CultureInfo.InvariantCulture)).Append("  ").Append(UnicodeCategoryName(unit)).Append("\r\n");
            Add(output, "释义", ChineseInputCode.Metadata(unit, "释义")); Add(output, "IDS", ChineseIds.Lookup(unit)); Add(output, "部首余笔", ChineseInputCode.Metadata(unit, "部首余笔")); Add(output, "康熙", ChineseInputCode.Metadata(unit, "康熙索引")); Add(output, "简体", Variants(ChineseInputCode.Metadata(unit, "简体异体"))); Add(output, "繁体", Variants(ChineseInputCode.Metadata(unit, "繁体异体")));
            output.Append("\r\n输入与语音\r\n"); foreach (string scheme in ChineseInputCode.SchemeChoices) { IList<string> values = ChineseInputCode.CodesFor(unit, scheme); if (values.Count > 0) output.Append(scheme).Append("：").Append(string.Join(" / ", new List<string>(values).ToArray())).Append("\r\n"); }
            output.Append("\r\n字符集字节\r\n"); foreach (string charset in Charsets) try { output.Append(charset).Append("：").Append(TransferEncoding.CharsetBytes(unit, charset, false)).Append("\r\n"); } catch { output.Append(charset).Append("：—\r\n"); }
            output.Append("HTML：&#x").Append(codepoint.ToString("X", CultureInfo.InvariantCulture)).Append(";\r\nJSON/JS：").Append(EscapeCodePoint(codepoint)).Append("\r\nURI：").Append(Uri.EscapeDataString(unit)); return output.ToString().TrimEnd();
        }
        internal static string CharsetComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) throw new CipherException("请输入要比较的文本"); StringBuilder output = new StringBuilder("字符集\t字节数\t十六进制\r\n"); foreach (string charset in Charsets) try { string hex = TransferEncoding.CharsetBytes(input, charset, false); output.Append(charset).Append('\t').Append(hex.Length / 2).Append('\t').Append(hex).Append("\r\n"); } catch { output.Append(charset).Append("\t—\t无法表示\r\n"); } return output.ToString().TrimEnd();
        }
        internal static string Identify(string input)
        {
            string source = (input ?? string.Empty).Trim(); if (source.Length == 0) throw new CipherException("请输入文本、字节或输入码"); StringBuilder output = new StringBuilder(); if (Regex.IsMatch(source, "^(?:[0-9A-Fa-f]{2}[\\s,:-]*)+$")) { output.Append("可能是十六进制字节。\r\n"); foreach (string charset in Charsets) try { string text = TransferEncoding.CharsetBytes(source, charset, true); output.Append(charset).Append(" → ").Append(text).Append("\r\n"); } catch { } }
            string[] tokens = source.Split(new[] { ' ', '\t', '\r', '\n', ',', '，' }, StringSplitOptions.RemoveEmptyEntries); string[] compactSchemes = { "汉语拼音", "注音", "仓颉", "四角号码" }; foreach (string scheme in compactSchemes) { int hits = ChineseInputCode.MatchCount(source, scheme); if (hits > 0) output.Append(scheme).Append("：").Append(hits).Append('/').Append(tokens.Length).Append(" 个码命中\r\n"); }
            foreach (string scheme in ChineseCodeTables.SchemeChoices) { int hits = 0; foreach (string token in tokens) try { if (!ChineseCodeTables.Transform(token, scheme, true).Contains("未收录")) hits++; } catch { } if (hits > 0) output.Append(scheme).Append("：").Append(hits).Append('/').Append(tokens.Length).Append(" 个码命中\r\n"); }
            if (output.Length == 0) output.Append("没有发现明确的中文编码特征；可在字符详情或码表工作台继续检查。"); return output.ToString().TrimEnd();
        }
        private static string UnicodeCategoryName(string unit) { UnicodeCategory c = CharUnicodeInfo.GetUnicodeCategory(unit, 0); return c.ToString(); }
        private static string EscapeCodePoint(int cp) { if (cp <= 0xFFFF) return "\\u" + cp.ToString("X4", CultureInfo.InvariantCulture); string s = char.ConvertFromUtf32(cp); return "\\u" + ((int)s[0]).ToString("X4") + "\\u" + ((int)s[1]).ToString("X4"); }
        private static string Variants(string raw) { if (string.IsNullOrEmpty(raw)) return raw; return Regex.Replace(raw, "U\\+([0-9A-Fa-f]{4,6})(?:<[^ ]+)?", delegate(Match m) { int cp; return int.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cp) ? char.ConvertFromUtf32(cp) : m.Value; }); }
        private static void Add(StringBuilder output, string name, string value) { if (!string.IsNullOrWhiteSpace(value)) output.Append(name).Append("：").Append(value).Append("\r\n"); }
    }

    internal static class ChineseCodeTableWorkbench
    {
        internal static string Query(string input, string scheme, string custom)
        {
            if (!string.IsNullOrWhiteSpace(custom)) return QueryCustom(input, custom); if (string.IsNullOrWhiteSpace(input) || input.Trim() == "统计") return ChineseCodeTables.Statistics(scheme); return ChineseCodeTables.Transform(input, scheme, LooksLikeCode(input));
        }
        private static bool LooksLikeCode(string input) { foreach (string unit in UnicodeAnalysis.Units(input ?? string.Empty)) if (unit.Length > 0 && unit[0] >= '\u3400' && unit[0] <= '\u9FFF') return false; return true; }
        private static string QueryCustom(string input, string table)
        {
            Dictionary<string, List<string>> chars = new Dictionary<string, List<string>>(), codes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); foreach (string raw in table.Replace("\r", string.Empty).Split('\n')) { string line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("---") || line.StartsWith("...")) continue; string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 2) continue; string character = parts[0], code = parts[1]; if (!ContainsHan(character) && ContainsHan(code)) { string swap = character; character = code; code = swap; } if (!ContainsHan(character)) continue; Add(chars, character, code); Add(codes, code, character); }
            if (string.IsNullOrWhiteSpace(input) || input.Trim() == "统计") return "自定义码表\r\n字符或词组：" + chars.Count + "\r\n码数：" + codes.Count;
            if (LooksLikeCode(input)) { StringBuilder output = new StringBuilder(); foreach (string token in input.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)) { List<string> values; output.Append(token).Append(" → ").Append(codes.TryGetValue(token, out values) ? string.Join(string.Empty, values.ToArray()) : "未收录").Append("\r\n"); } return output.ToString().TrimEnd(); }
            List<string> exact; if (chars.TryGetValue(input, out exact)) return string.Join("/", exact.ToArray()); List<string> result = new List<string>(); foreach (string unit in UnicodeAnalysis.Units(input)) { List<string> values; result.Add(chars.TryGetValue(unit, out values) ? string.Join("/", values.ToArray()) : "[" + unit + "]"); } return string.Join(" ", result.ToArray());
        }
        private static bool ContainsHan(string value) { foreach (char c in value ?? string.Empty) if (c >= '\u3400' && c <= '\u9FFF') return true; return false; }
        private static void Add(Dictionary<string, List<string>> map, string key, string value) { List<string> list; if (!map.TryGetValue(key, out list)) { list = new List<string>(); map[key] = list; } if (!list.Contains(value)) list.Add(value); }
    }

    internal static class UnicodeCompatibilityEncoding
    {
        internal static readonly string[] Choices = { "UTF-7", "CESU-8", "Modified UTF-8", "BOM 自动识别" };
        internal static string Transform(string input, string format, bool decode)
        {
            try { if (format == "UTF-7") return decode ? Encoding.UTF7.GetString(TransferEncoding.HexToBytes(input)) : TransferEncoding.BytesToHex(Encoding.UTF7.GetBytes(input ?? string.Empty)); if (format == "BOM 自动识别") return decode ? DecodeBom(TransferEncoding.HexToBytes(input)) : "EFBBBF" + TransferEncoding.BytesToHex(Encoding.UTF8.GetBytes(input ?? string.Empty)); return decode ? DecodeCesu(TransferEncoding.HexToBytes(input), format == "Modified UTF-8") : TransferEncoding.BytesToHex(EncodeCesu(input ?? string.Empty, format == "Modified UTF-8")); } catch { throw new CipherException("文本或字节不符合所选 Unicode 格式"); }
        }
        private static byte[] EncodeCesu(string input, bool modified) { List<byte> bytes = new List<byte>(); foreach (char c in input) { int value = c; if (modified && value == 0) { bytes.Add(0xC0); bytes.Add(0x80); } else if (value < 0x80) bytes.Add((byte)value); else if (value < 0x800) { bytes.Add((byte)(0xC0 | value >> 6)); bytes.Add((byte)(0x80 | value & 63)); } else { bytes.Add((byte)(0xE0 | value >> 12)); bytes.Add((byte)(0x80 | value >> 6 & 63)); bytes.Add((byte)(0x80 | value & 63)); } } return bytes.ToArray(); }
        private static string DecodeCesu(byte[] bytes, bool modified) { StringBuilder result = new StringBuilder(); for (int i = 0; i < bytes.Length;) { int b = bytes[i++]; if (b < 0x80) result.Append((char)b); else if ((b & 0xE0) == 0xC0 && i < bytes.Length) { int v = (b & 31) << 6 | bytes[i++] & 63; result.Append((char)v); } else if ((b & 0xF0) == 0xE0 && i + 1 < bytes.Length) { int v = (b & 15) << 12 | (bytes[i++] & 63) << 6 | bytes[i++] & 63; result.Append((char)v); } else throw new FormatException(); } return result.ToString(); }
        private static string DecodeBom(byte[] bytes) { if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3); if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2); if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2); return Encoding.UTF8.GetString(bytes); }
    }

    internal static class ChineseTransferFormats
    {
        internal static readonly string[] Choices = { "MIME encoded-word Base64", "MIME encoded-word Q", "JSON", "JavaScript", "CSS", "XML 十六进制实体", "XML 十进制实体", "URI", "IRI" };
        internal static string Transform(string input, string format, bool decode)
        {
            string value = input ?? string.Empty; if (format == "MIME encoded-word Base64") return decode ? DecodeMime(value) : "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "?="; if (format == "MIME encoded-word Q") return decode ? DecodeMime(value) : "=?UTF-8?Q?" + TransferEncoding.QuotedPrintable(value, false).Replace(" ", "_") + "?="; if (format == "URI") return decode ? Uri.UnescapeDataString(value) : Uri.EscapeDataString(value); if (format == "IRI") return decode ? Uri.UnescapeDataString(value) : EscapeIri(value); if (format == "JSON" || format == "JavaScript") return decode ? Unescape(value) : Escape(value, "\\u{0:X4}"); if (format == "CSS") return decode ? UnescapeCss(value) : Escape(value, "\\{0:X} "); if (format.StartsWith("XML")) return decode ? System.Net.WebUtility.HtmlDecode(value) : Escape(value, format.Contains("十六") ? "&#x{0:X};" : "&#{0};"); return value;
        }
        private static string Escape(string value, string format) { StringBuilder output = new StringBuilder(); foreach (char c in value) output.Append(c < 128 && c != '\\' && c != '"' ? c.ToString() : string.Format(CultureInfo.InvariantCulture, format, (int)c)); return output.ToString(); }
        private static string Unescape(string value) { return Regex.Replace(value, "\\\\u([0-9A-Fa-f]{4})", delegate(Match m) { return ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString(); }).Replace("\\\"", "\"").Replace("\\\\", "\\"); }
        private static string UnescapeCss(string value) { return Regex.Replace(value, "\\\\([0-9A-Fa-f]{1,6})\\s?", delegate(Match m) { return char.ConvertFromUtf32(int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)); }); }
        private static string EscapeIri(string value) { StringBuilder output = new StringBuilder(); foreach (char c in value) if (c < 128 && (char.IsLetterOrDigit(c) || "-._~/:?#[]@!$&'()*+,;=".IndexOf(c) >= 0)) output.Append(c); else output.Append(Uri.EscapeDataString(c.ToString())); return output.ToString(); }
        private static string DecodeMime(string value) { return Regex.Replace(value, @"=\?([^?]+)\?([BbQq])\?([^?]+)\?=", delegate(Match m) { Encoding enc = Encoding.GetEncoding(m.Groups[1].Value); byte[] bytes = m.Groups[2].Value.Equals("B", StringComparison.OrdinalIgnoreCase) ? Convert.FromBase64String(m.Groups[3].Value) : DecodeQ(m.Groups[3].Value); return enc.GetString(bytes); }); }
        private static byte[] DecodeQ(string value) { List<byte> bytes = new List<byte>(); string source = (value ?? string.Empty).Replace('_', ' '); for (int i = 0; i < source.Length; i++) { if (source[i] == '=' && i + 2 < source.Length && Uri.IsHexDigit(source[i + 1]) && Uri.IsHexDigit(source[i + 2])) { bytes.Add(byte.Parse(source.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 2; } else bytes.Add((byte)source[i]); } return bytes.ToArray(); }
    }

    internal static class ChineseHistoricalEncoding
    {
        internal static readonly string[] Choices = { "CNS 11643 / CP20000", "TCA / CP20001", "Big5 ETen / CP20002", "IBM5550 / CP20003", "TeleText / CP20004", "Wang / CP20005", "Big5-HKSCS / CP951", "EUC-TW / CP51950", "ISO-2022-CN-EXT / CP50229", "IBM EBCDIC 简体 / CP935", "IBM EBCDIC 繁体 / CP937", "IBM EBCDIC 简体 / CP1388" };
        internal static string Transform(string input, string scheme, bool decode) { int at = (scheme ?? string.Empty).LastIndexOf("CP", StringComparison.OrdinalIgnoreCase); if (at < 0) throw new CipherException("请选择历史字符集"); string digits = Regex.Match(scheme.Substring(at + 2), "^[0-9]+").Value; try { Encoding encoding = Encoding.GetEncoding(int.Parse(digits, CultureInfo.InvariantCulture), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback); return decode ? encoding.GetString(TransferEncoding.HexToBytes(input)) : TransferEncoding.BytesToHex(encoding.GetBytes(input ?? string.Empty)); } catch { throw new CipherException("当前 Windows 未安装该历史代码页，或文本无法表示"); } }
    }
}
