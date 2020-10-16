using System;

namespace Glacie.Modules
{
    public sealed class SourceModule : IDisposable
    {
        internal SourceModule() { }

        public void Dispose() => throw Error.NotImplemented();
    }
}
