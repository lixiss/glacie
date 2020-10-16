using System.Collections.Generic;
using System.Collections.Immutable;

using Glacie.Diagnostics;
using Glacie.Localization;
using Glacie.Resources;

namespace Glacie.Modules
{
    public interface IModuleInfo
    {
        string? PhysicalPath { get; }

        // TODO: (Module) Add module name  string? Name { get; }


        bool HasDatabase { get; }

        IDatabaseInfo? DatabaseInfo { get; }

        IResourceBundleSet ResourceSet { get; }

        //IReadOnlyCollection<LanguageSymbol> Languages { get; }
        //IReadOnlyCollection<IResourceBundleInfo> GetResourceBundles(LanguageSymbol languageSymbol);

        ImmutableArray<Diagnostic> GetDiagnostics();
    }
}
