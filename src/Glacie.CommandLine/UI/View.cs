using System;

namespace Glacie.CommandLine.UI
{
    public abstract class View : IDisposable
    {
        private Terminal? _terminal;

        protected View() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        internal Terminal? Terminal => _terminal;

        internal void Attach(Terminal terminal)
        {
            Check.That(_terminal == null);
            _terminal = terminal;
        }

        internal void Detach()
        {
            Check.That(_terminal != null);
            _terminal = null;
        }

        public virtual int Width { get; set; }

        public bool Visible { get; set; } = true;

        public virtual int Height => 1;

        internal string[] Render() => OnRender();

        protected abstract string[] OnRender();
    }
}
