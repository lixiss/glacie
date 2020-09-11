using System;

using Glacie.CommandLine.IO;

namespace Glacie.CommandLine.UI
{
    internal class TerminalStreamWriter : IStandardStreamWriter, IDisposable
    {
        private readonly Terminal _terminal;
        private readonly StandardStreamWriter _standardStreamWriter;

        public TerminalStreamWriter(Terminal terminal, StandardStreamWriter standardStreamWriter)
        {
            _terminal = terminal;
            _standardStreamWriter = standardStreamWriter;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            FlushBuffer();
            _standardStreamWriter.Dispose();
        }

        protected Terminal Terminal => _terminal;

        protected StandardStreamWriter UnderlyingStreamWriter => _standardStreamWriter;

        internal virtual bool ShouldFlushBuffer => false;

        internal virtual void FlushBuffer() { }

        public virtual void Write(string? value)
        {
            lock (_terminal.SyncRoot)
            {
                _terminal.ClearPaintedViews();

                _standardStreamWriter.Write(value);
            }
        }
    }
}
