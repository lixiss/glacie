using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Localization;
using Glacie.Modules;

namespace Glacie.Resources
{
    public interface IResourceBundleSet
    {
        /// <summary>
        /// Return total number of resource bundles in set.
        /// </summary>
        int Count { get; }

        IReadOnlyCollection<LanguageSymbol> Languages { get; }

        IEnumerable<IResourceBundleInfo> SelectAll();

        IReadOnlyCollection<IResourceBundleInfo> Select(LanguageSymbol language);
    }
}
