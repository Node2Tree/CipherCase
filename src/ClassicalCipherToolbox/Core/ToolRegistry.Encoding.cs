using System;
using System.Collections.Generic;
using ClassicalCipherToolbox.Analysis;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private delegate string Codec(string input, bool decode);
        private static void AddCodec(List<ICryptoTool> tools, string name, Codec codec)
        {
            tools.Add(new DelegateCryptoTool(name, ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return codec(r.Input, r.Mode == ToolMode.Decode); }));
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
            AddCodec(tools, "北约音标字母", NatoPhonetic.Transform);
            AddCodec(tools, "猪圈密码符号", SymbolCodes.Pigpen);
            AddCodec(tools, "旗语", SymbolCodes.FlagSemaphore);
            tools.Add(new DelegateCryptoTool("条形码", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new[] { new ToolParameter("type", "类型", false, ToolParameterEditor.Choice, "CODE39", new[] { "CODE39", "EAN13" }) }, delegate(ToolRequest r) { return BarcodeCode.Transform(r.Input, r.Get("type"), r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("QR Code", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return QrCodeV1.Transform(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("颜色编码", ToolCategories.Encoding, new[] { ToolMode.Encode, ToolMode.Decode }, new ToolParameter[0], delegate(ToolRequest r) { return ColorEncoding.Text(r.Input, r.Mode == ToolMode.Decode); }));
            tools.Add(new DelegateCryptoTool("取色器与调色盘", ToolCategories.Encoding, new[] { ToolMode.Analyze }, new ToolParameter[0], delegate(ToolRequest r) { return ColorEncoding.Palette(r.Input); }));
        }
    }
}
