using System.Collections.Generic;

namespace ClassicalCipherToolbox.Core
{
    internal interface ICryptoTool
    {
        string Name { get; }
        string Category { get; }
        IList<ToolMode> Modes { get; }
        IList<ToolParameter> Parameters { get; }
        string Execute(ToolRequest request);
    }
}
