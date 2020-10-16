using System;

using Glacie.Data.Arz;
using Glacie.Resources;

namespace Glacie.Modules
{
    public sealed class TargetModule : Module, IDisposable
    {
        public TargetModule()
            : base(physicalPath: null,
                  GetModuleDatabase(),
                  GetResourceBundleSet())
        { }

        public void Dispose() => DisposeInternal();

        private static ModuleDatabase GetModuleDatabase()
        {
            return new ModuleDatabase(physicalPath: null,
                database: ArzDatabase.Create(),
                disposeDatabase: true);
        }

        private static ResourceBundleSet GetResourceBundleSet()
        {
            return new ResourceBundleSet();
        }
    }
}
