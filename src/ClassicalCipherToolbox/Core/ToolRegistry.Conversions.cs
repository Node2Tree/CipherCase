using System.Collections.Generic;
using ClassicalCipherToolbox.Ciphers;

namespace ClassicalCipherToolbox.Core
{
    internal static partial class ToolRegistry
    {
        private static void AddConversions(List<ICryptoTool> tools)
        {
            AddCodec(tools, "格雷码", GrayCode.Transform);
            tools.Add(new DelegateCryptoTool("进制换算", ToolCategories.Encoding,
                new[] { ToolMode.Encode },
                new[] {
                    new ToolParameter("from", "源进制", false, ToolParameterEditor.Choice, "10", RadixConversion.BaseChoices()),
                    new ToolParameter("to", "目标进制", false, ToolParameterEditor.Choice, "16", RadixConversion.BaseChoices()),
                    new ToolParameter("precision", "小数位上限 1–4096", false, ToolParameterEditor.Text, "64", null),
                    new ToolParameter("rounding", "超出上限", false, ToolParameterEditor.Choice, "精确", new[] { "精确", "截断", "最近偶数" })
                }, RadixConversion.Convert));
            tools.Add(new DelegateCryptoTool("数据类型编码", ToolCategories.Encoding,
                new[] { ToolMode.Encode, ToolMode.Decode },
                new[] {
                    new ToolParameter("type", "数据类型", false, ToolParameterEditor.Choice, "Int32", MachineDataEncoding.TypeChoices),
                    new ToolParameter("radix", "数值进制 2–36", false, ToolParameterEditor.Choice, "10", RadixConversion.BaseChoices()),
                    new ToolParameter("endian", "字节序", false, ToolParameterEditor.Choice, "小端", new[] { "小端", "大端" }),
                    new ToolParameter("bytes", "位模式格式", false, ToolParameterEditor.Choice, "十六进制字节", new[] { "十六进制字节", "二进制位" }),
                    new ToolParameter("overflow", "整数溢出", false, ToolParameterEditor.Choice, "报错", new[] { "报错", "环绕", "饱和" }).When("type", "Int8", "UInt8", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Q 定点数"),
                    new ToolParameter("display", "浮点解码显示", false, ToolParameterEditor.Choice, "最短十进制", new[] { "最短十进制", "精确" }, ToolMode.Decode).When("type", "Float16", "BFloat16", "Float32", "Float64"),
                    new ToolParameter("precision", "浮点显示小数位 1–4096", false, ToolParameterEditor.Text, "64", null, ToolMode.Decode).When("type", "Float16", "BFloat16", "Float32", "Float64"),
                    new ToolParameter("width", "Q 定点总位宽", false, ToolParameterEditor.Choice, "32", MachineDataEncoding.WidthChoices).When("type", "Q 定点数"),
                    new ToolParameter("fraction", "Q 定点小数位", false, ToolParameterEditor.Choice, "16", MachineDataEncoding.FractionChoices).When("type", "Q 定点数"),
                    new ToolParameter("signed", "Q 定点符号", false, ToolParameterEditor.Choice, "有符号", new[] { "有符号", "无符号" }).When("type", "Q 定点数"),
                    new ToolParameter("quantize", "Q 定点量化", false, ToolParameterEditor.Choice, "报错", new[] { "报错", "截断", "最近偶数" }, ToolMode.Encode).When("type", "Q 定点数")
                }, MachineDataEncoding.Transform));
            tools.Add(new DelegateCryptoTool("碱基转氨基酸", ToolCategories.Encoding,
                new[] { ToolMode.Encode },
                new[] {
                    new ToolParameter("frame", "阅读框", false, ToolParameterEditor.Choice, "+1", new[] { "+1", "+2", "+3", "-1", "-2", "-3" }),
                    new ToolParameter("format", "氨基酸格式", false, ToolParameterEditor.Choice, "单字母", new[] { "单字母", "三字母" }),
                    new ToolParameter("tail", "不足三位的末尾", false, ToolParameterEditor.Choice, "报错", new[] { "报错", "忽略并提示" })
                }, GeneticCode.Translate));
        }
    }
}
