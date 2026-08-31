namespace ClassicalCipherToolbox.Core
{
    internal static class Alphabet
    {
        internal const int Length = 26;

        internal static bool IsAsciiLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') ||
                   (value >= 'a' && value <= 'z');
        }

        internal static int IndexOf(char value)
        {
            return value >= 'a' && value <= 'z'
                ? value - 'a'
                : value - 'A';
        }

        internal static char FromIndex(int index, bool lowerCase)
        {
            int normalized = Mod(index, Length);
            return (char)((lowerCase ? 'a' : 'A') + normalized);
        }

        internal static char Shift(char value, int amount)
        {
            if (!IsAsciiLetter(value))
            {
                return value;
            }

            bool lowerCase = value >= 'a' && value <= 'z';
            return FromIndex(IndexOf(value) + amount, lowerCase);
        }

        internal static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
