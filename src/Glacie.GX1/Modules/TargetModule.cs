using System;

using Glacie.Data.Arz;
using Glacie.GX1.Abstractions;

namespace Glacie.GX1
{
    internal sealed class TargetModule : Module, IDisposable
    {
        public TargetModule(string? name) : base(name)
        {
        }

        public void Dispose() => DisposeInternal();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override bool IsReadOnly => false;

        public override bool IsSource => false;

        protected override ArzDatabase OpenDatabaseCore()
        {
            throw new NotImplementedException();
        }
    }
}
