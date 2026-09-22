using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        internal static IList<ICryptoTool> CreateAll()
        {
            List<ICryptoTool> tools = new List<ICryptoTool>();
            AddGeneral(tools);
            AddEncoding(tools);
            AddConversions(tools);
            AddChinese(tools);
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

        private delegate string TwoKeyTransform(string text,string key1,string key2,bool decrypt);
        private static ICryptoTool TwoKeyTool(string name,string category,string hint1,string hint2,TwoKeyTransform transform)
        {
            return new DelegateCryptoTool(name,category,new[]{ToolMode.Encrypt,ToolMode.Decrypt},new[]{new ToolParameter("key1",hint1,true),new ToolParameter("key2",hint2,true)},delegate(ToolRequest r){return transform(r.Input,r.Get("key1"),r.Get("key2"),r.Mode==ToolMode.Decrypt);});
        }
    }
}
