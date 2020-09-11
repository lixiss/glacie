using System;
using System.IO;

namespace Glacie.CommandLine.IO
{
    internal sealed class StandardStreamWriter : IStandardStreamWriter, IDisposable
    {
        private readonly TextWriter _writer;

        public StandardStreamWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void Write(string? value)
        {
            _writer.Write(value);
        }
    }
}
