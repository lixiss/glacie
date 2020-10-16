using System;

namespace Glacie.Modules
{
    public sealed class TargetModule : Module, IDisposable
    {
        internal TargetModule() { }

        public void Dispose() => throw Error.NotImplemented();
    }
}
