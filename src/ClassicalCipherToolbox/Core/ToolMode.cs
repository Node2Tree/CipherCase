namespace ClassicalCipherToolbox.Core
{
    internal enum ToolMode
    {
        Encrypt,
        Decrypt,
        Encode,
        Decode,
        Crack,
        Analyze
    }

    internal static class ToolModeInfo
    {
        internal static string Label(ToolMode mode)
        {
            switch (mode)
            {
                case ToolMode.Encrypt: return "加密";
                case ToolMode.Decrypt: return "解密";
                case ToolMode.Encode: return "编码";
                case ToolMode.Decode: return "解码";
                case ToolMode.Crack: return "破解";
                default: return "分析";
            }
        }
    }
}
