using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Tests
{
    internal static class CipherTests
    {
        private static int passed;

        [STAThread]
        private static void Main()
        {
            CaesarCipher caesar = new CaesarCipher();
            Check("Caesar encrypt", "Def abc!", caesar.Encrypt("Abc xyz!", "3"));
            Check("Caesar decrypt", "Abc xyz!", caesar.Decrypt("Def abc!", "3"));
            Check("Caesar negative key", "Zab", caesar.Encrypt("Abc", "-1"));

            Rot13Cipher rot13 = new Rot13Cipher();
            Check("ROT13", "Uryyb, Jbeyq!", rot13.Encrypt("Hello, World!", string.Empty));
            Check("ROT13 round trip", "Hello, World!", rot13.Decrypt("Uryyb, Jbeyq!", string.Empty));

            AtbashCipher atbash = new AtbashCipher();
            Check("Atbash", "Zyx CBA!", atbash.Encrypt("Abc XYZ!", string.Empty));
            Check("Atbash round trip", "Abc XYZ!", atbash.Decrypt("Zyx CBA!", string.Empty));

            VigenereCipher vigenere = new VigenereCipher();
            Check("Vigenere encrypt", "LXFOPVEFRNHR", vigenere.Encrypt("ATTACKATDAWN", "LEMON"));
            Check("Vigenere decrypt", "ATTACKATDAWN", vigenere.Decrypt("LXFOPVEFRNHR", "LEMON"));
            Check("Vigenere punctuation", "LXFOPV EF RNHR!", vigenere.Encrypt("ATTACK AT DAWN!", "LEMON"));

            AffineCipher affine = new AffineCipher();
            Check("Affine encrypt", "IHHWVCSWFRCP", affine.Encrypt("AFFINECIPHER", "5,8"));
            Check("Affine decrypt", "AFFINECIPHER", affine.Decrypt("IHHWVCSWFRCP", "5,8"));
            Check("Affine Chinese comma", "IHHWVC", affine.Encrypt("AFFINE", "5，8"));

            ExpectCipherError("Caesar invalid key", delegate { caesar.Encrypt("ABC", "x"); });
            ExpectCipherError("Vigenere empty key", delegate { vigenere.Encrypt("ABC", "123"); });
            ExpectCipherError("Affine non-coprime key", delegate { affine.Encrypt("ABC", "2,8"); });

            CheckExtendedCiphers();
            try { CheckNewCipherFamilies(); } catch (Exception exception) { Console.Error.WriteLine("NEW: " + exception.Message); Environment.Exit(1); }
            try { CheckExpansionCiphers(); } catch (Exception exception) { Console.Error.WriteLine("EXPANSION: " + exception.Message); Environment.Exit(1); }
            try { CheckLatestFeatures(); } catch (Exception exception) { Console.Error.WriteLine("LATEST: " + exception.Message); Environment.Exit(1); }
            try { CheckEncodingAndMoreClassics(); } catch (Exception exception) { Console.Error.WriteLine("ENCODING: " + exception.Message); Environment.Exit(1); }
            try { CheckAnalysis(); } catch (Exception exception) { Console.Error.WriteLine("ANALYSIS: " + exception.Message); Environment.Exit(1); }
            try { CheckNewCrackers(); } catch (Exception exception) { Console.Error.WriteLine("CRACKERS: " + exception.Message); Environment.Exit(1); }
            try { CheckExpansionCrackers(); } catch (Exception exception) { Console.Error.WriteLine("EXPANSION CRACKERS: " + exception.Message); Environment.Exit(1); }
            try { CheckToolRegistry(); CheckDocumentation(); } catch (Exception exception) { Console.Error.WriteLine("REGISTRY: " + exception.Message); Environment.Exit(1); }
            try { CheckLiveUi(); } catch (Exception exception) { Console.Error.WriteLine("UI: " + exception.Message); Environment.Exit(1); }

            Console.WriteLine("PASS " + passed);
        }

        private static void Check(string name, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new Exception(name + ": expected [" + expected + "] but got [" + actual + "]");
            }

            passed++;
        }

        private static void ExpectCipherError(string name, Action action)
        {
            try
            {
                action();
            }
            catch (CipherException)
            {
                passed++;
                return;
            }

            throw new Exception(name + ": expected CipherException");
        }

        private static void CheckDocumentation()
        {
            IList<ICryptoTool> tools = ToolRegistry.CreateAll();
            foreach (ICryptoTool tool in tools)
            {
                if (string.IsNullOrEmpty(ToolDocumentation.GetSummary(tool.Name)))
                {
                    throw new Exception("Missing documentation for " + tool.Name);
                }
            }
            passed++;
            using (HelpForm help = new HelpForm())
            {
                if (help.FormBorderStyle != FormBorderStyle.Sizable || !help.MaximizeBox) throw new Exception("Documentation window is not resizable");
                passed++;
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                TreeView index = (TreeView)typeof(HelpForm).GetField("index", flags).GetValue(help);
                RichTextBox details = (RichTextBox)typeof(HelpForm).GetField("details", flags).GetValue(help);
                if (index.Nodes.Count < 6 || details.Text.Length < 500 || details.Text.IndexOf("原理", StringComparison.Ordinal) < 0 || details.Text.IndexOf("示例", StringComparison.Ordinal) < 0) throw new Exception("Structured documentation is incomplete (groups=" + index.Nodes.Count + ", text=" + details.Text.Length + ")");
                passed++;
            }
            using (HelpForm contextual = new HelpForm("Book Cipher"))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic; TreeView index = (TreeView)typeof(HelpForm).GetField("index", flags).GetValue(contextual);
                if (index.SelectedNode == null || index.SelectedNode.Text != "Book Cipher") throw new Exception("Context help did not select the current tool"); passed++;
            }
        }

        private static void CheckEncodingAndMoreClassics()
        {
            string unicode = "密码箱 Hello";
            Check("Base64 Unicode round trip", unicode, TransferEncoding.Base64(TransferEncoding.Base64(unicode, false), true));
            Check("Base32 Unicode round trip", unicode, TransferEncoding.Base32(TransferEncoding.Base32(unicode, false), true));
            Check("Base58 Unicode round trip", unicode, TransferEncoding.Base58(TransferEncoding.Base58(unicode, false), true));
            Check("ASCII85 Unicode round trip", unicode, TransferEncoding.Ascii85(TransferEncoding.Ascii85(unicode, false), true));
            Check("Quoted printable round trip", unicode, TransferEncoding.QuotedPrintable(TransferEncoding.QuotedPrintable(unicode, false), true));
            Check("GB18030 byte round trip", unicode, TransferEncoding.CharsetBytes(TransferEncoding.CharsetBytes(unicode, "GB18030", false), "GB18030", true));
            Check("Braille grade one round trip", "Hello 123", BrailleCode.Transform(BrailleCode.Transform("Hello 123", false), true));
            Check("Baudot ITA2 round trip", "HELLO 123", BaudotCode.Transform(BaudotCode.Transform("HELLO 123", false), true));
            Check("Chinese telegraph known values", "一丁七", ChineseTelegraphCode.Transform("0001 0002 0003", true));
            Check("Chinese telegraph round trip", "中国人", ChineseTelegraphCode.Transform(ChineseTelegraphCode.Transform("中国人", false), true));
            string qr = QrCodeV1.Transform("HELLO QR", false); Check("QR version one round trip", "HELLO QR", QrCodeV1.Transform(qr, true));
            string code39 = BarcodeCode.Transform("CODE39", "CODE39", false); Check("Code39 round trip", "CODE39", BarcodeCode.Transform(code39, "CODE39", true));
            string ean = BarcodeCode.Transform("690123456789", "EAN13", false); Check("EAN13 round trip", "6901234567892", BarcodeCode.Transform(ean, "EAN13", true));
            string color = ColorEncoding.Text(unicode, false); Check("Color byte round trip", unicode, ColorEncoding.Text(color, true));
            string automatic = AutoDecoder.Decode(TransferEncoding.Base64("AUTOMATIC DECODING WORKS", false)); if (!automatic.StartsWith("#1", StringComparison.Ordinal) || automatic.IndexOf("AUTOMATIC DECODING WORKS", StringComparison.Ordinal) < 0) throw new Exception("Automatic decoder missed Base64"); passed++;
            string base64Sample = TransferEncoding.Base64("THIS IS A CLEAR BASE64 MESSAGE", false); if (!CipherIdentifier.Identify(base64Sample, string.Empty).StartsWith("#1  类型 Base64", StringComparison.Ordinal)) throw new Exception("Identifier missed Base64"); passed++;
            Dictionary<string, string> universalValues = new Dictionary<string, string> { { "language", "EN" }, { "effort", "快速" }, { "clue", string.Empty } }; string universalDecoded = UniversalCracker.Crack(new ToolRequest(ToolMode.Crack, base64Sample, universalValues)); if (universalDecoded.IndexOf("类型 Base64", StringComparison.Ordinal) < 0 || universalDecoded.IndexOf("THIS IS A CLEAR BASE64 MESSAGE", StringComparison.Ordinal) < 0 || universalDecoded.IndexOf("识别：#1  类型 Base64", StringComparison.Ordinal) < 0) throw new Exception("Universal crack did not include automatic Base64 decoding and identification"); passed++;
            string qrUniversal = UniversalCracker.Crack(new ToolRequest(ToolMode.Crack, QrCodeV1.Transform("HELLO QR", false), universalValues)); if (qrUniversal.IndexOf("类型 QR Code", StringComparison.Ordinal) < 0 || qrUniversal.IndexOf("HELLO QR", StringComparison.Ordinal) < 0) throw new Exception("Universal crack did not route identified QR to its decoder"); passed++;
            string keywordPlain = "ATTACK AT DAWN"; string keywordCipher = KeywordCipher.Transform(keywordPlain, "CIPHER", false); Check("Keyword cipher round trip", keywordPlain, KeywordCipher.Transform(keywordCipher, "CIPHER", true));
            string multiplicative = MultiplicativeCipher.Transform(keywordPlain, "5", false); Check("Multiplicative round trip", keywordPlain, MultiplicativeCipher.Transform(multiplicative, "5", true)); if (MultiplicativeCipher.Crack(multiplicative + multiplicative + multiplicative, "EN").IndexOf("密钥 5", StringComparison.Ordinal) < 0) throw new Exception("Multiplicative crack missed key 5"); passed++;
            Check("Unicode reverse round trip", "A😀文", ReverseCipher.Transform(ReverseCipher.Transform("A😀文")));
            Check("Vatsyayana round trip", keywordPlain, VatsyayanaCipher.Transform(VatsyayanaCipher.Transform(keywordPlain, "SECRET"), "SECRET"));
            Check("Hill 3x3 vector", "POH", Hill3Cipher.Transform("ACT", "6,24,1,13,16,10,20,17,15", false));
            Check("Hill 3x3 round trip", "PAYMOREMONEY", Hill3Cipher.Transform(Hill3Cipher.Transform("PAYMOREMONEY", "6,24,1,13,16,10,20,17,15", false), "6,24,1,13,16,10,20,17,15", true));
        }

        private static void CheckExtendedCiphers()
        {
            RotNCipher rot = new RotNCipher();
            Check("ROT-N encrypt", "Khoor 123", rot.Encrypt("Hello 123", "3"));
            Check("ROT-N decrypt", "Hello 123", rot.Decrypt("Khoor 123", "3"));
            Check("ROT47 round trip", "Hello! 123", rot.Decrypt(rot.Encrypt("Hello! 123", "47"), "47"));
            Check("ROT18 round trip", "Hello 123", rot.Decrypt(rot.Encrypt("Hello 123", "18"), "18"));

            RailFenceCipher rail = new RailFenceCipher();
            Check("Rail Fence vector", "WECRLTEERDSOEEFEAOCAIVDEN", rail.Encrypt("WEAREDISCOVEREDFLEEATONCE", "3"));
            Check("Rail Fence round trip", "Text with spaces!", rail.Decrypt(rail.Encrypt("Text with spaces!", "4"), "4"));

            ColumnarTranspositionCipher columnar = new ColumnarTranspositionCipher();
            Check("Columnar round trip", "Text with spaces!", columnar.Decrypt(columnar.Encrypt("Text with spaces!", "BALLOON"), "BALLOON"));

            PolybiusCipher polybius = new PolybiusCipher();
            Check("Polybius vector", "2315313134", polybius.Encrypt("HELLO", string.Empty));
            Check("Polybius round trip", "HELLO WORLD", polybius.Decrypt(polybius.Encrypt("HELLO WORLD", "KEY"), "KEY"));

            BaconCipher bacon = new BaconCipher();
            Check("Bacon round trip", "HELLO WORLD", bacon.Decrypt(bacon.Encrypt("HELLO WORLD", string.Empty), string.Empty));

            MonoalphabeticCipher mono = new MonoalphabeticCipher();
            const string reverse = "ZYXWVUTSRQPONMLKJIHGFEDCBA";
            Check("Monoalphabetic", "Zyx!", mono.Encrypt("Abc!", reverse));
            Check("Monoalphabetic round trip", "Abc!", mono.Decrypt(mono.Encrypt("Abc!", reverse), reverse));

            PlayfairCipher playfair = new PlayfairCipher();
            Check("Playfair vector", "BMODZBXDNABEKUDMUIXMMOUVIF", playfair.Encrypt("HIDE THE GOLD IN THE TREE STUMP", "PLAYFAIR EXAMPLE"));
            Check("Playfair decrypt", "HIDETHEGOLDINTHETREXESTUMP", playfair.Decrypt("BMODZBXDNABEKUDMUIXMMOUVIF", "PLAYFAIR EXAMPLE"));

            BeaufortCipher beaufort = new BeaufortCipher();
            Check("Beaufort round trip", "DEFEND THE EAST", beaufort.Decrypt(beaufort.Encrypt("DEFEND THE EAST", "FORT"), "FORT"));

            AutokeyCipher autokey = new AutokeyCipher();
            Check("Autokey vector", "QNXEPVYTWTWP", autokey.Encrypt("ATTACKATDAWN", "QUEENLY"));
            Check("Autokey round trip", "ATTACK AT DAWN", autokey.Decrypt(autokey.Encrypt("ATTACK AT DAWN", "QUEENLY"), "QUEENLY"));

            HillCipher hill = new HillCipher();
            Check("Hill vector", "HIAT", hill.Encrypt("HELP", "3,3,2,5"));
            Check("Hill round trip", "HELP", hill.Decrypt("HIAT", "3,3,2,5"));

            string bifid = BifidCipher.Encrypt("DEFENDTHEEASTWALL", "KEYWORD", "5");
            Check("Bifid round trip", "DEFENDTHEEASTWALL", BifidCipher.Decrypt(bifid, "KEYWORD", "5"));

            string adfgx = AdfgxCipher.Encrypt("ATTACKATDAWN", "CARGO", "BATTLE", false);
            Check("ADFGX round trip", "ATTACKATDAWN", AdfgxCipher.Decrypt(adfgx, "CARGO", "BATTLE", false));
            string adfgvx = AdfgxCipher.Encrypt("ATTACK2026", "CARGO", "BATTLE", true);
            Check("ADFGVX round trip", "ATTACK2026", AdfgxCipher.Decrypt(adfgvx, "CARGO", "BATTLE", true));
        }

        private static void CheckNewCipherFamilies()
        {
            PortaCipher porta = new PortaCipher();
            Check("Porta round trip", "DEFENDTHEEAST", porta.Decrypt(porta.Encrypt("DEFENDTHEEAST", "FORT"), "FORT"));
            GronsfeldCipher gronsfeld = new GronsfeldCipher();
            Check("Gronsfeld round trip", "ATTACK AT DAWN", gronsfeld.Decrypt(gronsfeld.Encrypt("ATTACK AT DAWN", "31415"), "31415"));
            RunningKeyCipher running = new RunningKeyCipher();
            Check("Running key round trip", "ATTACKATDAWN", running.Decrypt(running.Encrypt("ATTACKATDAWN", "THISISALONGKEYTEXT"), "THISISALONGKEYTEXT"));
            string four = FourSquareCipher.Transform("ATTACKATDAWN", "EXAMPLE", "KEYWORD", false);
            Check("Four-square round trip", "ATTACKATDAWN", FourSquareCipher.Transform(four, "EXAMPLE", "KEYWORD", true));
            string two = TwoSquareCipher.Transform("ATTACKATDAWN", "EXAMPLE", "KEYWORD", false);
            Check("Two-square round trip", "ATTACKATDAWN", TwoSquareCipher.Transform(two, "EXAMPLE", "KEYWORD", true));
            string nihilist = NihilistCipher.Encrypt("DEFEND", "ZEBRA", "KEY");
            Check("Nihilist round trip", "DEFEND", NihilistCipher.Decrypt(nihilist, "ZEBRA", "KEY"));
            string bazeries = BazeriesCipher.Transform("DEFENDTHEEAST", "375", "KEYWORD", false);
            Check("Bazeries round trip", "DEFENDTHEEAST", BazeriesCipher.Transform(bazeries, "375", "KEYWORD", true));
            string myszkowski = MyszkowskiCipher.Encrypt("WEAREDISCOVERED", "TOMATO");
            Check("Myszkowski round trip", "WEAREDISCOVERED", MyszkowskiCipher.Decrypt(myszkowski, "TOMATO"));
            string route = RouteCipher.Encrypt("ATTACKATDAWN", "4");
            Check("Route round trip", "ATTACKATDAWN", RouteCipher.Decrypt(route, "4"));
            string fractionated = FractionatedMorseCipher.Encrypt("DEFENDTHEEAST", "KEYWORD");
            Check("Fractionated Morse round trip", "DEFENDTHEEAST", FractionatedMorseCipher.Decrypt(fractionated, "KEYWORD"));
            string homophonic = HomophonicCipher.Encrypt("DEFEND", "KEYWORD");
            Check("Homophonic round trip", "DEFEND", HomophonicCipher.Decrypt(homophonic, "KEYWORD"));
            string checker = StraddlingCheckerboardCipher.Encrypt("DEFENDTHEEAST", "KEYWORD", "37");
            Check("Checkerboard round trip", "DEFENDTHEEAST", StraddlingCheckerboardCipher.Decrypt(checker, "KEYWORD", "37"));
        }

        private static void CheckAnalysis()
        {
            string cracked = ClassicalAnalysis.CrackCaesar("WKH TXLFN EURZQ IRA MXPSV RYHU WKH ODCB GRJ");
            if (!cracked.StartsWith("#1  密钥 3", StringComparison.Ordinal)) throw new Exception("Caesar cracking did not rank key 3 first");
            passed++;
            if (ClassicalAnalysis.Frequency("ABBA").IndexOf("A", StringComparison.Ordinal) < 0) throw new Exception("Frequency output missing A");
            passed++;
            if (ClassicalAnalysis.Ngrams("ABABAB", "2").IndexOf("AB", StringComparison.Ordinal) < 0) throw new Exception("N-gram output missing AB");
            passed++;
            if (ClassicalAnalysis.IndexOfCoincidence("THISISALONGERENGLISHTEXT").IndexOf("重合指数", StringComparison.Ordinal) < 0) throw new Exception("IC output missing value");
            passed++;
            if (ClassicalAnalysis.Kasiski("ABCXYZABCXYZABC", "3").IndexOf("可能的密钥长度", StringComparison.Ordinal) < 0) throw new Exception("Kasiski output missing factors");
            passed++;
            string longPlain = "THISISALONGENGLISHTEXTTHATCONTAINSMANYCOMMONWORDSANDLETTERFREQUENCIESTHEQUICKBROWNFOXJUMPSOVERTHELAZYDOGANDTHENRETURNSTOTHEHOUSE";
            string vigenereCrack = ClassicalAnalysis.CrackVigenere(new VigenereCipher().Encrypt(longPlain, "LEMON"), "EN");
            if (vigenereCrack.IndexOf("LEMON", StringComparison.Ordinal) < 0) throw new Exception("Vigenere cracking did not recover LEMON: " + vigenereCrack);
            passed++;
            if (LanguageModels.Normalize("fr") != "FR" || LanguageModels.Normalize("unknown") != "EN") throw new Exception("Language model selection failed");
            passed++;
            if (LanguageModels.NormalizeMatchMethod("AUTO", 20) != "COSINE" || LanguageModels.NormalizeMatchMethod("AUTO", 100) != "LLR" || LanguageModels.NormalizeMatchMethod("AUTO", 300) != "NGRAM") throw new Exception("Automatic language matching method selection failed"); passed++;
            if (LanguageModels.MatchMethodLabel("COSINE", 20) != "余弦相似度" || double.IsInfinity(LanguageModels.LanguageMatchScore("THEQUICKBROWNFOX", "EN", "LLR"))) throw new Exception("Language matching metrics failed"); passed++;
            string monoPlain = "THISISALONGENGLISHTEXTWITHCOMMONWORDSANDLETTERPATTERNSTHATSHOULDALLOWTHEHILLCLIMBINGALGORITHMTOPRODUCEACANDIDATEPLAINTEXT";
            string monoCipher = new MonoalphabeticCipher().Encrypt(monoPlain, "QWERTYUIOPASDFGHJKLZXCVBNM");
            if (ClassicalAnalysis.CrackMonoalphabetic(monoCipher, "EN").IndexOf("明文表", StringComparison.Ordinal) < 0) throw new Exception("Monoalphabetic cracking returned no key");
            passed++;
            string unicodeAlphabet = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩ"; StringBuilder unicodeCipherBuilder = new StringBuilder(); foreach (char value in monoCipher) unicodeCipherBuilder.Append(value >= 'A' && value <= 'Z' ? unicodeAlphabet[value - 'A'] : value); int unicodeProgress = 0; string unicodeMono = ClassicalAnalysis.CrackMonoalphabetic(unicodeCipherBuilder.ToString(), "EN", string.Empty, "3000", delegate(int value, string stage) { unicodeProgress = Math.Max(unicodeProgress, value); }, delegate { return false; }); if (unicodeMono.IndexOf("Unicode映射", StringComparison.Ordinal) < 0 || unicodeMono.IndexOf("密文表：", StringComparison.Ordinal) < 0 || unicodeProgress != 100) throw new Exception("Unicode monoalphabetic cracking was not activated"); passed++;
            StringBuilder unicodeLocks = new StringBuilder(); for (int i = 0; i < 26; i++) { char symbol = unicodeAlphabet[i]; if (unicodeCipherBuilder.ToString().IndexOf(symbol) < 0) continue; if (unicodeLocks.Length > 0) unicodeLocks.Append(','); unicodeLocks.Append(symbol).Append('=').Append((char)('A' + "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf((char)('A' + i)))); } string lockedUnicodeMono = ClassicalAnalysis.CrackMonoalphabetic(unicodeCipherBuilder.ToString(), "EN", unicodeLocks.ToString(), "500"); if (lockedUnicodeMono.IndexOf(monoPlain, StringComparison.Ordinal) < 0) throw new Exception("Unicode monoalphabetic locks did not preserve the known mapping"); passed++;
            string crackPlain = "THATTHISWITHFROMTHERTHATTHISWITHFROMTHERTHATTHISWITHFROMTHER";
            string rotCrack = ClassicalAnalysis.CrackRotN(new RotNCipher().Encrypt(crackPlain, "7"), "EN");
            if (rotCrack.IndexOf("密钥 7", StringComparison.Ordinal) < 0) throw new Exception("ROT-N cracking missed key 7");
            passed++;
            string railCrack = ClassicalAnalysis.CrackRailFence(new RailFenceCipher().Encrypt(crackPlain, "3"), "EN");
            if (railCrack.IndexOf("密钥 3", StringComparison.Ordinal) < 0) throw new Exception("Rail Fence cracking missed 3 rails");
            passed++;
            string routePlain = "THATTHISWITHFROMTHERTHATTHISWITHFROMTHERTHATTHISWITHFROMTHER";
            string routeCrack = ClassicalAnalysis.CrackRoute(RouteCipher.Encrypt(routePlain, "5"), "EN");
            if (routeCrack.IndexOf("密钥 5", StringComparison.Ordinal) < 0) throw new Exception("Route cracking missed width 5");
            passed++;
            string gronsfeldCrack = ClassicalAnalysis.CrackGronsfeld(new GronsfeldCipher().Encrypt(longPlain + longPlain, "31415"), "EN");
            if (gronsfeldCrack.IndexOf("31415", StringComparison.Ordinal) < 0) throw new Exception("Gronsfeld cracking missed key");
            passed++;
            string beaufortCrack = ClassicalAnalysis.CrackBeaufort(new BeaufortCipher().Encrypt(longPlain + longPlain, "FORT"), "EN");
            if (beaufortCrack.IndexOf("FORT", StringComparison.Ordinal) < 0) throw new Exception("Beaufort cracking missed key");
            passed++;
            string fractionatedPlain = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOGTHISISALONGENGLISHTEXTFORTESTINGCLASSICALCIPHERSEARCHANDLANGUAGESCORING";
            string fractionatedCipher = FractionatedMorseCipher.Encrypt(fractionatedPlain, "CIPHER");
            string fractionatedIdentification = CipherIdentifier.Identify(fractionatedCipher, string.Empty, "AUTO");
            if (fractionatedIdentification.IndexOf("Fractionated Morse", StringComparison.Ordinal) < 0) throw new Exception("Identifier omitted Fractionated Morse family"); passed++;
            Dictionary<string, string> universalValues = new Dictionary<string, string> { { "language", "EN" }, { "effort", "快速" } };
            string universalFractionated = UniversalCracker.Crack(new ToolRequest(ToolMode.Crack, fractionatedCipher, universalValues));
            if (universalFractionated.IndexOf(fractionatedPlain, StringComparison.Ordinal) < 0) throw new Exception("Universal crack discarded the Fractionated Morse answer"); passed++;
        }

        private static void CheckLatestFeatures()
        {
            TextRuleOptions rules = new TextRuleOptions { Alphabet = "ZYXWVUTSRQPONMLKJIHGFEDCBA", PreserveCase = true, PreserveSpaces = true, PreservePunctuation = true };
            Check("Custom alphabet in", "Zyx!", TextRules.ToWorking("Abc!", rules));
            Check("Custom alphabet out", "Abc!", TextRules.FromWorking("Zyx!", rules));
            if (CipherIdentifier.Identify("... --- ...", string.Empty).IndexOf("Morse", StringComparison.Ordinal) < 0) throw new Exception("Identifier missed Morse"); passed++;
            string ean = BarcodeCode.Transform("690123456789", "EAN13", false); if (!CipherIdentifier.Identify(ean, string.Empty).StartsWith("#1  类型 条形码", StringComparison.Ordinal)) throw new Exception("Identifier missed EAN-13 bit structure"); passed++;
            string qr = QrCodeV1.Transform("HELLO QR", false); if (!CipherIdentifier.Identify(qr, string.Empty).StartsWith("#1  类型 QR Code", StringComparison.Ordinal)) throw new Exception("Identifier missed QR matrix"); passed++;
            if (CipherIdentifier.Identify("00001 10010 10100 10100", string.Empty).IndexOf("博多码 ITA2", StringComparison.Ordinal) < 0) throw new Exception("Identifier missed five-unit Baudot groups"); passed++;
            if (!CipherIdentifier.Identify("0001 0002 0003 0004", string.Empty).StartsWith("#1  类型 中文电报码", StringComparison.Ordinal)) throw new Exception("Identifier missed Chinese telegraph groups"); passed++;
            string identifyPlain = "THISISALONGENGLISHTEXTWITHMANYCOMMONWORDSANDLETTERPATTERNSTHATSHOULDMAKEPERIODICSTATISTICSSTABLETHEMESSAGEISREPEATEDFORBETTERANALYSISTHISISALONGENGLISHTEXTWITHMANYCOMMONWORDSANDLETTERPATTERNS";
            string identifyVigenere = CipherIdentifier.Identify(new VigenereCipher().Encrypt(identifyPlain + identifyPlain, "LEMON"), string.Empty);
            if (!identifyVigenere.StartsWith("#1  类型 维吉尼亚", StringComparison.Ordinal) || identifyVigenere.IndexOf("LEMON", StringComparison.Ordinal) < 0) throw new Exception("Identifier missed clear Vigenere evidence: " + identifyVigenere); passed++;
            if (CipherIdentifier.Identify(new VigenereCipher().Encrypt(identifyPlain + identifyPlain, "LEMON"), "算法：维吉尼亚\r\n明文：THE").IndexOf("类型 维吉尼亚", StringComparison.Ordinal) < 0) throw new Exception("Identifier did not accept structured editor clues"); passed++;
            string identifierSample = "TO BE OR NOT TO BE THAT IS THE QUESTION WHETHER IT IS NOBLER IN THE MIND TO SUFFER THE SLINGS AND ARROWS OF OUTRAGEOUS FORTUNE OR TO TAKE ARMS AGAINST A SEA OF TROUBLES AND BY OPPOSING END THEM ";
            identifierSample += identifierSample + identifierSample;
            int universalPartials = 0; Dictionary<string, string> universalValues = new Dictionary<string, string> { { "language", "EN" }, { "effort", "快速" }, { "clue", string.Empty } }; string universalPlain = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG AND THIS NATURAL ENGLISH MESSAGE CONTAINS ENOUGH WORDS FOR UNIVERSAL CRACKING"; string universalOutput = UniversalCracker.Crack(new ToolRequest(ToolMode.Crack, new Rot13Cipher().Encrypt(universalPlain, string.Empty), universalValues, null, delegate { return false; }, delegate(string partial) { universalPartials++; })); int universalSecond = universalOutput.IndexOf("#2  ", StringComparison.Ordinal); string universalFirst = universalSecond < 0 ? universalOutput : universalOutput.Substring(0, universalSecond); if (universalFirst.IndexOf(universalPlain, StringComparison.Ordinal) < 0 || universalOutput.IndexOf("语言分 ", StringComparison.Ordinal) < 0 || universalOutput.IndexOf("语言分 100", StringComparison.Ordinal) >= 0 || universalOutput.IndexOf("综合 ", StringComparison.Ordinal) < 0 || universalPartials < 2) throw new Exception("Universal crack did not stream and rank ROT13 plaintext first with calibrated language score: " + universalOutput); passed++;
            Dictionary<string, string> fractionUniversalValues = new Dictionary<string, string> { { "language", "EN" }, { "effort", "标准" }, { "clue", string.Empty } }; string fractionUniversalPlain = new string(identifierSample.Where(delegate(char c) { return c >= 'A' && c <= 'Z'; }).ToArray()); string fractionUniversalOutput = UniversalCracker.Crack(new ToolRequest(ToolMode.Crack, FractionatedMorseCipher.Encrypt(fractionUniversalPlain, "CIPHER"), fractionUniversalValues)); if (fractionUniversalOutput.IndexOf("类型 Fractionated Morse", StringComparison.Ordinal) < 0 || fractionUniversalOutput.IndexOf(fractionUniversalPlain, StringComparison.Ordinal) < 0 || fractionUniversalOutput.IndexOf("匹配 70", StringComparison.Ordinal) < 0) throw new Exception("Universal crack did not promote the identified Fractionated Morse family: " + fractionUniversalOutput); passed++;
            string naturalIdentification = CipherIdentifier.Identify(identifierSample, string.Empty); if (!naturalIdentification.StartsWith("#1  类型 未加密自然语言", StringComparison.Ordinal)) throw new Exception("Identifier missed natural-language control EN=" + (LanguageModels.TextScore(identifierSample, "EN") / identifierSample.Length) + ": " + naturalIdentification); passed++;
            string identifyBeaufort = CipherIdentifier.Identify(new BeaufortCipher().Encrypt(identifierSample, "ORANGE"), string.Empty);
            if (!identifyBeaufort.StartsWith("#1  类型 Beaufort", StringComparison.Ordinal) || identifyBeaufort.IndexOf("ORANGE", StringComparison.Ordinal) < 0) throw new Exception("Identifier missed Beaufort evidence: " + identifyBeaufort); passed++;
            string identifyRail = CipherIdentifier.Identify(new RailFenceCipher().Encrypt(identifierSample, "4"), string.Empty);
            if (!identifyRail.StartsWith("#1  类型 栅栏", StringComparison.Ordinal)) throw new Exception("Identifier missed rail-fence trial: " + identifyRail); passed++;
            if (!CipherIdentifier.Identify(A1Z26Cipher.Encrypt(identifierSample), string.Empty).StartsWith("#1  类型 A1Z26", StringComparison.Ordinal)) throw new Exception("Identifier missed A1Z26 tokens"); passed++;
            if (!CipherIdentifier.Identify(NihilistCipher.Encrypt(identifierSample, "CIPHER", "CODE"), string.Empty).StartsWith("#1  类型 Nihilist", StringComparison.Ordinal)) throw new Exception("Identifier missed Nihilist numeric range"); passed++;
            if (!CipherIdentifier.Identify(FractionatedMorseCipher.Encrypt(identifierSample, "CIPHER"), string.Empty).StartsWith("#1  类型 Fractionated Morse", StringComparison.Ordinal)) throw new Exception("Identifier missed fractionated stream"); passed++;
            if (AnalysisWorkbench.Analyze("THISISALONGENGLISHTEXTFORANALYSIS", "AUTO", "3").IndexOf("Shannon", StringComparison.Ordinal) < 0) throw new Exception("Workbench missing entropy"); passed++;
            string chinese = "天地玄黄宇宙洪荒日月盈昃辰宿列张天地玄黄";
            string unicodeWorkbench = AnalysisWorkbench.Analyze(chinese, "AUTO", "2", "COSINE"); if (unicodeWorkbench.IndexOf("汉字", StringComparison.Ordinal) < 0 || unicodeWorkbench.IndexOf("天地", StringComparison.Ordinal) < 0 || unicodeWorkbench.IndexOf("Shannon", StringComparison.Ordinal) < 0) throw new Exception("Workbench did not analyze CJK text"); passed++;
            string shortLanguage = AnalysisWorkbench.Analyze("THE QUICK BROWN FOX", "AUTO", "2", "COSINE"); if (shortLanguage.IndexOf("语言匹配（余弦相似度）", StringComparison.Ordinal) < 0 || shortLanguage.IndexOf("EN", StringComparison.Ordinal) < 0) throw new Exception("Workbench did not expose selectable language metric"); passed++;
            if (CipherIdentifier.Identify(chinese, string.Empty, "LLR").IndexOf("非拉丁文本", StringComparison.Ordinal) < 0) throw new Exception("Identifier did not retain non-Latin text"); passed++;
            string symbolicCipher = "ℵ∃⨀⨀∴∫⋈⋈∴ℜ∇∞ℵ∀⨀⟡∴∀∃⋈ℵℵ∃⋈∴∅∃∅∅∇⊕†ℵ∅⊥⋈∅∇⨀⟡∀∅⟡⟡∀ℵ⋈⦸∴∅⨀∞∅⨀∞∞∫∫∇∞⋈⨀ℜ⊕∴⊥∫⨀∴ℜ∀ℵ⦸ℜ∇⊥∀∀ℜ∴∅∇∴†⋈⟡ℜ∴∴∅⋈∃⦸∫∴∴∴†ℵ∇ℵ∀∇∅∴∅∀∅∫∫∇∞∴∫∅ℜ⋈∇∇∞⦸ℜ∴∫∴∞†⨀∀∃†∃∴∅∀∅∴ℜ∃ℵℵ∀ℵ∴⋈⨀ℜ⊕∴⊥∫⨀ℵ∃⦸∇∅∇ℵ⊥∴†⊥∀∀ℵ⋈⦸∴∅†⨀ℵ∀⨀⟡†∇∇∃";
            string symbolicIdentification = CipherIdentifier.Identify(symbolicCipher, string.Empty, "LLR"); if (!symbolicIdentification.StartsWith("#1  类型 中文编码单表（Unicode 十六进制）", StringComparison.Ordinal) || symbolicIdentification.IndexOf("单表替换（Unicode 符号）", StringComparison.Ordinal) < 0) throw new Exception("Identifier missed Chinese Unicode-hex substitution structure: " + symbolicIdentification); passed++;
            List<string> symbolicUnits = UnicodeAnalysis.Units(symbolicCipher); if (symbolicUnits.Count != 180 || UnicodeAnalysis.Different(symbolicUnits) != 16 || !symbolicUnits.Contains("†")) throw new Exception("Unicode analysis dropped mathematical punctuation"); passed++;
            string symbolicCrack = ClassicalAnalysis.CrackMonoalphabetic(symbolicCipher, "EN", string.Empty, "500", "LLR", null, null); if (symbolicCrack.IndexOf("Unicode映射 / EN / 对数似然比", StringComparison.Ordinal) < 0 || symbolicCrack.IndexOf("†", StringComparison.Ordinal) < 0 || symbolicCrack.IndexOf("#2", StringComparison.Ordinal) < 0 || symbolicCrack.IndexOf("分词：", StringComparison.Ordinal) < 0 || symbolicCrack.IndexOf("共识", StringComparison.Ordinal) < 0 || symbolicCrack.IndexOf("置信", StringComparison.Ordinal) < 0) throw new Exception("Unicode spaceless substitution crack did not expose diverse candidates"); passed++;
            string chineseEncodedCrack = ClassicalAnalysis.CrackMonoalphabetic(symbolicCipher, "ZH", string.Empty, "50000", "NGRAM", null, null); if (!chineseEncodedCrack.StartsWith("#1  中文编码单表 / Unicode 十六进制", StringComparison.Ordinal) || chineseEncodedCrack.IndexOf("我很希望在昨天", StringComparison.Ordinal) < 0 || chineseEncodedCrack.IndexOf("汉字率", StringComparison.Ordinal) < 0) throw new Exception("Chinese Unicode-hex substitution recovery failed: " + chineseEncodedCrack); passed++;
            string automaticSymbolicCrack = ClassicalAnalysis.CrackMonoalphabetic(symbolicCipher, "AUTO", string.Empty, "500", "LLR", null, null); if (automaticSymbolicCrack.IndexOf(" / EN / ", StringComparison.Ordinal) < 0 || automaticSymbolicCrack.IndexOf("#3", StringComparison.Ordinal) < 0) throw new Exception("Spaceless AUTO crack did not retain multiple language hypotheses"); passed++;
            string naturalSequence = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOGTHEREAREMANYCOMMONWORDSINTHISENGLISHTEXT"; char[] reversedSequence = naturalSequence.ToCharArray(); Array.Reverse(reversedSequence); if (LanguageModels.SpacelessSubstitutionScore(naturalSequence, "EN") <= LanguageModels.SpacelessSubstitutionScore(new string(reversedSequence), "EN")) throw new Exception("Five-gram spaceless model did not prefer natural English order"); passed++;
            string monoKnownPlain = "ITWASTHEBESTOFTIMESITWASTHEWORSTOFTIMESITWASTHEAGEOFWISDOMITWASTHEAGEOFFOOLISHNESSWEHADBEFOREUSEVERYTHINGWEHADBEFOREUSNOTHINGWEWEREALLGOINGDIRECTTOHEAVENWEWEREALLGOINGDIRECTTHEOTHERWAY";
            string monoKnownAscii = new MonoalphabeticCipher().Encrypt(monoKnownPlain, "QWERTYUIOPASDFGHJKLZXCVBNM"); StringBuilder monoKnownSymbols = new StringBuilder(); foreach (char c in monoKnownAscii) monoKnownSymbols.Append((char)(0x24B6 + c - 'A'));
            string monoKnownCrack = ClassicalAnalysis.CrackMonoalphabetic(monoKnownSymbols.ToString(), "EN", string.Empty, "100000", "NGRAM", null, null); int monoRaw = monoKnownCrack.IndexOf("原串：", StringComparison.Ordinal); string monoRecovered = monoRaw < 0 ? string.Empty : monoKnownCrack.Substring(monoRaw + 3, monoKnownPlain.Length); if (monoRecovered != monoKnownPlain) throw new Exception("Advanced spaceless substitution search did not recover known plaintext: " + monoRecovered); passed++;
            if (ClassicalAnalysis.Frequency("Привет мир Привет").IndexOf("П", StringComparison.Ordinal) < 0) throw new Exception("Unicode frequency missed Cyrillic"); passed++;
            if (ClassicalAnalysis.Ngrams(chinese, "2").IndexOf("天地", StringComparison.Ordinal) < 0) throw new Exception("Unicode N-gram missed CJK pair"); passed++;
            if (ClassicalAnalysis.IndexOfCoincidence("مرحبا بالعالم مرحبا").IndexOf("重合指数", StringComparison.Ordinal) < 0) throw new Exception("Unicode IC rejected Arabic"); passed++;
            if (ClassicalAnalysis.Kasiski("天地玄黄天地玄黄天地", "3").IndexOf("可能的密钥长度", StringComparison.Ordinal) < 0) throw new Exception("Unicode Kasiski missed repeated CJK sequence"); passed++;
            if (CribAnalysis.Analyze("KHOOR", "HELLO", "Caesar").IndexOf("凯撒位移 3", StringComparison.Ordinal) < 0) throw new Exception("Crib missed Caesar shift"); passed++;

            ScytaleCipher scytale = new ScytaleCipher(); Check("Scytale round trip", "TEXT WITH SPACES", scytale.Decrypt(scytale.Encrypt("TEXT WITH SPACES", "5"), "5"));
            string redefence = RedefenceCipher.Encrypt("WEAREDISCOVERED", "4", "2"); Check("Redefence round trip", "WEAREDISCOVERED", RedefenceCipher.Decrypt(redefence, "4", "2"));
            string amsco = AmscoCipher.Encrypt("WEAREDISCOVERED", "CARGO"); Check("AMSCO round trip", "WEAREDISCOVERED", AmscoCipher.Decrypt(amsco, "CARGO"));
            string trifid = TrifidCipher.Encrypt("DEFENDTHEEAST", "KEYWORD", "5"); Check("Trifid round trip", "DEFENDTHEEAST", TrifidCipher.Decrypt(trifid, "KEYWORD", "5"));
            string morbit = MorbitCipher.Encrypt("DEFEND THE EAST", "KEYWORD"); Check("Morbit round trip", "DEFEND THE EAST", MorbitCipher.Decrypt(morbit, "KEYWORD"));
            string pollux = PolluxCipher.Encrypt("DEFEND THE EAST", "SEED"); Check("Pollux round trip", "DEFEND THE EAST", PolluxCipher.Decrypt(pollux, "SEED"));
            string alberti = AlbertiCipher.Transform("DEFEND THE EAST", "KEYWORD", "4", false); Check("Alberti round trip", "DEFEND THE EAST", AlbertiCipher.Transform(alberti, "KEYWORD", "4", true));
            string bellaso = BellasoCipher.Transform("DEFEND THE EAST", "FORT", "KEYWORD", false); Check("Bellaso round trip", "DEFEND THE EAST", BellasoCipher.Transform(bellaso, "FORT", "KEYWORD", true));
            string ragbaby = RagbabyCipher.Transform("RAG BABY", "ALPHABET", "1", "1", false); Check("Ragbaby round trip", "RAG BABY", RagbabyCipher.Transform(ragbaby, "ALPHABET", "1", "1", true));
            string jefferson = JeffersonWheelCipher.Transform("DEFENDTHEEAST", "1776", "3", false); Check("Jefferson round trip", "DEFENDTHEEAST", JeffersonWheelCipher.Transform(jefferson, "1776", "3", true));
            string three = ThreeSquareCipher.Encrypt("ATTACKATDAWN", "EXAMPLE", "KEYWORD"); Check("Three-square round trip", "ATTACKATDAWN", ThreeSquareCipher.Decrypt(three, "EXAMPLE", "KEYWORD"));
            string digrafid = DigrafidCipher.Encrypt("ATTACKATDAWN", "EXAMPLE", "KEYWORD", "5"); Check("Digrafid round trip", "ATTACKATDAWN", DigrafidCipher.Decrypt(digrafid, "EXAMPLE", "KEYWORD", "5"));
            string grandpre = GrandpreCipher.Encrypt("DEFEND", "KEYWORD"); Check("Grandpre round trip", "DEFEND", GrandpreCipher.Decrypt(grandpre, "KEYWORD"));
            string bookKey = "ALPHA BRAVO CHARLIE DELTA ECHO FOXTROT GOLF HOTEL INDIA JULIET KILO LIMA MIKE NOVEMBER OSCAR PAPA QUEBEC ROMEO SIERRA TANGO UNIFORM VICTOR WHISKEY XRAY YANKEE ZULU";
            string book = BookCipher.Encrypt("DEFEND", bookKey); Check("Book round trip", "DEFEND", BookCipher.Decrypt(book, bookKey));
            string ubchi = UbchiCipher.Encrypt("SECRET", "UBER", "X"); Check("Ubchi round trip", "SECRET", UbchiCipher.Decrypt(ubchi, "UBER", "1"));
            string grille = TurningGrilleCipher.Encrypt("ATTACKATDAWNXXXX", "4", "1,2,3,6"); Check("Turning grille round trip", "ATTACKATDAWN", TurningGrilleCipher.Decrypt(grille, "4", "1,2,3,6"));
            string vic = VicCipher.Encrypt("ATTACKATDAWN", "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", "72401");
            Check("VIC round trip", "ATTACKATDAWN", VicCipher.Decrypt(vic, "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", string.Empty));
            string vicNumeric = VicCipher.Encrypt("ATTACK2026.", "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", "72401");
            Check("VIC numeric round trip", "ATTACK2026.", VicCipher.Decrypt(vicNumeric, "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", string.Empty));
            string vicCut = VicCipher.Encrypt("ASSIGNEDOBJECTIVES", "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", "72401", "8");
            Check("VIC bifurcation round trip", "ASSIGNEDOBJECTIVES", VicCipher.Decrypt(vicCut, "ATONESIR", "TWAS THE NIGHT BEFORE CHRISTMAS", "139195", "6", string.Empty, "8"));
            string vicKeys = VicCipher.DescribeKeys("ATONESIR", "ALL THE PEOPLE ARE DEAD BUT I AM GONNA KEEP DANCING", "391752", "15", "60115");
            if (vicKeys.IndexOf("第一换位宽度：19", StringComparison.Ordinal) < 0 || vicKeys.IndexOf("第二换位宽度：20", StringComparison.Ordinal) < 0 || vicKeys.IndexOf("2960581734", StringComparison.Ordinal) < 0) throw new Exception("VIC historical key derivation mismatch: " + vicKeys); passed++;
        }

        private static void CheckExpansionCiphers()
        {
            string plain = "DEFEND THE EAST WALL AT DAWN";
            for (int variant = 1; variant <= 4; variant++) { string q = QuagmireCipher.Transform(plain, variant.ToString(), "EXAMPLE", "KEYWORD", "FORT", false); Check("Quagmire " + variant + " round trip", plain, QuagmireCipher.Transform(q, variant.ToString(), "EXAMPLE", "KEYWORD", "FORT", true)); }
            string g = GromarkCipher.Transform(plain, "KEYWORD", "31415", string.Empty, false, false); Check("Gromark round trip", plain, GromarkCipher.Transform(g, "KEYWORD", "31415", string.Empty, false, true));
            string pg = GromarkCipher.Transform(plain, "KEYWORD", "31415", "10", true, false); Check("Periodic Gromark round trip", plain, GromarkCipher.Transform(pg, "KEYWORD", "31415", "10", true, true));
            string chao = ChaocipherCipher.Transform(plain, "HXUCZVAMDSLKPEFJRIGTWOBNYQ", "PTLNBQDEOYSFAVZKGJRIHWXUMC", false); Check("Chaocipher round trip", plain, ChaocipherCipher.Transform(chao, "HXUCZVAMDSLKPEFJRIGTWOBNYQ", "PTLNBQDEOYSFAVZKGJRIHWXUMC", true));
            string solitaire = SolitaireCipher.Transform(plain, "CRYPTONOMICON", false); Check("Solitaire round trip", plain, SolitaireCipher.Transform(solitaire, "CRYPTONOMICON", true));
            string phillips = PhillipsCipher.Transform("DEFENDTHEEASTWALL", "GERMANY", false); Check("Phillips round trip", "DEFENDTHEEASTWALL", PhillipsCipher.Transform(phillips, "GERMANY", true));
            string block = "ABCDEFGHIJKLMNOPQRSTUVWXY"; string swagman = SwagmanCipher.Transform(block, "CARGO", false); Check("Swagman round trip", block, SwagmanCipher.Transform(swagman, "CARGO", true));
            string swagmanShort = SwagmanCipher.Transform(plain, "CARGO", false); Check("Swagman partial block", plain, SwagmanCipher.Transform(swagmanShort, "CARGO", true));
            string cadenusPlain = new string('A', 25) + new string('B', 25) + new string('C', 25) + new string('D', 25) + new string('E', 25); string cadenus = CadenusCipher.Transform(cadenusPlain, "CARGO", false); Check("Cadenus round trip", cadenusPlain, CadenusCipher.Transform(cadenus, "CARGO", true));
            string cadenusShort = CadenusCipher.Transform(plain, "CARGO", false); Check("Cadenus partial block", plain, CadenusCipher.Transform(cadenusShort, "CARGO", true));
            string nicodemus = NicodemusCipher.Transform(plain, "CIPHER", false); Check("Nicodemus round trip", plain, NicodemusCipher.Transform(nicodemus, "CIPHER", true));
            string disrupted = DisruptedTranspositionCipher.Transform(plain, "CARGO", false); Check("Disrupted round trip", plain, DisruptedTranspositionCipher.Transform(disrupted, "CARGO", true));
            Check("Enigma M3 vector", "BDZGO", EnigmaCipher.Transform("AAAAA", "M3", "I II III", "1 1 1", "AAA", "B", string.Empty));
            string enigmaM4 = EnigmaCipher.Transform(plain, "M4", "Beta I II III", "1 1 1 1", "AAAA", "B-Thin", "AV BS CG"); Check("Enigma M4 round trip", plain, EnigmaCipher.Transform(enigmaM4, "M4", "Beta I II III", "1 1 1 1", "AAAA", "B-Thin", "AV BS CG"));
        }

        private static void CheckExpansionCrackers()
        {
            string plain = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOGTHISISALONGENGLISHTEXTFORTESTINGCLASSICALCIPHERSEARCHANDLANGUAGESCORING";
            plain += plain;
            Dictionary<string, string> language = new Dictionary<string, string> { { "language", "EN" } }; int progress = 0; Action<int, string> report = delegate(int value, string stage) { progress = Math.Max(progress, value); };
            string runningCipher = new RunningKeyCipher().Encrypt(plain, plain); Dictionary<string, string> runningValues = new Dictionary<string, string>(language) { { "crib", plain } }; string running = ExpansionCrackers.CrackRunningKey(new ToolRequest(ToolMode.Crack, runningCipher, runningValues, report, delegate { return false; })); if (running.IndexOf(plain, StringComparison.Ordinal) < 0) throw new Exception("Running Key crib recovery failed"); passed++;
            progress = 0; string bazeriesCipher = BazeriesCipher.Transform(plain.Replace('J', 'I'), "37", "KEY", false); Dictionary<string, string> bazeriesValues = new Dictionary<string, string>(language) { { "minnumber", "37" }, { "maxnumber", "37" }, { "wordlimit", "6005" } }; string bazeries = ExpansionCrackers.CrackBazeries(new ToolRequest(ToolMode.Crack, bazeriesCipher, bazeriesValues, report, delegate { return false; })); if (bazeries.IndexOf(plain.Replace('J', 'I'), StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Bazeries dictionary search failed"); passed++;
            progress = 0; string ragbabyCipher = RagbabyCipher.Transform(plain, string.Empty, "1", "1", false); Dictionary<string, string> ragbabyValues = new Dictionary<string, string>(language) { { "minfirst", "1" }, { "maxfirst", "1" }, { "minstep", "1" }, { "maxstep", "1" }, { "wordlimit", "50" } }; string ragbaby = ExpansionCrackers.CrackRagbaby(new ToolRequest(ToolMode.Crack, ragbabyCipher, ragbabyValues, report, delegate { return false; })); if (ragbaby.IndexOf(plain.Replace('J', 'I').Replace('X', 'W'), StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Ragbaby standard alphabet search failed"); passed++;
            progress = 0; string jeffersonCipher = JeffersonWheelCipher.Transform(plain, "1776", "3", false); Dictionary<string, string> jeffersonValues = new Dictionary<string, string>(language) { { "minseed", "1776" }, { "maxseed", "1776" } }; string jefferson = ExpansionCrackers.CrackJefferson(new ToolRequest(ToolMode.Crack, jeffersonCipher, jeffersonValues, report, delegate { return false; })); if (jefferson.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Jefferson range search failed"); passed++;
            progress = 0; string albertiCipher = AlbertiCipher.Transform(plain, "KEYWORD", "4", false); Dictionary<string, string> albertiValues = new Dictionary<string, string>(language) { { "minperiod", "4" }, { "maxperiod", "4" }, { "wordlimit", "100" } }; string alberti = ExpansionCrackers.CrackAlberti(new ToolRequest(ToolMode.Crack, albertiCipher, albertiValues, report, delegate { return false; })); if (alberti.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Alberti dictionary search failed"); passed++;
            progress = 0; string bellasoCipher = BellasoCipher.Transform(plain, "FORT", string.Empty, false); Dictionary<string, string> bellasoValues = new Dictionary<string, string>(language) { { "wordlimit", "100" } }; string bellaso = ExpansionCrackers.CrackBellaso(new ToolRequest(ToolMode.Crack, bellasoCipher, bellasoValues, report, delegate { return false; })); if (bellaso.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Bellaso dictionary search failed"); passed++;
            string pairPlain = plain.Replace('J', 'I'); progress = 0; string threeCipher = ThreeSquareCipher.Encrypt(pairPlain, "EXAMPLE", "KEYWORD"); Dictionary<string, string> pairValues = new Dictionary<string, string>(language) { { "wordlimit", "100" } }; string three = ExpansionCrackers.CrackThreeSquare(new ToolRequest(ToolMode.Crack, threeCipher, pairValues, report, delegate { return false; })); if (three.IndexOf(pairPlain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Three-square dictionary search failed"); passed++;
            progress = 0; string digrafidCipher = DigrafidCipher.Encrypt(pairPlain, "EXAMPLE", "KEYWORD", "5"); Dictionary<string, string> digrafidValues = new Dictionary<string, string>(pairValues) { { "minperiod", "5" }, { "maxperiod", "5" } }; string digrafid = ExpansionCrackers.CrackDigrafid(new ToolRequest(ToolMode.Crack, digrafidCipher, digrafidValues, report, delegate { return false; })); if (digrafid.IndexOf(pairPlain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Digrafid dictionary search failed"); passed++;
            progress = 0; string enigmaCipher = EnigmaCipher.Transform(plain.Substring(0, 70), "M3", "I II III", "1 1 1", "AAA", "B", string.Empty); Dictionary<string, string> enigmaValues = new Dictionary<string, string> { { "model", "M3" }, { "rotors", "I II III" }, { "rings", "1 1 1" }, { "reflector", "B" }, { "crib", plain.Substring(0, 12) } }; string enigma = EnigmaCracker.Crack(new ToolRequest(ToolMode.Crack, enigmaCipher, enigmaValues, report, delegate { return false; })); if (enigma.IndexOf(plain.Substring(0, 70), StringComparison.Ordinal) < 0 || enigma.IndexOf("AAA", StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Enigma position search failed"); passed++;
        }

        private static void CheckToolRegistry()
        {
            IList<ICryptoTool> tools = ToolRegistry.CreateAll();
            if (tools.Count != 107) throw new Exception("Tool registry: expected 107 tools but got " + tools.Count);
            bool foundCrack = false;
            bool foundAnalyze = false;
            int crackable = 0;
            foreach (ICryptoTool tool in tools)
            {
                if (tool.Modes.Contains(ToolMode.Crack)) { foundCrack = true; crackable++; }
                if (tool.Modes.Contains(ToolMode.Analyze)) foundAnalyze = true;
            }
            if (!foundCrack || !foundAnalyze) throw new Exception("Tool registry missing crack or analysis modes");
            if (crackable != 58) throw new Exception("Tool registry: expected 58 crackable tools but got " + crackable);
            string[] expectedCrackers = { "自动解码", "ROT13", "Atbash", "培根", "A1Z26", "Tap Code", "Morse", "Keyword Cipher", "Multiplicative", "Reverse", "Hill 3×3", "Hill 2×2", "Morbit", "Pollux", "Turning Grille", "列换位", "Myszkowski", "AMSCO", "Autokey", "Playfair", "ADFGX", "ADFGVX", "Fractionated Morse", "Nihilist", "跨行棋盘", "Polybius", "Bifid", "同音替换", "Two-square", "Four-square", "Trifid", "双重列换位", "Ubchi", "Running Key", "Bazeries", "Alberti", "Bellaso", "Ragbaby", "Jefferson Wheel", "Three-square", "Digrafid", "Enigma" };
            foreach (string name in expectedCrackers)
            {
                ICryptoTool match = null; foreach (ICryptoTool tool in tools) if (tool.Name == name) { match = tool; break; }
                if (match == null || !match.Modes.Contains(ToolMode.Crack)) throw new Exception("Missing requested cracker: " + name);
            }
            int universal = -1, workbench = -1, crib = -1, base64 = -1, base32 = -1, caesar = -1, vatsyayana = -1; for (int i = 0; i < tools.Count; i++) { if (tools[i].Category == "分析") throw new Exception("Analysis still has a separate category"); if (tools[i].Name == "通用破解") universal = i; if (tools[i].Name == "分析工作台") workbench = i; if (tools[i].Name == "Crib 工具") crib = i; if (tools[i].Name == "Base64") base64 = i; if (tools[i].Name == "Base32") base32 = i; if (tools[i].Name == "凯撒") caesar = i; if (tools[i].Name == "Vatsyayana") vatsyayana = i; } if (!(universal >= 0 && universal < workbench && workbench < crib && base64 >= 0 && base64 < base32 && caesar >= 0 && caesar < vatsyayana)) throw new Exception("Tools are not ordered by commonness");
            passed++;
            IList<string> generalTags = ToolTags.AllForCategory(tools, ToolCategories.General); if (!generalTags.Contains("常用") || !generalTags.Contains("已知明文") || generalTags.Contains("全部")) throw new Exception("Tool tags were not generated for General"); ICryptoTool cribTool = tools[crib]; if (!ToolTags.Matches(cribTool, "已知明文") || ToolTags.Matches(cribTool, "图形")) throw new Exception("Tool tag matching is incorrect"); passed++;
            bool playfairHeuristic = false; foreach (ICryptoTool tool in tools) if (tool.Name == "Playfair") foreach (ToolParameter parameter in tool.Parameters) if (parameter.Id == "heuristic") playfairHeuristic = true; if (!playfairHeuristic) throw new Exception("Search heuristic parameter missing from Playfair"); passed++;
            bool bookLongText = false, languageChoice = false, monoAlphabet = false; foreach (ICryptoTool tool in tools) foreach (ToolParameter parameter in tool.Parameters) { if (tool.Name == "Book Cipher" && parameter.Id == "book" && parameter.Editor == ToolParameterEditor.LongTextFile) bookLongText = true; if (tool.Name == "单表替换" && parameter.Id == "key" && parameter.Editor == ToolParameterEditor.Alphabet) monoAlphabet = true; if (parameter.Id == "language" && parameter.Editor == ToolParameterEditor.Choice && parameter.DefaultValue == "AUTO") languageChoice = true; } if (!bookLongText || !languageChoice || !monoAlphabet) throw new Exception("Typed parameter editors were not registered"); passed++;
        }

        private static void CheckNewCrackers()
        {
            string plain = "THEQUICKBROWNFOXJUMPSOVERTHELAZYDOGTHISISALONGENGLISHTEXTFORTESTINGCLASSICALCIPHERSEARCHANDLANGUAGESCORING";
            plain += plain + plain;
            int progress = 0; Action<int, string> report = delegate(int value, string stage) { progress = Math.Max(progress, value); };
            Dictionary<string, string> language = new Dictionary<string, string> { { "language", "EN" } };

            string hillCipher = new HillCipher().Encrypt(plain, "3,3,2,5");
            string hill = ExtendedCrackers.CrackHill2(new ToolRequest(ToolMode.Crack, hillCipher, language, report, delegate { return false; }));
            if (hill.IndexOf(plain, StringComparison.Ordinal) < 0 || hill.IndexOf("3,3,2,5", StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Hill 2x2 search missed the known matrix"); passed++;

            progress = 0; string columnCipher = ColumnarTranspositionCipher.EncryptText(plain, "ZEBRAS");
            Dictionary<string, string> columnValues = new Dictionary<string, string>(language) { { "min", "6" }, { "max", "6" } };
            string column = ExtendedCrackers.CrackColumnar(new ToolRequest(ToolMode.Crack, columnCipher, columnValues, report, delegate { return false; }));
            if (column.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Columnar search missed the known order"); passed++;

            progress = 0; string morbitCipher = MorbitCipher.Encrypt(plain, "KEYWORD");
            string morbit = ExtendedCrackers.CrackMorbit(new ToolRequest(ToolMode.Crack, morbitCipher, language, report, delegate { return false; }));
            if (morbit.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Morbit search missed the mapping"); passed++;

            string polluxCipher = PolluxCipher.Encrypt(plain, "SEED");
            string pollux = ExtendedCrackers.CrackPollux(new ToolRequest(ToolMode.Crack, polluxCipher, language));
            if (pollux.IndexOf(plain, StringComparison.Ordinal) < 0) throw new Exception("Pollux automatic decode failed"); passed++;

            progress = 0; string amscoCipher = AmscoCipher.Encrypt(plain, "CARGO");
            Dictionary<string, string> amscoValues = new Dictionary<string, string>(language) { { "min", "5" }, { "max", "5" } };
            string amsco = AdvancedCrackers.CrackAmsco(new ToolRequest(ToolMode.Crack, amscoCipher, amscoValues, report, delegate { return false; }));
            if (amsco.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("AMSCO search missed the known order"); passed++;

            progress = 0; string grillePlain = plain.Substring(0, 96); string grilleCipher = TurningGrilleCipher.Encrypt(grillePlain, "4", "1,2,3,6");
            Dictionary<string, string> grilleValues = new Dictionary<string, string>(language) { { "size", "4" } };
            string grille = AdvancedCrackers.CrackTurningGrille(new ToolRequest(ToolMode.Crack, grilleCipher, grilleValues, report, delegate { return false; }));
            if (grille.IndexOf(grillePlain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Turning Grille search missed the known holes"); passed++;

            progress = 0; string adfgxCipher = AdfgxCipher.Encrypt(plain, string.Empty, "CARGO", false);
            Dictionary<string, string> adfgxValues = new Dictionary<string, string>(language) { { "min", "5" }, { "max", "5" }, { "square", "ABCDEFGHIKLMNOPQRSTUVWXYZ" } };
            string adfgx = AdvancedCrackers.CrackAdfgx(new ToolRequest(ToolMode.Crack, adfgxCipher, adfgxValues, report, delegate { return false; }), false);
            if (adfgx.IndexOf(plain.Replace('J', 'I'), StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("ADFGX search missed the known column order"); passed++;

            progress = 0; AutokeyCipher autokeyAlgorithm = new AutokeyCipher(); string autokeyCipher = autokeyAlgorithm.Encrypt(plain, "KEY");
            Dictionary<string, string> autokeyValues = new Dictionary<string, string>(language) { { "min", "3" }, { "max", "3" }, { "iterations", "4" } };
            string autokey = AdvancedCrackers.CrackAutokey(new ToolRequest(ToolMode.Crack, autokeyCipher, autokeyValues, report, delegate { return false; }));
            if (autokey.IndexOf(plain, StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Autokey search missed the known initial key"); passed++;

            progress = 0; string nihilistCipher = NihilistCipher.Encrypt(plain.Replace('J', 'I'), string.Empty, "KEY");
            Dictionary<string, string> nihilistValues = new Dictionary<string, string>(language) { { "square", string.Empty }, { "min", "3" }, { "max", "3" } };
            string nihilist = AdvancedCrackers.CrackNihilist(new ToolRequest(ToolMode.Crack, nihilistCipher, nihilistValues, report, delegate { return false; }));
            if (nihilist.IndexOf(plain.Replace('J', 'I'), StringComparison.Ordinal) < 0 || progress != 100) throw new Exception("Nihilist search missed the known additive key"); passed++;

            string sample = plain.Substring(0, 96); Dictionary<string, string> brief = new Dictionary<string, string>(language) { { "iterations", "500" }, { "restarts", "1" }, { "minperiod", "5" }, { "maxperiod", "5" }, { "min", "6" }, { "max", "6" }, { "nullmax", "3" } };
            List<string> smoke = new List<string>();
            Dictionary<string, string> briefThousand = new Dictionary<string, string>(brief); briefThousand["iterations"] = "1000";
            smoke.Add(AdvancedCrackers.CrackMyszkowski(new ToolRequest(ToolMode.Crack, MyszkowskiCipher.Encrypt(sample, "TOMATO"), briefThousand, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackPlayfair(new ToolRequest(ToolMode.Crack, new PlayfairCipher().Encrypt(sample, "CIPHER"), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackFractionatedMorse(new ToolRequest(ToolMode.Crack, FractionatedMorseCipher.Encrypt(sample, "CIPHER"), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackPolybius(new ToolRequest(ToolMode.Crack, new PolybiusCipher().Encrypt(sample, "CIPHER"), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackBifid(new ToolRequest(ToolMode.Crack, BifidCipher.Encrypt(sample, "CIPHER", "5"), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackCheckerboard(new ToolRequest(ToolMode.Crack, StraddlingCheckerboardCipher.Encrypt(sample, "CIPHER", "37"), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackHomophonic(new ToolRequest(ToolMode.Crack, HomophonicCipher.Encrypt(sample, "CIPHER"), briefThousand, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackTwoSquare(new ToolRequest(ToolMode.Crack, TwoSquareCipher.Transform(sample, "CIPHER", "KEYWORD", false), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackFourSquare(new ToolRequest(ToolMode.Crack, FourSquareCipher.Transform(sample, "CIPHER", "KEYWORD", false), brief, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackTrifid(new ToolRequest(ToolMode.Crack, TrifidCipher.Encrypt(sample, "CIPHER", "5"), brief, report, delegate { return false; })));
            string twice = ColumnarTranspositionCipher.EncryptText(ColumnarTranspositionCipher.EncryptText(sample, "CARGO"), "ZEBRA");
            Dictionary<string, string> briefFive = new Dictionary<string, string>(brief); briefFive["min"] = "5"; briefFive["max"] = "5"; briefFive["iterations"] = "1000";
            smoke.Add(AdvancedCrackers.CrackDoubleColumnar(new ToolRequest(ToolMode.Crack, twice, briefFive, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackUbchi(new ToolRequest(ToolMode.Crack, UbchiCipher.Encrypt(sample, "CARGO", "XYZ"), briefFive, report, delegate { return false; })));
            smoke.Add(AdvancedCrackers.CrackAdfgx(new ToolRequest(ToolMode.Crack, AdfgxCipher.Encrypt(sample, string.Empty, "CARGO", true), new Dictionary<string, string>(language) { { "min", "5" }, { "max", "5" }, { "iterations", "500" } }, report, delegate { return false; }), true));
            foreach (string result in smoke) if (string.IsNullOrWhiteSpace(result) || result.IndexOf("#1", StringComparison.Ordinal) < 0) throw new Exception("Advanced cracker smoke test returned no candidate"); passed++;
            foreach (string strategy in new[] { "模拟退火", "爬山", "延迟接受", "再加热退火" }) { Dictionary<string, string> strategyValues = new Dictionary<string, string>(brief) { { "heuristic", strategy } }; string result = AdvancedCrackers.CrackPolybius(new ToolRequest(ToolMode.Crack, new PolybiusCipher().Encrypt(sample, "CIPHER"), strategyValues, report, delegate { return false; })); if (result.IndexOf("#1", StringComparison.Ordinal) < 0) throw new Exception("Heuristic strategy returned no candidate: " + strategy); } passed++;
        }

        private static void CheckLiveUi()
        {
            CipherForm form = new CipherForm();
            try
            {
                form.ShowInTaskbar = false;
                form.Opacity = 0;
                form.Show();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                TextBox input = (TextBox)typeof(CipherForm).GetField("inputBox", flags).GetValue(form);
                TextBox output = (TextBox)typeof(CipherForm).GetField("outputBox", flags).GetValue(form);
                Dictionary<string, TextBox> parameters = (Dictionary<string, TextBox>)typeof(CipherForm).GetField("parameterBoxes", flags).GetValue(form);
                ComboBox category = (ComboBox)typeof(CipherForm).GetField("categoryPicker", flags).GetValue(form);
                ComboBox tags = (ComboBox)typeof(CipherForm).GetField("tagPicker", flags).GetValue(form);
                if (form.Text != "密码箱 1.1.8" || category.Items.Contains("全部") || !category.Items.Contains(ToolCategories.Encoding)) throw new Exception("Product version or concrete categories not applied");
                if (!tags.Items.Contains("常用") || !tags.Items.Contains("可破解") || tags.Items.Contains("全部")) throw new Exception("Tag picker was not populated");
                passed++;
                tags.SelectedItem = "可破解"; ComboBox taggedTools = (ComboBox)typeof(CipherForm).GetField("toolPicker", flags).GetValue(form); foreach (object item in taggedTools.Items) if (!((ICryptoTool)item).Modes.Contains(ToolMode.Crack)) throw new Exception("Tag picker retained a non-crackable tool"); tags.SelectedItem = ToolTags.Any; passed++;
                category.SelectedItem = ToolCategories.Substitution;
                parameters["key"].Text = "3";
                input.Text = "ABC";
                DateTime deadline = DateTime.UtcNow.AddSeconds(2);
                while (output.Text != "DEF" && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                Check("Live UI update", "DEF", output.Text);

                typeof(CipherForm).GetMethod("SetMode", flags).Invoke(form, new object[] { ToolMode.Crack });
                input.Text = "WKH TXLFN EURZQ IRA MXPSV RYHU WKH ODCB GRJ";
                DataGridView candidates = (DataGridView)typeof(CipherForm).GetField("candidateGrid", flags).GetValue(form);
                deadline = DateTime.UtcNow.AddSeconds(3);
                while ((!candidates.Visible || candidates.Rows.Count == 0) && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
                if (!candidates.Visible || candidates.Rows.Count == 0) throw new Exception("Candidate panel did not populate");
                passed++;

                ComboBox tools = (ComboBox)typeof(CipherForm).GetField("toolPicker", flags).GetValue(form);
                for (int i = 0; i < tools.Items.Count; i++) if (((ICryptoTool)tools.Items[i]).Name == "单表替换") { tools.SelectedIndex = i; break; }
                typeof(CipherForm).GetMethod("SetMode", flags).Invoke(form, new object[] { ToolMode.Crack });
                Dictionary<string, ComboBox> parameterPickers = (Dictionary<string, ComboBox>)typeof(CipherForm).GetField("parameterPickers", flags).GetValue(form); if (!parameterPickers.ContainsKey("method") || parameterPickers["method"].Items.Count != 5 || parameterPickers["method"].Text != "AUTO") throw new Exception("Monoalphabetic language metric is not a selectable list"); passed++;
                typeof(CipherForm).GetMethod("SetMode", flags).Invoke(form, new object[] { ToolMode.Encrypt }); Application.DoEvents(); FlowLayoutPanel monoParameters = (FlowLayoutPanel)typeof(CipherForm).GetField("parameterPanel", flags).GetValue(form); bool alphabetButton = false; foreach (Control card in monoParameters.Controls) foreach (Control host in card.Controls) foreach (Control child in host.Controls) if (child is Button && child.Text == "…") alphabetButton = true; if (!parameters.ContainsKey("key") || parameters["key"].Width < 300 || !alphabetButton) throw new Exception("Monoalphabetic alphabet editor is too small or unreachable"); passed++;
                Type alphabetDialogType = typeof(CipherForm).GetNestedType("AlphabetParameterDialog", flags); ConstructorInfo alphabetConstructor = alphabetDialogType.GetConstructor(flags, null, new[] { typeof(string) }, null); Form alphabetDialog = (Form)alphabetConstructor.Invoke(new object[] { "ABCDEFGHIJKLMNOPQRSTUVWXYZ" }); TextBox quickAlphabet = (TextBox)alphabetDialogType.GetField("quick", flags).GetValue(alphabetDialog); quickAlphabet.Text = "ZYXWVUTSRQPONMLKJIHGFEDCBA"; alphabetDialogType.GetMethod("AcceptAlphabet", flags).Invoke(alphabetDialog, null); string editedAlphabet = (string)alphabetDialogType.GetProperty("Value", flags).GetValue(alphabetDialog, null); alphabetDialog.Dispose(); if (editedAlphabet != "ZYXWVUTSRQPONMLKJIHGFEDCBA") throw new Exception("Quick alphabet input was not applied"); passed++;
                category.SelectedItem = ToolCategories.Polyalphabetic;
                for (int i = 0; i < tools.Items.Count; i++) if (((ICryptoTool)tools.Items[i]).Name == "维吉尼亚") { tools.SelectedIndex = i; break; }
                typeof(CipherForm).GetMethod("SetMode", flags).Invoke(form, new object[] { ToolMode.Crack });
                Application.DoEvents();
                TableLayoutPanel rootLayout = (TableLayoutPanel)typeof(CipherForm).GetField("rootLayout", flags).GetValue(form);
                ToolTip tips = (ToolTip)typeof(CipherForm).GetField("tips", flags).GetValue(form);
                if (rootLayout.RowStyles[1].Height <= 52 || parameterPickers["language"].Width <= 180 || string.IsNullOrEmpty(tips.GetToolTip(parameterPickers["language"]))) throw new Exception("Parameter hints are still clipped without an accessible full label");
                passed++;
                if (tools.DropDownWidth < tools.Width) throw new Exception("Tool dropdown is narrower than its display field");
                passed++;
                ProgressBar workProgress = (ProgressBar)typeof(CipherForm).GetField("workProgress", flags).GetValue(form);
                Button cancelWork = (Button)typeof(CipherForm).GetField("cancelWorkButton", flags).GetValue(form);
                typeof(CipherForm).GetMethod("SetWorkProgress", flags).Invoke(form, new object[] { true, 42, "搜索" });
                if (!workProgress.Parent.Visible || workProgress.Value != 42 || !cancelWork.Visible) throw new Exception("Long-running crack progress controls are not visible");
                cancelWork.PerformClick(); if (workProgress.Parent.Visible) throw new Exception("Crack cancel did not close progress controls"); passed++;
                category.SelectedItem = ToolCategories.General;
                for (int i = 0; i < tools.Items.Count; i++) if (((ICryptoTool)tools.Items[i]).Name == "频率") { tools.SelectedIndex = i; break; }
                input.Text = "ABBA";
                deadline = DateTime.UtcNow.AddSeconds(2);
                while (output.Text.IndexOf("字符", StringComparison.Ordinal) < 0 && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                if (output.Text.IndexOf("字符", StringComparison.Ordinal) < 0) throw new Exception("Live analysis did not update");
                passed++;
                if (ContainsRunButton(form)) throw new Exception("Persistent run button still exists");
                passed++;
                category.SelectedItem = ToolCategories.Substitution;
                tools.SelectedIndex = 0;
                parameters["key"].Text = "3";
                string first = Path.GetFullPath(Path.Combine("tests", "data", "batch-a.txt"));
                string second = Path.GetFullPath(Path.Combine("tests", "data", "batch-b.txt"));
                typeof(CipherForm).GetMethod("LoadFiles", flags).Invoke(form, new object[] { new[] { first, second } });
                deadline = DateTime.UtcNow.AddSeconds(2);
                while (output.Text.IndexOf("DEF", StringComparison.Ordinal) < 0 && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents(); Thread.Sleep(20);
                }
                if (output.Text.IndexOf("batch-a.txt", StringComparison.Ordinal) < 0 || output.Text.IndexOf("DEF", StringComparison.Ordinal) < 0) throw new Exception("Batch files were not processed independently");
                passed++;
                for (int i = 0; i < tools.Items.Count; i++) if (((ICryptoTool)tools.Items[i]).Name == "Book Cipher") { tools.SelectedIndex = i; break; }
                Application.DoEvents(); FlowLayoutPanel parameterPanel = (FlowLayoutPanel)typeof(CipherForm).GetField("parameterPanel", flags).GetValue(form); bool longEditorButton = false; foreach (Control card in parameterPanel.Controls) foreach (Control host in card.Controls) foreach (Control child in host.Controls) if (child is Button && child.Text == "…") longEditorButton = true;
                if (!parameters.ContainsKey("book") || !parameters["book"].ReadOnly || !longEditorButton) throw new Exception("Book Cipher long-text file editor is not reachable"); passed++;
                ((System.Windows.Forms.Timer)typeof(CipherForm).GetField("liveTimer", flags).GetValue(form)).Dispose();
            }
            finally
            {
                form.Dispose();
            }
        }

        private static bool ContainsRunButton(Control root)
        {
            foreach (Control control in root.Controls)
            {
                Button button = control as Button;
                if (button != null && button.Text == "运行") return true;
                if (ContainsRunButton(control)) return true;
            }
            return false;
        }
    }
}
