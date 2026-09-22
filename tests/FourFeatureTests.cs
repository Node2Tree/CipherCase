using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ClassicalCipherToolbox.Ciphers;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox.Tests
{
    internal static partial class CipherTests
    {
        private static ToolRequest ConversionRequest(string input, params string[] values)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < values.Length; i += 2) parameters.Add(values[i], values[i + 1]);
            return new ToolRequest(ToolMode.Encode, input, parameters);
        }

        private static ToolRequest MachineRequest(ToolMode mode, string input, params string[] values)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int i = 0; i < values.Length; i += 2) parameters.Add(values[i], values[i + 1]);
            return new ToolRequest(mode, input, parameters);
        }

        private static void CheckConversions()
        {
            Check("Gray known sequence", "0000 0001 0011 0010 0110 0111 0101 0100",
                GrayCode.Transform("0000 0001 0010 0011 0100 0101 0110 0111", false));
            Check("Gray inverse vector", "0100", GrayCode.Transform("0110", true));
            Check("Gray separators", "0011\r\n0010\t0000", GrayCode.Transform("0010\r\n0011\t0000", false));
            string previous = null;
            for (int value = 0; value < 256; value++)
            {
                string binary = Convert.ToString(value, 2).PadLeft(8, '0');
                string gray = GrayCode.Transform(binary, false);
                if (GrayCode.Transform(gray, true) != binary) throw new Exception("Gray round trip failed: " + value);
                if (previous != null)
                {
                    int differences = 0;
                    for (int bit = 0; bit < gray.Length; bit++) if (gray[bit] != previous[bit]) differences++;
                    if (differences != 1) throw new Exception("Adjacent Gray codes must differ by one bit");
                }
                previous = gray;
            }
            passed++;
            ExpectCipherError("Gray invalid bit", delegate { GrayCode.Transform("012", false); });

            Check("Radix large signed vector", "FF -2A 10000000000000000",
                RadixConversion.Convert(ConversionRequest("255 -42 18446744073709551616", "from", "10", "to", "16")));
            Check("Radix case and zero", "255\n-0\t0", RadixConversion.Convert(ConversionRequest("+ff\n-0\t000", "from", "16", "to", "10")));
            Check("Radix base36", "35", RadixConversion.Convert(ConversionRequest("z", "from", "36", "to", "10")));
            foreach (string radix in RadixConversion.BaseChoices())
            {
                const string value = "-123456789012345678901234567890";
                string encoded = RadixConversion.Convert(ConversionRequest(value, "from", "10", "to", radix));
                if (RadixConversion.Convert(ConversionRequest(encoded, "from", radix, "to", "10")) != value)
                    throw new Exception("Radix round trip failed: " + radix);
            }
            passed++;
            ExpectCipherError("Radix invalid digit", delegate { RadixConversion.Convert(ConversionRequest("2", "from", "2")); });
            Check("Radix terminating fraction", "FF.A", RadixConversion.Convert(ConversionRequest("255.625", "from", "10", "to", "16")));
            Check("Radix repeating fraction", "0.0(0011)", RadixConversion.Convert(ConversionRequest("0.1", "from", "10", "to", "2")));
            Check("Radix fraction syntax", "0.(3)", RadixConversion.Convert(ConversionRequest("1/3", "from", "10", "to", "10")));
            Check("Radix repeating input", "1/7", RadixConversion.Convert(ConversionRequest("0.(142857)", "from", "10", "to", "10", "precision", "4")));
            Check("Radix scientific notation", "1000", RadixConversion.Convert(ConversionRequest("1e3", "from", "10", "to", "10")));
            Check("Radix prefix and separators", "65535", RadixConversion.Convert(ConversionRequest("0xFF_FF", "from", "16", "to", "10")));
            Check("Radix rounded fraction", "≈0.00011010", RadixConversion.Convert(ConversionRequest("0.1", "from", "10", "to", "2", "precision", "8", "rounding", "最近偶数")));
            Check("Radix rounding carry", "≈1.00", RadixConversion.Convert(ConversionRequest("0.999", "from", "10", "to", "10", "precision", "2", "rounding", "最近偶数")));
            Check("Radix negative fraction", "-1.1", RadixConversion.Convert(ConversionRequest("-1.5", "from", "10", "to", "2")));
            ExpectCipherError("Radix sign only", delegate { RadixConversion.Convert(ConversionRequest("-")); });
            ExpectCipherError("Radix out of range", delegate { RadixConversion.Convert(ConversionRequest("1", "from", "37")); });
            ExpectCipherError("Radix size limit", delegate { RadixConversion.Convert(ConversionRequest(new string('1', 4097))); });

            Check("Int32 little endian", "78 56 34 12", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "305419896", "type", "Int32")));
            Check("Int32 big endian", "12 34 56 78", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "305419896", "type", "Int32", "endian", "大端")));
            Check("Int32 decode", "305419896", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "78 56 34 12", "type", "Int32")));
            Check("Signed integer complement", "FF", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "-1", "type", "Int8")));
            Check("Signed integer decode", "-128", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "80", "type", "Int8")));
            Check("Unsigned integer decode", "255", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "FF", "type", "UInt8")));
            Check("Integer wrap", "00", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "256", "type", "UInt8", "overflow", "环绕")));
            Check("Integer saturate", "FF", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "999", "type", "UInt8", "overflow", "饱和")));
            Check("Int64 minimum", "00 00 00 00 00 00 00 80", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "-9223372036854775808", "type", "Int64")));
            ExpectCipherError("Integer overflow", delegate { MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "256", "type", "UInt8")); });
            ExpectCipherError("Integer rejects fraction", delegate { MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1.5", "type", "Int32")); });

            Check("Float16 one", "00 3C", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1", "type", "Float16")));
            Check("BFloat16 one", "80 3F", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1", "type", "BFloat16")));
            Check("Float32 one", "00 00 80 3F", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1", "type", "Float32")));
            Check("Float64 one", "00 00 00 00 00 00 F0 3F", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1", "type", "Float64")));
            Check("Float32 decode", "2.5", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00 00 20 40", "type", "Float32")));
            Check("Float32 common value", "CD CC CC 3D", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "0.1", "type", "Float32")));
            Check("Float32 shortest decode", "0.1", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "CD CC CC 3D", "type", "Float32")));
            Check("Float negative zero", "00 00 00 80", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "-0", "type", "Float32")));
            Check("Float negative zero decode", "-0", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00 00 00 80", "type", "Float32")));
            Check("Float infinity", "00 00 80 7F", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "Infinity", "type", "Float32")));
            Check("Float infinity decode", "-Infinity", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00 00 80 FF", "type", "Float32")));
            Check("Float NaN decode", "NaN", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00 00 C0 7F", "type", "Float32")));
            Check("Float16 tie to even", "00 3C", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1.00000000001", "type", "Float16", "radix", "2")));
            string subnormal = MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "01 00 00 00", "type", "Float32", "radix", "2", "display", "精确", "precision", "200"));
            Check("Float32 smallest subnormal round trip", "01 00 00 00", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, subnormal, "type", "Float32", "radix", "2")));
            string[,] floatPatterns = {
                { "Float16", "01 00" }, { "Float16", "00 04" }, { "Float16", "FF 7B" },
                { "BFloat16", "01 00" }, { "BFloat16", "80 00" }, { "BFloat16", "7F 7F" },
                { "Float32", "01 00 00 00" }, { "Float32", "FF FF 7F 7F" },
                { "Float64", "01 00 00 00 00 00 00 00" }, { "Float64", "FF FF FF FF FF FF EF 7F" }
            };
            for (int i = 0; i < floatPatterns.GetLength(0); i++)
            {
                string value = MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, floatPatterns[i, 1], "type", floatPatterns[i, 0]));
                Check(floatPatterns[i, 0] + " boundary round trip", floatPatterns[i, 1], MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, value, "type", floatPatterns[i, 0])));
            }

            Check("Decimal128 vector", "7B 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1.23", "type", ".NET Decimal128")));
            Check("Decimal128 decode", "1.23", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "7B 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00", "type", ".NET Decimal128")));
            Check("Q16.16 vector", "00 80 01 00", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1.5", "type", "Q 定点数", "width", "32", "fraction", "16")));
            Check("Q16.16 decode", "1.5", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00 80 01 00", "type", "Q 定点数", "width", "32", "fraction", "16")));
            Check("Q signed negative", "00 80 FF FF", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "-0.5", "type", "Q 定点数", "width", "32", "fraction", "16")));
            Check("Q quantize", "02", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "0.2", "type", "Q 定点数", "width", "8", "fraction", "3", "quantize", "最近偶数")));
            ExpectCipherError("Q exact quantization", delegate { MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "0.2", "type", "Q 定点数", "width", "8", "fraction", "3")); });
            Check("Machine binary bits", "00111100 00000000", MachineDataEncoding.Transform(MachineRequest(ToolMode.Encode, "1", "type", "Float16", "bytes", "二进制位", "endian", "大端")));
            Check("Machine binary decode", "1", MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00111100 00000000", "type", "Float16", "bytes", "二进制位", "endian", "大端")));
            ExpectCipherError("Machine byte count", delegate { MachineDataEncoding.Transform(MachineRequest(ToolMode.Decode, "00", "type", "Int32")); });

            Check("DNA known translation", "MAIVMGR*KGAR*", GeneticCode.Translate(ConversionRequest("ATGGCCATTGTAATGGGCCGCTGAAAGGGTGCCCGATAG")));
            Check("RNA translation", "MA*", GeneticCode.Translate(ConversionRequest("aug gcu uaa")));
            Check("FASTA single record", "MA*", GeneticCode.Translate(ConversionRequest(">sample\nATG\nGCTTAA\n")));
            Check("Three letter output", "Met Ala Stop", GeneticCode.Translate(ConversionRequest("ATGGCTTAA", "format", "三字母")));
            Check("Ambiguous consensus", "A*X", GeneticCode.Translate(ConversionRequest("GCNTARNNN")));
            Check("Stops retained", "M*M", GeneticCode.Translate(ConversionRequest("ATGTGAATG")));
            Check("No start coercion", "LL", GeneticCode.Translate(ConversionRequest("TTGCTG")));
            string[] sequences = { "ATGAAA", "AATGAAA", "AAATGAAA", "TTTCAT", "TTTCATA", "TTTCATAA" };
            string[] frames = { "+1", "+2", "+3", "-1", "-2", "-3" };
            for (int i = 0; i < frames.Length; i++) Check("Frame " + frames[i], "MK", GeneticCode.Translate(ConversionRequest(sequences[i], "frame", frames[i])));
            Check("Complement ambiguity", "NBDHVKMWSRYACGT", GeneticCode.ReverseComplement("ACGTRYSWKMBDHVN"));
            Check("Tail warning", "M\r\n（已忽略末尾 1 个碱基）", GeneticCode.Translate(ConversionRequest("ATGA", "tail", "忽略并提示")));
            ExpectCipherError("DNA incomplete codon", delegate { GeneticCode.Translate(ConversionRequest("ATGA")); });
            ExpectCipherError("DNA invalid base", delegate { GeneticCode.Translate(ConversionRequest("AT!")); });
            ExpectCipherError("DNA too short", delegate { GeneticCode.Translate(ConversionRequest("AT")); });
            ExpectCipherError("DNA multiple FASTA", delegate { GeneticCode.Translate(ConversionRequest(">one\nATG\n>two\nAAA")); });
            ExpectCipherError("DNA empty FASTA", delegate { GeneticCode.Translate(ConversionRequest(">one")); });
            ExpectCipherError("DNA invalid frame", delegate { GeneticCode.Translate(ConversionRequest("ATG", "frame", "0")); });

            Check("Semaphore known pairs", "↓↙ / ↓← / ↓↖", SemaphoreCode.Transform("ABC", false));
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Check("Semaphore entire alphabet", alphabet, SemaphoreCode.Transform(SemaphoreCode.Transform(alphabet, false), true));
            using (SemaphorePreview preview = new SemaphorePreview())
            {
                preview.SetOutput(SemaphoreCode.Transform(alphabet, false), true);
                if (preview.SymbolCount != 26) throw new Exception("Semaphore symbols missing");
                preview.SetOutput(new string('A', 300), false);
                if (preview.SymbolCount != 256) throw new Exception("Semaphore preview limit failed");
                preview.SetOutput(string.Empty, false);
                if (preview.SymbolCount != 0) throw new Exception("Semaphore preview did not clear");
                passed++;
            }
            foreach (string name in new[] { "格雷码", "进制换算", "碱基转氨基酸" })
            {
                ICryptoTool found = null;
                foreach (ICryptoTool tool in ToolRegistry.CreateAll()) if (tool.Name == name) found = tool;
                if (found == null || string.IsNullOrEmpty(ToolDocumentation.GetSummary(name))) throw new Exception("Missing conversion registration/help: " + name);
                string input = name == "格雷码" ? "0010" : name == "进制换算" ? "255" : "ATG";
                string expected = name == "格雷码" ? "0011" : name == "进制换算" ? "FF" : "M";
                Check("Registered " + name, expected, found.Execute(ConversionRequest(input)));
            }
        }

        private static void CheckConversionUi()
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (CipherForm form = new CipherForm())
            {
                form.Show();
                ComboBox category = (ComboBox)typeof(CipherForm).GetField("categoryPicker", flags).GetValue(form);
                ComboBox picker = (ComboBox)typeof(CipherForm).GetField("toolPicker", flags).GetValue(form);
                TextBox input = (TextBox)typeof(CipherForm).GetField("inputBox", flags).GetValue(form);
                TextBox output = (TextBox)typeof(CipherForm).GetField("outputBox", flags).GetValue(form);
                SemaphorePreview preview = (SemaphorePreview)typeof(CipherForm).GetField("semaphorePreview", flags).GetValue(form);
                category.SelectedItem = ToolCategories.Encoding;
                SelectConversion(picker, "旗语");
                input.Text = "ABCDEFG";
                WaitForConversion(delegate { return output.Text == SemaphoreCode.Transform("ABCDEFG", false) && preview.Visible; });
                if (preview.SymbolCount != 7 || preview.Bottom > output.Top) throw new Exception("Semaphore preview overlaps output or misses letters");
                using (Bitmap snapshot = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(snapshot, new Rectangle(Point.Empty, snapshot.Size));
                    snapshot.Save(Path.Combine("work", "tests", "semaphore-ui.png"), ImageFormat.Png);
                }
                passed++;
                typeof(CipherForm).GetMethod("SwapText", flags).Invoke(form, null);
                WaitForConversion(delegate { return output.Text == "ABCDEFG" && preview.SymbolCount == 7; });
                passed++;
                input.Clear();
                Application.DoEvents();
                if (preview.Visible || preview.SymbolCount != 0) throw new Exception("Clearing input leaves stale semaphore drawings");
                passed++;
                SelectConversion(picker, "进制换算");
                input.Text = "255";
                WaitForConversion(delegate { return output.Text == "FF"; });
                typeof(CipherForm).GetMethod("SwapText", flags).Invoke(form, null);
                WaitForConversion(delegate { return output.Text == "255"; });
                if (preview.Visible) throw new Exception("Semaphore preview leaked into another tool");
                passed++;
                SelectConversion(picker, "数据类型编码");
                Dictionary<string, ComboBox> parameterPickers = (Dictionary<string, ComboBox>)typeof(CipherForm).GetField("parameterPickers", flags).GetValue(form);
                parameterPickers["type"].SelectedItem = "Float32";
                if (parameterPickers.ContainsKey("width") || parameterPickers.ContainsKey("overflow")) throw new Exception("Irrelevant machine parameters remain visible for Float32");
                input.Text = "1.5";
                WaitForConversion(delegate { return output.Text == "00 00 C0 3F"; });
                using (Bitmap snapshot = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(snapshot, new Rectangle(Point.Empty, snapshot.Size));
                    snapshot.Save(Path.Combine("work", "tests", "machine-data-ui.png"), ImageFormat.Png);
                }
                typeof(CipherForm).GetMethod("SwapText", flags).Invoke(form, null);
                WaitForConversion(delegate { return output.Text == "1.5"; });
                passed++;
            }
        }

        private static void SelectConversion(ComboBox picker, string name)
        {
            for (int i = 0; i < picker.Items.Count; i++)
                if (((ICryptoTool)picker.Items[i]).Name == name) { picker.SelectedIndex = i; return; }
            throw new Exception("Tool not in picker: " + name);
        }

        private static void WaitForConversion(Func<bool> complete)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (!complete() && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
            if (!complete()) throw new Exception("Conversion UI did not finish in time");
        }
    }
}
