using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Glacie.Data.Resources;
using Glacie.Logging;

namespace Glacie.Resources
{
    partial class ResourceManager
    {
        private sealed class PrefixRegistration
        {
            private readonly ResourceManager _manager;
            private readonly Path _name;
            private List<BundleRegistration> _bundleRegistrations;

            public PrefixRegistration(ResourceManager manager, Path prefix)
            {
                Check.Argument.NotNull(manager, nameof(manager));

                _manager = manager;
                _name = prefix;
                _bundleRegistrations = new List<BundleRegistration>();
            }

            public ResourceManager Manager => _manager;

            public Path Path => _name;

            public BundleRegistration AddBundle(string? language, string? bundleName, ushort sourceId, ResourceBundle bundle, bool disposeBundle)
            {
                var result = new BundleRegistration(this, language, bundleName, sourceId, bundle, disposeBundle);
                _bundleRegistrations.Add(result);
                return result;
            }

            public IEnumerable<BundleRegistration> SelectBundleRegistrations()
            {
                return _bundleRegistrations;
            }
        }
    }
}
