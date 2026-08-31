namespace ClassicalCipherToolbox.Core
{
    internal interface ICipher
    {
        string Name { get; }
        bool RequiresKey { get; }
        string KeyHint { get; }

        string Encrypt(string input, string key);
        string Decrypt(string input, string key);
    }
}
