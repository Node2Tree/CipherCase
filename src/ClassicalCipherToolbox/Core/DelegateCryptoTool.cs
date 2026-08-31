using System;
using System.Collections.Generic;

namespace ClassicalCipherToolbox.Core
{
    internal sealed class DelegateCryptoTool : ICryptoTool
    {
        private readonly IList<ToolMode> modes;
        private readonly IList<ToolParameter> parameters;
        private readonly Func<ToolRequest, string> executor;

        internal DelegateCryptoTool(
            string name,
            string category,
            IEnumerable<ToolMode> modes,
            IEnumerable<ToolParameter> parameters,
            Func<ToolRequest, string> executor)
        {
            Name = name;
            Category = category;
            this.modes = new List<ToolMode>(modes).AsReadOnly();
            this.parameters = new List<ToolParameter>(parameters).AsReadOnly();
            this.executor = executor;
        }

        public string Name { get; private set; }
        public string Category { get; private set; }
        public IList<ToolMode> Modes { get { return modes; } }
        public IList<ToolParameter> Parameters { get { return parameters; } }

        public string Execute(ToolRequest request)
        {
            if (!modes.Contains(request.Mode))
            {
                throw new CipherException("当前工具不支持此模式");
            }

            foreach (ToolParameter parameter in parameters)
            {
                if (parameter.AppliesTo(request.Mode) &&
                    parameter.Required &&
                    string.IsNullOrWhiteSpace(request.Get(parameter.Id)))
                {
                    throw new CipherException("缺少参数：" + parameter.Hint);
                }
            }

            return executor(request) ?? string.Empty;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
