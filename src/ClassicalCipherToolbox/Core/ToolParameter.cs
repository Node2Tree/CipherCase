using System;

namespace ClassicalCipherToolbox.Core
{
    internal sealed class ToolParameter
    {
        private readonly ToolMode[] modes;

        internal ToolParameter(string id, string hint, bool required, params ToolMode[] modes)
        {
            Id = id;
            Hint = hint;
            Required = required;
            this.modes = modes ?? new ToolMode[0];
        }

        internal string Id { get; private set; }
        internal string Hint { get; private set; }
        internal bool Required { get; private set; }

        internal bool AppliesTo(ToolMode mode)
        {
            if (modes.Length == 0)
            {
                return true;
            }

            return Array.IndexOf(modes, mode) >= 0;
        }
    }
}
