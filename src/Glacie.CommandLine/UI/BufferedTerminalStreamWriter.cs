using System;
using System.Text;

using Glacie.CommandLine.IO;

namespace Glacie.CommandLine.UI
{
    internal sealed class BufferedTerminalStreamWriter : TerminalStreamWriter
    {
        private const int MaxBufferLength = 16384;

        private readonly StringBuilder _buffer;

        public BufferedTerminalStreamWriter(Terminal terminal, StandardStreamWriter standardStreamWriter)
            : base(terminal, standardStreamWriter)
        {
            _buffer = new StringBuilder();
        }

        internal override bool ShouldFlushBuffer => _buffer.Length > 0;

        internal override void FlushBuffer()
        {
            // This method called under lock.
            if (_buffer.Length > 0)
            {
                UnderlyingStreamWriter.Write(_buffer.ToString());
                _buffer.Clear();
            }
        }

        public override void Write(string? value)
        {
            lock (Terminal.SyncRoot)
            {
                if (Terminal.PaintScheduled)
                {
                    if (_buffer.Length > MaxBufferLength)
                    {
                        Terminal.ClearPaintedViews();
                        FlushBuffer();
                        UnderlyingStreamWriter.Write(value);
                    }
                    else
                    {
                        _buffer.Append(value);
                    }
                }
                else
                {
                    Terminal.ClearPaintedViews();
                    UnderlyingStreamWriter.Write(value);
                }
            }
        }
    }
}
