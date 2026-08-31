using System;

namespace ClassicalCipherToolbox.Core
{
    internal sealed class CipherException : Exception
    {
        public CipherException(string message)
            : base(message)
        {
        }
    }
}
