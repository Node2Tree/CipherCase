using System;

namespace ClassicalCipherToolbox.Core
{
    internal enum ToolParameterEditor
    {
        Text,
        Choice,
        LongTextFile,
        Alphabet
    }

    internal sealed class ToolParameter
    {
        private readonly ToolMode[] modes;
        private string dependencyId;
        private string[] dependencyValues;

        internal ToolParameter(string id, string hint, bool required, params ToolMode[] modes)
        {
            Id = id;
            Hint = hint;
            Required = required;
            Editor = ToolParameterEditor.Text;
            DefaultValue = string.Empty;
            Choices = new string[0];
            this.modes = modes ?? new ToolMode[0];
        }

        internal ToolParameter(string id, string hint, bool required, ToolParameterEditor editor,
            string defaultValue, string[] choices, params ToolMode[] modes)
        {
            Id = id;
            Hint = hint;
            Required = required;
            Editor = editor;
            DefaultValue = defaultValue ?? string.Empty;
            Choices = choices ?? new string[0];
            this.modes = modes ?? new ToolMode[0];
        }

        internal string Id { get; private set; }
        internal string Hint { get; private set; }
        internal bool Required { get; private set; }
        internal ToolParameterEditor Editor { get; private set; }
        internal string DefaultValue { get; private set; }
        internal string[] Choices { get; private set; }
        internal string DependencyId { get { return dependencyId ?? string.Empty; } }

        internal ToolParameter When(string parameterId, params string[] values)
        {
            dependencyId = parameterId;
            dependencyValues = values ?? new string[0];
            return this;
        }

        internal bool IsVisible(string dependencyValue)
        {
            return string.IsNullOrEmpty(dependencyId) || Array.IndexOf(dependencyValues, dependencyValue) >= 0;
        }

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
