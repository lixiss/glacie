using System;
using System.Collections.Generic;

using Glacie.Localization;
using Glacie.Resources;

namespace Glacie.Modules
{
    // TODO: Should it be public or internal?
    public sealed class SourceModule : Module, IDisposable
    {
        internal SourceModule(string? physicalPath,
            ModuleDatabase? database,
            ResourceBundleSet? resourceBundleSet)
            : base(physicalPath, database, resourceBundleSet)
        {
        }

        public void Dispose() => DisposeInternal();
    }
}
