using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace ClassicalCipherToolbox.Analysis
{
    internal sealed class EnglishFiveGramModel
    {
        internal short Floor;
        internal System.Collections.Generic.Dictionary<int, short> Values;
    }

    internal static class EnglishNgramData
    {
        internal static short[] Load()
        {
            byte[] raw = new byte[(17576 + 456976) * 2];
            using (Stream resource = Resource("ClassicalCipherToolbox.Analysis.EnglishNgrams"))
            using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress))
            {
                int position = 0; while (position < raw.Length) { int read = gzip.Read(raw, position, raw.Length - position); if (read == 0) break; position += read; }
            }
            short[] values = new short[17576 + 456976]; Buffer.BlockCopy(raw, 0, values, 0, raw.Length); return values;
        }

        internal static string[] LoadWords()
        {
            using (Stream resource = Resource("ClassicalCipherToolbox.Analysis.EnglishKeywords"))
            using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(gzip)) return reader.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static EnglishFiveGramModel LoadFiveGrams()
        {
            using (Stream resource = Resource("ClassicalCipherToolbox.Analysis.EnglishFiveGrams"))
            using (GZipStream gzip = new GZipStream(resource, CompressionMode.Decompress))
            using (BinaryReader reader = new BinaryReader(gzip))
            {
                short floor = reader.ReadInt16(); int count = reader.ReadInt32(); System.Collections.Generic.Dictionary<int, short> values = new System.Collections.Generic.Dictionary<int, short>(count);
                for (int i = 0; i < count; i++) values[(int)reader.ReadUInt32()] = reader.ReadInt16();
                return new EnglishFiveGramModel { Floor = floor, Values = values };
            }
        }

        private static Stream Resource(string name)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name); if (stream == null) throw new InvalidOperationException("语言模型资源缺失"); return stream;
        }
    }
}
