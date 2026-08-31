using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClassicalCipherToolbox
{
    internal static class NativeMethods
    {
        private const int EmSetCueBanner = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            int message,
            IntPtr showWhenFocused,
            string text);

        internal static void SetCueBanner(TextBox textBox, string text)
        {
            SendMessage(textBox.Handle, EmSetCueBanner, (IntPtr)1, text);
        }
    }
}
