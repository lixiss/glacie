using System;

using Glacie.Data.Arz;
using Glacie.GX1.Abstractions;

namespace Glacie.GX1
{
    // TODO: probably should be internal
    public sealed class SourceModule : Module, IDisposable
    {
        // TODO: probably should be always created via factory...
        public SourceModule(string? name) : base(name)
        {
        }

        public void Dispose() => DisposeInternal();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override bool IsReadOnly => true;

        public override bool IsSource => true;

        protected override ArzDatabase OpenDatabaseCore()
        {
            throw new NotImplementedException();
        }
    }
}
