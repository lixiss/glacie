using System;

namespace Glacie.CommandLine.IO
{
    using SC = System.Console;

    public sealed class SystemConsole : IConsole, IDisposable
    {
        private readonly StandardStreamWriter _out;
        private readonly StandardStreamWriter _error;
        private readonly bool _isOutputRedirected;
        private readonly bool _isErrorRedirected;
        private readonly bool _isInputRedirected;

        public SystemConsole()
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
