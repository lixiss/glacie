using System;

using Glacie.CommandLine.IO;

namespace Glacie.CommandLine.Rendering
{
    using SC = System.Console;

    // TODO: Terminal may wrap IConsole, but this will be unfair, because 
    // terminal perform operations over system/platform console, so it is can't
    // be bound to abstract console (there is no sesne to do what.) Instead
    // it should be multiple Terminals for different output modes.
    internal sealed class Terminal : ITerminal, IDisposable
    {
        private readonly StandardStreamWriter _out;
        private readonly StandardStreamWriter _error;
        private readonly bool _isOutputRedirected;
        private readonly bool _isErrorRedirected;
        private readonly bool _isInputRedirected;

        public Terminal()
        {
            _out = new StandardStreamWriter(SC.Out);
            _error = new StandardStreamWriter(SC.Error);
            _isOutputRedirected = SC.IsOutputRedirected;
            _isErrorRedirected = SC.IsErrorRedirected;
            _isInputRedirected = SC.IsInputRedirected;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _out.Dispose();
                _error.Dispose();
            }
        }

        #region IConsole

        public IStandardStreamWriter Out => _out;

        public bool IsOutputRedirected => _isOutputRedirected;

        public IStandardStreamWriter Error => _error;

        public bool IsErrorRedirected => _isErrorRedirected;

        public bool IsInputRedirected => _isInputRedirected;

        #endregion
    }
}
