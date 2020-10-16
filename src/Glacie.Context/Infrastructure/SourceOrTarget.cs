using System;
using Glacie.Data.Arz;

namespace Glacie.Infrastructure
{
    internal abstract class SourceOrTarget : IDisposable, IDatabaseProvider
    {
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public abstract int Identifier { get; }

        protected abstract void Dispose(bool disposing);

        public abstract bool CanProvideDatabase { get; }

        public abstract ArzDatabase GetDatabase();
    }
}
