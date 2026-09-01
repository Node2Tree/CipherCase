using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static class ToolRegistry
    {
        internal static IList<ICryptoTool> CreateAll()
        {
            List<ICryptoTool> tools = new List<ICryptoTool>();
            AddGeneral(tools);
            AddEncoding(tools);
            AddCipher(tools, new CaesarCipher(), ToolCategories.Substitution, true, true);
            AddCipher(tools, new Rot13Cipher(), ToolCategories.Substitution, false, true);
            AddCipher(tools, new AtbashCipher(), ToolCategories.Substitution, false, true);
            AddVigenere(tools);
            AddAffine(tools);
            AddCrackableCipher(tools, new RotNCipher(), ToolCategories.Substitution, ClassicalAnalysis.CrackRotN);
            AddCrackableCipher(tools, new RailFenceCipher(), ToolCategories.Transposition, ClassicalAnalysis.CrackRailFence);
            AddColumnar(tools);
            AddPolybius(tools);
            AddCipher(tools, new BaconCipher(), ToolCategories.Substitution, false, true);
            AddMonoalphabetic(tools);
            AddPlayfair(tools);
            AddCrackableCipher(tools, new BeaufortCipher(), ToolCategories.Polyalphabetic, ClassicalAnalysis.CrackBeaufort);
            AddAutokey(tools);
            AddHill(tools);
            AddBifid(tools);
            AddAdfgx(tools, false);
            AddAdfgx(tools, true);
            AddExtended(tools);
            AddAdditionalClassics(tools);
            AddExpansionClassics(tools);
            AddMoreClassics(tools);
            AddEnigma(tools);
            AddAnalysis(tools);
            SortByCommonness(tools);
            return tools.AsReadOnly();
        }

        private static void AddCipher(List<ICryptoTool> tools, ICipher cipher, string category, bool keyRequired, bool crack)
        {
            AddCipher(tools, cipher, category, keyRequired, crack, false);
        }

        private static void AddCrackableCipher(List<ICryptoTool> tools, ICipher cipher, string category, Func<string, string, string> cracker)
        {
            tools.Add(new DelegateCryptoTool(cipher.Name, category,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter() },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return cracker(request.Input, request.Get("language"));
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
                }));
        }

        private static void AddCipher(List<ICryptoTool> tools, ICipher cipher, string category, bool keyRequired, bool crack, bool optionalKey)
        {
            List<ToolMode> modes = new List<ToolMode> { ToolMode.Encrypt, ToolMode.Decrypt };
            if (crack) modes.Add(ToolMode.Crack);
            List<ToolParameter> parameters = new List<ToolParameter>();
            if (cipher.RequiresKey || optionalKey)
                parameters.Add(new ToolParameter("key", cipher.KeyHint, keyRequired, ToolMode.Encrypt, ToolMode.Decrypt));
            if (crack) parameters.Add(LanguageParameter());
            tools.Add(new DelegateCryptoTool(cipher.Name, category, modes, parameters, delegate(ToolRequest request)
            {
                if (request.Mode == ToolMode.Crack && cipher is CaesarCipher) return ClassicalAnalysis.CrackCaesar(request.Input, request.Get("language"));
                return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
            }));
        }

        private static void AddAffine(List<ICryptoTool> tools)
        {
            AffineCipher cipher = new AffineCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Substitution,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[]
                {
                    new ToolParameter("a", "A，例如 5", true, ToolMode.Encrypt, ToolMode.Decrypt),
                    new ToolParameter("b", "B，例如 8", true, ToolMode.Encrypt, ToolMode.Decrypt),
                    LanguageParameter()
                },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return ClassicalAnalysis.CrackAffine(request.Input, request.Get("language"));
                    string key = request.Get("a") + "," + request.Get("b");
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, key) : cipher.Decrypt(request.Input, key);
                }));
        }

        private static void AddVigenere(List<ICryptoTool> tools)
        {
            VigenereCipher cipher = new VigenereCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Polyalphabetic,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] {
                    new ToolParameter("key", "KEY", true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(),
                    new ToolParameter("min", "最短密钥长度", false, ToolMode.Crack), new ToolParameter("max", "最长密钥长度", false, ToolMode.Crack),
                    new ToolParameter("length", "已知长度", false, ToolMode.Crack), new ToolParameter("partial", "部分密钥，例如 LE?ON", false, ToolMode.Crack),
                    new ToolParameter("crib", "已知明文片段", false, ToolMode.Crack)
                },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return ClassicalAnalysis.CrackVigenere(request.Input, request.Get("language"), request.Get("min"), request.Get("max"), request.Get("length"), request.Get("partial"), request.Get("crib"));
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
                }));
        }

        private static void AddMonoalphabetic(List<ICryptoTool> tools)
        {
            MonoalphabeticCipher cipher = new MonoalphabeticCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Substitution,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolParameterEditor.Alphabet, string.Empty, null, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(), MatchMethodParameter(ToolMode.Crack), new ToolParameter("locks", "锁定映射 X=E 或 Ж=E", false, ToolMode.Crack), new ToolParameter("iterations", "总搜索预算，默认 30000", false, ToolMode.Crack) },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return ClassicalAnalysis.CrackMonoalphabetic(request.Input, request.Get("language"), request.Get("locks"), request.Get("iterations"), request.Get("method"), request.ReportProgress, delegate { return request.IsCancellationRequested; });
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
                }));
        }

        private static void AddColumnar(List<ICryptoTool> tools)
        {
            ColumnarTranspositionCipher cipher = new ColumnarTranspositionCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Transposition,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolMode.Encrypt, ToolMode.Decrypt), new ToolParameter("min", "最短列数，默认 2", false, ToolMode.Crack), new ToolParameter("max", "最长列数，默认 8", false, ToolMode.Crack), LanguageParameter() },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return ExtendedCrackers.CrackColumnar(request);
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
                }));
        }

        private static void AddHill(List<ICryptoTool> tools)
        {
            HillCipher cipher = new HillCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Grid,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter() },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return ExtendedCrackers.CrackHill2(request);
                    return request.Mode == ToolMode.Encrypt ? cipher.Encrypt(request.Input, request.Get("key")) : cipher.Decrypt(request.Input, request.Get("key"));
                }));
        }

        private static void AddPolybius(List<ICryptoTool> tools)
        {
            PolybiusCipher cipher = new PolybiusCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Grid,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, false, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(), SearchHeuristic(), SearchIterations(), SearchRestarts() },
                delegate(ToolRequest r) { if (r.Mode == ToolMode.Crack) return AdvancedCrackers.CrackPolybius(r); return r.Mode == ToolMode.Encrypt ? cipher.Encrypt(r.Input, r.Get("key")) : cipher.Decrypt(r.Input, r.Get("key")); }));
        }

        private static void AddPlayfair(List<ICryptoTool> tools)
        {
            PlayfairCipher cipher = new PlayfairCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Grid,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(), SearchHeuristic(), SearchIterations(), SearchRestarts() },
                delegate(ToolRequest r) { if (r.Mode == ToolMode.Crack) return AdvancedCrackers.CrackPlayfair(r); return r.Mode == ToolMode.Encrypt ? cipher.Encrypt(r.Input, r.Get("key")) : cipher.Decrypt(r.Input, r.Get("key")); }));
        }

        private static void AddAutokey(List<ICryptoTool> tools)
        {
            AutokeyCipher cipher = new AutokeyCipher();
            tools.Add(new DelegateCryptoTool(cipher.Name, ToolCategories.Polyalphabetic,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[] { new ToolParameter("key", cipher.KeyHint, true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(), new ToolParameter("min", "最短初始密钥长度，默认 2", false, ToolMode.Crack), new ToolParameter("max", "最长初始密钥长度，默认 12", false, ToolMode.Crack), new ToolParameter("iterations", "坐标优化轮数，默认 8", false, ToolMode.Crack) },
                delegate(ToolRequest r) { if (r.Mode == ToolMode.Crack) return AdvancedCrackers.CrackAutokey(r); return r.Mode == ToolMode.Encrypt ? cipher.Encrypt(r.Input, r.Get("key")) : cipher.Decrypt(r.Input, r.Get("key")); }));
        }

        private static void AddExtended(List<ICryptoTool> tools)
        {
            AddCrackableCipher(tools, new PortaCipher(), ToolCategories.Polyalphabetic, ClassicalAnalysis.CrackPorta);
            AddCrackableCipher(tools, new GronsfeldCipher(), ToolCategories.Polyalphabetic, ClassicalAnalysis.CrackGronsfeld);
            RunningKeyCipher runningKey = new RunningKeyCipher();
            tools.Add(new DelegateCryptoTool(runningKey.Name,ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key",runningKey.KeyHint,true,ToolParameterEditor.LongTextFile,string.Empty,null,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("crib","完整明文或从开头对齐的片段",false,ToolMode.Crack),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackRunningKey(r);return r.Mode==ToolMode.Encrypt?runningKey.Encrypt(r.Input,r.Get("key")):runningKey.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Four-square",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key1","方阵关键词 1",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key2","方阵关键词 2",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackFourSquare(r);return FourSquareCipher.Transform(r.Input,r.Get("key1"),r.Get("key2"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Two-square",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key1","方阵关键词 1",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key2","方阵关键词 2",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackTwoSquare(r);return TwoSquareCipher.Transform(r.Input,r.Get("key1"),r.Get("key2"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Nihilist",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("square","方阵关键词（破解时可提供）",false),new ToolParameter("key","加法密钥",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("min","最短加法密钥长度，默认 2",false,ToolMode.Crack),new ToolParameter("max","最长加法密钥长度，默认 10",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackNihilist(r);return r.Mode==ToolMode.Encrypt?NihilistCipher.Encrypt(r.Input,r.Get("square"),r.Get("key")):NihilistCipher.Decrypt(r.Input,r.Get("square"),r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Bazeries",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("number","分组数字，例如 375",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key","方阵关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minnumber","最小分组数字，默认 1",false,ToolMode.Crack),new ToolParameter("maxnumber","最大分组数字，默认 999",false,ToolMode.Crack),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackBazeries(r);return BazeriesCipher.Transform(r.Input,r.Get("number"),r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Myszkowski",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","含重复字母的关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("min","最短宽度，默认 3",false,ToolMode.Crack),new ToolParameter("max","最长宽度，默认 7",false,ToolMode.Crack),SearchIterations(),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackMyszkowski(r);return r.Mode==ToolMode.Encrypt?MyszkowskiCipher.Encrypt(r.Input,r.Get("key")):MyszkowskiCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("双重列换位",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key1","关键词 1",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key2","关键词 2",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("min","最短宽度，默认 2",false,ToolMode.Crack),new ToolParameter("max","最长宽度，默认 6",false,ToolMode.Crack),SearchHeuristic(),SearchIterations(),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackDoubleColumnar(r);return r.Mode==ToolMode.Encrypt?ColumnarTranspositionCipher.EncryptText(ColumnarTranspositionCipher.EncryptText(r.Input,r.Get("key1")),r.Get("key2")):ColumnarTranspositionCipher.DecryptText(ColumnarTranspositionCipher.DecryptText(r.Input,r.Get("key2")),r.Get("key1"));}));
            tools.Add(new DelegateCryptoTool("路线换位",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("width","矩阵宽度",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ClassicalAnalysis.CrackRoute(r.Input,r.Get("language"));return r.Mode==ToolMode.Encrypt?RouteCipher.Encrypt(r.Input,r.Get("width")):RouteCipher.Decrypt(r.Input,r.Get("width"));}));
            tools.Add(new DelegateCryptoTool("Fractionated Morse",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","关键词（可选）",false,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackFractionatedMorse(r);return r.Mode==ToolMode.Encrypt?FractionatedMorseCipher.Encrypt(r.Input,r.Get("key")):FractionatedMorseCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("同音替换",ToolCategories.Substitution,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","随机化关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackHomophonic(r);return r.Mode==ToolMode.Encrypt?HomophonicCipher.Encrypt(r.Input,r.Get("key")):HomophonicCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("跨行棋盘",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","棋盘关键词（可选）",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("blanks","两个空位，例如 37",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter(),SearchHeuristic(),SearchIterations()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackCheckerboard(r);return r.Mode==ToolMode.Encrypt?StraddlingCheckerboardCipher.Encrypt(r.Input,r.Get("key"),r.Get("blanks")):StraddlingCheckerboardCipher.Decrypt(r.Input,r.Get("key"),r.Get("blanks"));}));
            tools.Add(new DelegateCryptoTool("VIC",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("common","8 个常用字母，例如 ATONESIR",true),new ToolParameter("phrase","20 字母以上记忆短语",true,ToolParameterEditor.LongTextFile,string.Empty,null),new ToolParameter("date","6 位日期，例如 139195",true),new ToolParameter("personal","个人编号，例如 6",true),new ToolParameter("indicator","5 位消息组；解密可留空",false),new ToolParameter("cut","可选消息切分位置",false)},delegate(ToolRequest r){return r.Mode==ToolMode.Encrypt?VicCipher.Encrypt(r.Input,r.Get("common"),r.Get("phrase"),r.Get("date"),r.Get("personal"),r.Get("indicator"),r.Get("cut")):VicCipher.Decrypt(r.Input,r.Get("common"),r.Get("phrase"),r.Get("date"),r.Get("personal"),r.Get("indicator"),r.Get("cut"));}));
        }

        private static void AddGeneral(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("通用破解", ToolCategories.General, new[] { ToolMode.Crack }, new[] { LanguageParameter(), new ToolParameter("effort", "强度", false, ToolParameterEditor.Choice, "标准", new[] { "快速", "标准", "深入" }, ToolMode.Crack), new ToolParameter("clue", "算法:名称 或 明文:片段；可留空", false, ToolMode.Crack) }, delegate(ToolRequest r) { return UniversalCracker.Crack(r); }));
            tools.Add(new DelegateCryptoTool("密码识别器", ToolCategories.General, new[] { ToolMode.Analyze }, new[] { new ToolParameter("clue", "线索或关键词（可选）", false), MatchMethodParameter() }, delegate(ToolRequest r) { return CipherIdentifier.Identify(r.Input, r.Get("clue"), r.Get("method")); }));
            tools.Add(new DelegateCryptoTool("Crib 工具", ToolCategories.General, new[] { ToolMode.Analyze }, new[] { new ToolParameter("crib", "已知明文片段", true), new ToolParameter("algorithm", "算法提示（可选）", false) }, delegate(ToolRequest r) { return CribAnalysis.Analyze(r.Input, r.Get("crib"), r.Get("algorithm")); }));
        }

        private delegate string Codec(string input, bool decode);
        private static void AddCodec(List<ICryptoTool> tools, string name, Codec codec)
        {
            tools.Add(new DelegateCryptoTool(name, ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return codec(r.Input, r.Mode == ToolMode.Decode); }));
        }

        private static void AddMoreClassics(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("Keyword Cipher", ToolCategories.Substitution, new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack }, new[] { new ToolParameter("key", "关键词", true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter(), MatchMethodParameter(ToolMode.Crack), new ToolParameter("locks", "锁定映射 X=E", false, ToolMode.Crack), SearchIterations() }, delegate(ToolRequest r) { if (r.Mode == ToolMode.Crack) return ClassicalAnalysis.CrackMonoalphabetic(r.Input, r.Get("language"), r.Get("locks"), r.Get("iterations"), r.Get("method"), r.ReportProgress, delegate { return r.IsCancellationRequested; }); return KeywordCipher.Transform(r.Input, r.Get("key"), r.Mode == ToolMode.Decrypt); }));
            tools.Add(new DelegateCryptoTool("Multiplicative", ToolCategories.Substitution, new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack }, new[] { new ToolParameter("key", "与 26 互质的乘数", true, ToolMode.Encrypt, ToolMode.Decrypt), LanguageParameter() }, delegate(ToolRequest r) { return r.Mode == ToolMode.Crack ? MultiplicativeCipher.Crack(r.Input, r.Get("language")) : MultiplicativeCipher.Transform(r.Input, r.Get("key"), r.Mode == ToolMode.Decrypt); }));
            tools.Add(new DelegateCryptoTool("Reverse", ToolCategories.Transposition, new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack }, new ToolParameter[0], delegate(ToolRequest r) { return r.Mode == ToolMode.Crack ? ReverseCipher.Crack(r.Input) : ReverseCipher.Transform(r.Input); }));
            tools.Add(new DelegateCryptoTool("Vatsyayana", ToolCategories.Substitution, new[] { ToolMode.Encrypt, ToolMode.Decrypt }, new[] { new ToolParameter("key", "配对字母表关键词（可选）", false) }, delegate(ToolRequest r) { return VatsyayanaCipher.Transform(r.Input, r.Get("key")); }));
            tools.Add(new DelegateCryptoTool("Hill 3×3", ToolCategories.Grid, new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack }, new[] { new ToolParameter("key", "9 个矩阵整数", true, ToolMode.Encrypt, ToolMode.Decrypt), new ToolParameter("crib", "从密文开头对齐的已知明文，至少 9 字母", true, ToolMode.Crack) }, delegate(ToolRequest r) { return r.Mode == ToolMode.Crack ? Hill3Cipher.CrackKnownPlaintext(r.Input, r.Get("crib")) : Hill3Cipher.Transform(r.Input, r.Get("key"), r.Mode == ToolMode.Decrypt); }));
        }

        private static void AddEncoding(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("自动解码", ToolCategories.Encoding, new[] { ToolMode.Crack }, new ToolParameter[0], delegate(ToolRequest r) { return AutoDecoder.Decode(r.Input); }));
            AddCodec(tools, "Base64", TransferEncoding.Base64);
            AddCodec(tools, "十六进制", TransferEncoding.Hex);
            AddCodec(tools, "URL 编码", TransferEncoding.Url);
            AddCodec(tools, "Unicode 转义", TransferEncoding.UnicodeEscape);
            AddCodec(tools, "HTML 实体", TransferEncoding.Html);
            AddCodec(tools, "Base32", TransferEncoding.Base32);
            AddCodec(tools, "Base64URL", TransferEncoding.Base64Url);
            AddCodec(tools, "二进制", TransferEncoding.Binary);
            AddCodec(tools, "Base58", TransferEncoding.Base58);
            AddCodec(tools, "ASCII85", TransferEncoding.Ascii85);
            AddCodec(tools, "Quoted-Printable", TransferEncoding.QuotedPrintable);
            AddCodec(tools, "Punycode", TransferEncoding.Punycode);
            tools.Add(new DelegateCryptoTool("字符集字节", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("charset", "字符集", false, ToolParameterEditor.Choice, "UTF-8", TransferEncoding.CharsetChoices) }, delegate(ToolRequest r) { return TransferEncoding.CharsetBytes(r.Input, r.Get("charset"), r.Mode == ToolMode.Decode); }));
            AddCodec(tools, "盲文（英语一级）", BrailleCode.Transform);
            AddCodec(tools, "博多码 ITA2", BaudotCode.Transform);
            AddCodec(tools, "中文电报码", ChineseTelegraphCode.Transform);
            AddCodec(tools, "北约音标字母", NatoPhonetic.Transform);
            AddCodec(tools, "猪圈密码符号", SymbolCodes.Pigpen);
            AddCodec(tools, "旗语", SymbolCodes.FlagSemaphore);
            tools.Add(new DelegateCryptoTool("条形码", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("type", "类型", false, ToolParameterEditor.Choice, "CODE39", new[] { "CODE39", "EAN13" }) }, delegate(ToolRequest r) { return BarcodeCode.Transform(r.Input, r.Get("type"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("QR Code", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return QrCodeV1.Transform(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("颜色编码", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return ColorEncoding.Text(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("取色器与调色盘", ToolCategories.Encoding, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ColorEncoding.Palette(r.Input); }));
        }

        private static void AddAdditionalClassics(List<ICryptoTool> tools)
        {
            AddCrackableCipher(tools, new VariantBeaufortCipher(), ToolCategories.Polyalphabetic, ClassicalAnalysis.CrackVariantBeaufort);
            AddCipher(tools, new TrithemiusCipher(), ToolCategories.Polyalphabetic, false, false);
            tools.Add(new DelegateCryptoTool("渐进凯撒",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","起始位移，默认 0",false,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ClassicalAnalysis.CrackProgressiveCaesar(r.Input,r.Get("language"));ProgressiveCaesarCipher c=new ProgressiveCaesarCipher();return r.Mode==ToolMode.Encrypt?c.Encrypt(r.Input,r.Get("key")):c.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Scytale",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","列数，例如 5",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ClassicalAnalysis.CrackScytale(r.Input,r.Get("language"));ScytaleCipher c=new ScytaleCipher();return r.Mode==ToolMode.Encrypt?c.Encrypt(r.Input,r.Get("key")):c.Decrypt(r.Input,r.Get("key"));}));
            AddCrackableCipher(tools, new CaesarBoxCipher(), ToolCategories.Transposition, ClassicalAnalysis.CrackScytale);
            tools.Add(new DelegateCryptoTool("Redefence",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("rails","栏数",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("offset","起点偏移，默认 0",false,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ClassicalAnalysis.CrackRedefence(r.Input,r.Get("language"));return r.Mode==ToolMode.Encrypt?RedefenceCipher.Encrypt(r.Input,r.Get("rails"),r.Get("offset")):RedefenceCipher.Decrypt(r.Input,r.Get("rails"),r.Get("offset"));}));
            tools.Add(new DelegateCryptoTool("AMSCO",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","列关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("min","最短宽度，默认 2",false,ToolMode.Crack),new ToolParameter("max","最长宽度，默认 8",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackAmsco(r);return r.Mode==ToolMode.Encrypt?AmscoCipher.Encrypt(r.Input,r.Get("key")):AmscoCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Turning Grille",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("size","边长 4 或 6",true),new ToolParameter("holes","孔位，例如 1,2,3,6",true,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackTurningGrille(r);return r.Mode==ToolMode.Encrypt?TurningGrilleCipher.Encrypt(r.Input,r.Get("size"),r.Get("holes")):TurningGrilleCipher.Decrypt(r.Input,r.Get("size"),r.Get("holes"));}));
            tools.Add(new DelegateCryptoTool("A1Z26",ToolCategories.Encoding,new[]{ToolMode.Encode,ToolMode.Decode,ToolMode.Crack},new ToolParameter[0],delegate(ToolRequest r){return r.Mode==ToolMode.Encode?A1Z26Cipher.Encrypt(r.Input):A1Z26Cipher.Decrypt(r.Input);}));
            tools.Add(new DelegateCryptoTool("Tap Code",ToolCategories.Encoding,new[]{ToolMode.Encode,ToolMode.Decode,ToolMode.Crack},new ToolParameter[0],delegate(ToolRequest r){return r.Mode==ToolMode.Encode?TapCodeCipher.Encrypt(r.Input):TapCodeCipher.Decrypt(r.Input);}));
            tools.Add(new DelegateCryptoTool("Morse",ToolCategories.Encoding,new[]{ToolMode.Encode,ToolMode.Decode,ToolMode.Crack},new ToolParameter[0],delegate(ToolRequest r){return r.Mode==ToolMode.Encode?MorseCipher.Encrypt(r.Input):MorseCipher.Decrypt(r.Input);}));
            tools.Add(new DelegateCryptoTool("Morbit",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","关键词（可选）",false,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExtendedCrackers.CrackMorbit(r);return r.Mode==ToolMode.Encrypt?MorbitCipher.Encrypt(r.Input,r.Get("key")):MorbitCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Pollux",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","随机种子（可选）",false,ToolMode.Encrypt,ToolMode.Decrypt),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExtendedCrackers.CrackPollux(r);return r.Mode==ToolMode.Encrypt?PolluxCipher.Encrypt(r.Input,r.Get("key")):PolluxCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Trifid",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","方阵关键词（可选）",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("period","周期，默认 5",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minperiod","最短周期，默认 2",false,ToolMode.Crack),new ToolParameter("maxperiod","最长周期，默认 12",false,ToolMode.Crack),LanguageParameter(),SearchHeuristic(),SearchIterations(),SearchRestarts()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackTrifid(r);return r.Mode==ToolMode.Encrypt?TrifidCipher.Encrypt(r.Input,r.Get("key"),r.Get("period")):TrifidCipher.Decrypt(r.Input,r.Get("key"),r.Get("period"));}));
            tools.Add(new DelegateCryptoTool("Alberti",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","混合字母表关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("period","转盘周期，默认 5",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minperiod","最短周期，默认 1",false,ToolMode.Crack),new ToolParameter("maxperiod","最长周期，默认 20",false,ToolMode.Crack),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackAlberti(r);return AlbertiCipher.Transform(r.Input,r.Get("key"),r.Get("period"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Bellaso",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","移位关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("alphabet","已知混合字母表关键词",false),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackBellaso(r);return BellasoCipher.Transform(r.Input,r.Get("key"),r.Get("alphabet"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Ragbaby",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","混合字母表关键词",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("first","首词位移，默认 1",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("step","逐字增量，默认 1",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minfirst","最小首词位移",false,ToolMode.Crack),new ToolParameter("maxfirst","最大首词位移",false,ToolMode.Crack),new ToolParameter("minstep","最小逐字增量",false,ToolMode.Crack),new ToolParameter("maxstep","最大逐字增量",false,ToolMode.Crack),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackRagbaby(r);return RagbabyCipher.Transform(r.Input,r.Get("key"),r.Get("first"),r.Get("step"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Jefferson Wheel",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("seed","转轮组编号，默认 1776",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("offset","行偏移，默认 3",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minseed","最小转轮组编号，默认 1700",false,ToolMode.Crack),new ToolParameter("maxseed","最大转轮组编号，默认 1850",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackJefferson(r);return JeffersonWheelCipher.Transform(r.Input,r.Get("seed"),r.Get("offset"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Three-square",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key1","方阵关键词 1",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key2","方阵关键词 2",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackThreeSquare(r);return r.Mode==ToolMode.Encrypt?ThreeSquareCipher.Encrypt(r.Input,r.Get("key1"),r.Get("key2")):ThreeSquareCipher.Decrypt(r.Input,r.Get("key1"),r.Get("key2"));}));
            tools.Add(new DelegateCryptoTool("Digrafid",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key1","字母表关键词 1",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("key2","字母表关键词 2",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("period","周期，默认 5",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("minperiod","最短周期，默认 2",false,ToolMode.Crack),new ToolParameter("maxperiod","最长周期，默认 12",false,ToolMode.Crack),new ToolParameter("wordlimit","候选关键词数量",false,ToolMode.Crack),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return ExpansionCrackers.CrackDigrafid(r);return r.Mode==ToolMode.Encrypt?DigrafidCipher.Encrypt(r.Input,r.Get("key1"),r.Get("key2"),r.Get("period")):DigrafidCipher.Decrypt(r.Input,r.Get("key1"),r.Get("key2"),r.Get("period"));}));
            tools.Add(new DelegateCryptoTool("Grandpré",ToolCategories.Substitution,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","字母表关键词（可选）",false)},delegate(ToolRequest r){return r.Mode==ToolMode.Encrypt?GrandpreCipher.Encrypt(r.Input,r.Get("key")):GrandpreCipher.Decrypt(r.Input,r.Get("key"));}));
            tools.Add(new DelegateCryptoTool("Nomenclator",ToolCategories.Substitution,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("map","命名码 KING=42;ARMY=731",true,ToolParameterEditor.LongTextFile,string.Empty,null)},delegate(ToolRequest r){return r.Mode==ToolMode.Encrypt?NomenclatorCipher.Encrypt(r.Input,r.Get("map")):NomenclatorCipher.Decrypt(r.Input,r.Get("map"));}));
            tools.Add(new DelegateCryptoTool("Book Cipher",ToolCategories.Substitution,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("book","书本密钥文本",true,ToolParameterEditor.LongTextFile,string.Empty,null)},delegate(ToolRequest r){return r.Mode==ToolMode.Encrypt?BookCipher.Encrypt(r.Input,r.Get("book")):BookCipher.Decrypt(r.Input,r.Get("book"));}));
            tools.Add(new DelegateCryptoTool("Ubchi",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("key","换位关键词",true,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("nulls","加密填充字母 / 解密填充数量",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("min","最短宽度，默认 2",false,ToolMode.Crack),new ToolParameter("max","最长宽度，默认 8",false,ToolMode.Crack),new ToolParameter("nullmax","最多尝试空字母，默认 3",false,ToolMode.Crack),SearchIterations(),LanguageParameter()},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return AdvancedCrackers.CrackUbchi(r);return r.Mode==ToolMode.Encrypt?UbchiCipher.Encrypt(r.Input,r.Get("key"),r.Get("nulls")):UbchiCipher.Decrypt(r.Input,r.Get("key"),r.Get("nulls"));}));
        }

        private static void AddExpansionClassics(List<ICryptoTool> tools)
        {
            for (int variant = 1; variant <= 4; variant++) { int selected = variant; string name = "Quagmire " + ToRoman(variant); tools.Add(new DelegateCryptoTool(name,ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key1","主字母表关键词",true),new ToolParameter("key2","第二字母表关键词（IV 使用）",selected==4),new ToolParameter("indicator","指示词",true)},delegate(ToolRequest r){return QuagmireCipher.Transform(r.Input,selected.ToString(),r.Get("key1"),r.Get("key2"),r.Get("indicator"),r.Mode==ToolMode.Decrypt);})); }
            tools.Add(new DelegateCryptoTool("Gromark",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","混合字母表关键词",false),new ToolParameter("primer","数字引子，例如 31415",true)},delegate(ToolRequest r){return GromarkCipher.Transform(r.Input,r.Get("key"),r.Get("primer"),string.Empty,false,r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Periodic Gromark",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","混合字母表关键词",false),new ToolParameter("primer","数字引子，例如 31415",true),new ToolParameter("period","重置周期，默认 10",false)},delegate(ToolRequest r){return GromarkCipher.Transform(r.Input,r.Get("key"),r.Get("primer"),r.Get("period"),true,r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Chaocipher",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("left","左字母表关键词",true),new ToolParameter("right","右字母表关键词",true)},delegate(ToolRequest r){return ChaocipherCipher.Transform(r.Input,r.Get("left"),r.Get("right"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Solitaire",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","牌组口令（可选）",false)},delegate(ToolRequest r){return SolitaireCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Phillips",ToolCategories.Grid,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","方阵关键词",false)},delegate(ToolRequest r){return PhillipsCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Swagman",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","换位关键词",true)},delegate(ToolRequest r){return SwagmanCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Cadenus",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","换位关键词",true)},delegate(ToolRequest r){return CadenusCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("Nicodemus",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","关键词",true)},delegate(ToolRequest r){return NicodemusCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
            tools.Add(new DelegateCryptoTool("扰乱式换位",ToolCategories.Transposition,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key","换位关键词",true)},delegate(ToolRequest r){return DisruptedTranspositionCipher.Transform(r.Input,r.Get("key"),r.Mode==ToolMode.Decrypt);}));
        }

        private static void AddEnigma(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("Enigma",ToolCategories.Polyalphabetic,new[]{ToolMode.Encrypt,ToolMode.Decrypt,ToolMode.Crack},new[]{new ToolParameter("model","型号",false,ToolParameterEditor.Choice,"M3",new[]{"I","M3","M4"}),new ToolParameter("rotors","转子：I II III；M4 例 Beta I II III",false),new ToolParameter("rings","环位：1 1 1",false),new ToolParameter("positions","初始位置：AAA",false,ToolMode.Encrypt,ToolMode.Decrypt),new ToolParameter("reflector","反射器",false,ToolParameterEditor.Choice,"B",new[]{"A","B","C","B-Thin","C-Thin"}),new ToolParameter("plugboard","插线板，例如 AV BS CG",false),new ToolParameter("crib","已知明文片段",true,ToolMode.Crack),new ToolParameter("rotorsearch","搜索转子顺序",false,ToolParameterEditor.Choice,"false",new[]{"false","true"},ToolMode.Crack)},delegate(ToolRequest r){if(r.Mode==ToolMode.Crack)return EnigmaCracker.Crack(r);return EnigmaCipher.Transform(r.Input,r.Get("model"),r.Get("rotors"),r.Get("rings"),r.Get("positions"),r.Get("reflector"),r.Get("plugboard"));}));
        }

        private static string ToRoman(int value) { return value == 1 ? "I" : value == 2 ? "II" : value == 3 ? "III" : "IV"; }

        private delegate string TwoKeyTransform(string text,string key1,string key2,bool decrypt);
        private static ICryptoTool TwoKeyTool(string name,string category,string hint1,string hint2,TwoKeyTransform transform)
        {
            return new DelegateCryptoTool(name,category,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key1",hint1,true),new ToolParameter("key2",hint2,true)},delegate(ToolRequest r){return transform(r.Input,r.Get("key1"),r.Get("key2"),r.Mode==ToolMode.Decrypt);});
        }

        private static void AddBifid(List<ICryptoTool> tools)
        {
            tools.Add(new DelegateCryptoTool("Bifid", ToolCategories.Grid,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[]
                {
                    new ToolParameter("key", "方阵关键词（可选）", false, ToolMode.Encrypt, ToolMode.Decrypt),
                    new ToolParameter("period", "周期，例如 5", true, ToolMode.Encrypt, ToolMode.Decrypt),
                    new ToolParameter("minperiod", "最短周期，默认 2", false, ToolMode.Crack),
                    new ToolParameter("maxperiod", "最长周期，默认 12", false, ToolMode.Crack), LanguageParameter(), SearchHeuristic(), SearchIterations(), SearchRestarts()
                },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return AdvancedCrackers.CrackBifid(request);
                    return request.Mode == ToolMode.Encrypt
                        ? BifidCipher.Encrypt(request.Input, request.Get("key"), request.Get("period"))
                        : BifidCipher.Decrypt(request.Input, request.Get("key"), request.Get("period"));
                }));
        }

        private static void AddAdfgx(List<ICryptoTool> tools, bool withDigits)
        {
            tools.Add(new DelegateCryptoTool(withDigits ? "ADFGVX" : "ADFGX", ToolCategories.Grid,
                new[] { ToolMode.Encrypt, ToolMode.Decrypt, ToolMode.Crack },
                new[]
                {
                    new ToolParameter("square", "方阵关键词（破解时可提供）", false),
                    new ToolParameter("column", "换位关键词", true, ToolMode.Encrypt, ToolMode.Decrypt),
                    new ToolParameter("min", "最短列数，默认 2", false, ToolMode.Crack),
                    new ToolParameter("max", "最长列数，默认 8", false, ToolMode.Crack), LanguageParameter(), SearchHeuristic(), SearchIterations()
                },
                delegate(ToolRequest request)
                {
                    if (request.Mode == ToolMode.Crack) return AdvancedCrackers.CrackAdfgx(request, withDigits);
                    return request.Mode == ToolMode.Encrypt
                        ? AdfgxCipher.Encrypt(request.Input, request.Get("square"), request.Get("column"), withDigits)
                        : AdfgxCipher.Decrypt(request.Input, request.Get("square"), request.Get("column"), withDigits);
                }));
        }

        private static void AddAnalysis(List<ICryptoTool> tools)
        {
            tools.Add(AnalysisTool("分析工作台", new[] { LanguageParameter(ToolMode.Analyze), MatchMethodParameter(), new ToolParameter("n", "N-gram 阶数，默认 3", false) }, delegate(ToolRequest request) { return AnalysisWorkbench.Analyze(request.Input, request.Get("language"), request.Get("n"), request.Get("method")); }));
            tools.Add(AnalysisTool("频率", new ToolParameter[0], delegate(ToolRequest request) { return ClassicalAnalysis.Frequency(request.Input); }));
            tools.Add(AnalysisTool("N-gram", new[] { new ToolParameter("n", "N，例如 2", true) }, delegate(ToolRequest request) { return ClassicalAnalysis.Ngrams(request.Input, request.Get("n")); }));
            tools.Add(AnalysisTool("重合指数", new ToolParameter[0], delegate(ToolRequest request) { return ClassicalAnalysis.IndexOfCoincidence(request.Input); }));
            tools.Add(AnalysisTool("Kasiski", new[] { new ToolParameter("length", "序列长度，例如 3", true) }, delegate(ToolRequest request) { return ClassicalAnalysis.Kasiski(request.Input, request.Get("length")); }));
        }

        private static ICryptoTool AnalysisTool(string name, IEnumerable<ToolParameter> parameters, Func<ToolRequest, string> executor)
        {
            return new DelegateCryptoTool(name, ToolCategories.General, new[] { ToolMode.Analyze }, parameters, executor);
        }

        private static ToolParameter LanguageParameter()
        {
            return LanguageParameter(ToolMode.Crack);
        }

        private static ToolParameter LanguageParameter(ToolMode mode)
        {
            return new ToolParameter("language", "语言", false, ToolParameterEditor.Choice, "AUTO",
                new[] { "AUTO", "ZH", "EN", "FR", "DE", "ES", "IT", "PT", "NL", "SV", "PL", "TR" }, mode);
        }

        private static ToolParameter SearchIterations()
        {
            return new ToolParameter("iterations", "搜索次数", false, ToolMode.Crack);
        }

        private static ToolParameter SearchHeuristic()
        {
            return new ToolParameter("heuristic", "搜索策略", false, ToolParameterEditor.Choice, "自动",
                new[] { "自动", "模拟退火", "爬山", "延迟接受", "再加热退火", "阈值接受", "大洪水", "记录到记录", "自适应退火" }, ToolMode.Crack);
        }

        private static ToolParameter SearchRestarts()
        {
            return new ToolParameter("restarts", "随机重启次数", false, ToolMode.Crack);
        }

        private static ToolParameter MatchMethodParameter()
        {
            return MatchMethodParameter(ToolMode.Analyze);
        }

        private static ToolParameter MatchMethodParameter(ToolMode mode)
        {
            return new ToolParameter("method", "语言匹配", false, ToolParameterEditor.Choice, "AUTO",
                new[] { "AUTO", "COSINE", "LLR", "CHI", "NGRAM" }, mode);
        }

        private static void SortByCommonness(List<ICryptoTool> tools)
        {
            tools.Sort(delegate(ICryptoTool a, ICryptoTool b)
            {
                int category = CategoryRank(a.Category).CompareTo(CategoryRank(b.Category)); if (category != 0) return category;
                int priority = Commonness(a).CompareTo(Commonness(b)); return priority != 0 ? priority : string.CompareOrdinal(a.Name, b.Name);
            });
        }

        private static int CategoryRank(string category)
        {
            if (category == ToolCategories.General) return 0; if (category == ToolCategories.Encoding) return 1; if (category == ToolCategories.Substitution) return 2; if (category == ToolCategories.Polyalphabetic) return 3; if (category == ToolCategories.Transposition) return 4; if (category == ToolCategories.Grid) return 5; return 6;
        }

        private static int Commonness(ICryptoTool tool)
        {
            string[] order;
            if (tool.Category == ToolCategories.General) order = new[] { "通用破解", "密码识别器", "分析工作台", "频率", "重合指数", "N-gram", "Kasiski", "Crib 工具" };
            else if (tool.Category == ToolCategories.Encoding) order = new[] { "自动解码", "Base64", "十六进制", "URL 编码", "Unicode 转义", "二进制", "Base32", "字符集字节", "HTML 实体", "QR Code", "Morse", "条形码", "Base64URL", "Quoted-Printable", "盲文（英语一级）", "中文电报码", "博多码 ITA2", "颜色编码", "取色器与调色盘", "Base58", "ASCII85", "Punycode", "A1Z26", "Tap Code", "北约音标字母", "旗语", "猪圈密码符号" };
            else if (tool.Category == ToolCategories.Substitution) order = new[] { "凯撒", "ROT13", "Atbash", "仿射", "ROT-N", "单表替换", "培根", "Keyword Cipher", "Multiplicative", "同音替换", "Book Cipher", "Nomenclator", "Grandpré", "Vatsyayana" };
            else if (tool.Category == ToolCategories.Polyalphabetic) order = new[] { "维吉尼亚", "Beaufort", "Autokey", "Gronsfeld", "Porta", "Running Key", "Enigma", "Variant Beaufort", "Trithemius", "渐进凯撒", "Alberti", "Bellaso", "Ragbaby", "Jefferson Wheel", "Quagmire I", "Quagmire II", "Quagmire III", "Quagmire IV", "Gromark", "Periodic Gromark", "Chaocipher", "Solitaire", "Nicodemus" };
            else if (tool.Category == ToolCategories.Transposition) order = new[] { "栅栏", "列换位", "路线换位", "Scytale", "Reverse", "Redefence", "Caesar Box", "Myszkowski", "AMSCO", "双重列换位", "Turning Grille", "Ubchi", "扰乱式换位", "Swagman", "Cadenus" };
            else order = new[] { "Polybius", "Playfair", "Hill 2×2", "Hill 3×3", "ADFGX", "ADFGVX", "Bifid", "Trifid", "Four-square", "Two-square", "Nihilist", "Fractionated Morse", "跨行棋盘", "Bazeries", "Morbit", "Pollux", "Three-square", "Digrafid", "Phillips", "VIC" };
            for (int i = 0; i < order.Length; i++) if (tool.Name == order[i]) return i; return 1000;
        }
    }
}
