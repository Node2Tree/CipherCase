using System;
using System.Collections.Generic;

namespace ClassicalCipherToolbox.Core
{
    internal sealed class ToolRequest
    {
        private readonly IDictionary<string, string> values;
        private readonly Action<int, string> progress;
        private readonly Func<bool> cancellation;
        private readonly Action<string> partial;

        internal ToolRequest(ToolMode mode, string input, IDictionary<string, string> values)
            : this(mode, input, values, null, null)
        {
        }

        internal ToolRequest(ToolMode mode, string input, IDictionary<string, string> values, Action<int, string> progress, Func<bool> cancellation)
            : this(mode, input, values, progress, cancellation, null)
        {
        }

        internal ToolRequest(ToolMode mode, string input, IDictionary<string, string> values, Action<int, string> progress, Func<bool> cancellation, Action<string> partial)
        {
            Mode = mode;
            Input = input ?? string.Empty;
            this.values = values ?? new Dictionary<string, string>();
            this.progress = progress;
            this.cancellation = cancellation;
            this.partial = partial;
        }

        internal ToolMode Mode { get; private set; }
        internal string Input { get; private set; }

        internal string Get(string id)
        {
            string value;
            return values.TryGetValue(id, out value) ? value : string.Empty;
        }

        internal bool IsCancellationRequested { get { return cancellation != null && cancellation(); } }

        internal void ReportProgress(int percent, string stage)
        {
            if (progress != null) progress(Math.Max(0, Math.Min(100, percent)), stage ?? string.Empty);
        }

        internal void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested) throw new OperationCanceledException();
        }

        internal void ReportPartial(string output)
        {
            if (partial != null) partial(output ?? string.Empty);
        }
    }
}
