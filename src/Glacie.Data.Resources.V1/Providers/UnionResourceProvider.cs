using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Abstractions;

namespace Glacie.Data.Resources.V1.Providers
{
    // TODO: need shadowing provider too...
    using ResourceMap = Dictionary<string, Resource>;

    [Obsolete("This should be refactored to resolver in most cases.")]
    public sealed class UnionResourceProvider : ResourceProvider
    {
        private readonly ResourceProvider[] _providers;
        private ResourceMap? _resourceMap;

        public UnionResourceProvider(
            Path1Form virtualPathForm,
            IReadOnlyCollection<ResourceProvider> providers)
            : this(name: "<union>",
                  virtualBasePath: default,
                  virtualPathForm: virtualPathForm,
                  physicalBasePath: default,
                  physicalPathForm: Path1Form.Any,
                  supportedTypes: null, // TODO: create union resource types
                  providers: providers)
        {
        }

        private UnionResourceProvider(string? name,
            in Path1 virtualBasePath,
            Path1Form virtualPathForm,
            in Path1 physicalBasePath,
            Path1Form physicalPathForm,
            IEnumerable<ResourceType>? supportedTypes,
            IReadOnlyCollection<ResourceProvider> providers)
            : base(name,
                  virtualBasePath,
                  virtualPathForm,
                  physicalBasePath,
                  physicalPathForm,
                  supportedTypes)
        {
            _providers = providers.ToArray();

            // TODO: ensure what all provides work with path(s) in same form,
            // or build maps per-path form.
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var i = 0; i < _providers.Length; i++)
                {
                    _providers[i]?.Dispose();
                    _providers[i] = null!;
                }
            }
            base.Dispose(disposing);
        }

        public override IEnumerable<Resource> SelectAll()
        {
            return GetResourceMap().Values;
        }

        public override bool TryGetByPhysicalPath(string physicalPath, [NotNullWhen(true)] out Resource? result)
        {
            var key = Path1.From(physicalPath).ToForm(VirtualPathForm);
            var resourceMap = GetResourceMap();
            return resourceMap.TryGetValue(key.Value, out result);
        }

        private ResourceMap GetResourceMap()
        {
            if (_resourceMap != null) return _resourceMap;
            else return (_resourceMap = CreateResourceMap());
        }

        private ResourceMap CreateResourceMap()
        {
            var resourceMap = new ResourceMap(StringComparer.Ordinal);

            foreach (var provider in _providers)
            {
                foreach (var resource in provider.SelectAll())
                {
                    DebugCheck.That(resource.VirtualPath.Form == VirtualPathForm);
                    var key = resource.VirtualPath.ToForm(VirtualPathForm);
                    DebugCheck.That(key.Form == VirtualPathForm);
                    DebugCheck.That(key.Value == resource.VirtualPath.Value);

                    if (!resourceMap.TryGetValue(key.Value, out var previousResource))
                    {
                        resourceMap.Add(key.Value, resource);
                    }
                    else
                    {
                        throw Error.InvalidOperation("Attept to overriding resource \"{0}\" ({1}) by ({2})",
                            resource.VirtualPath,
                            previousResource.Provider.Name,
                            resource.Provider.Name
                            );
                        // resourceMap[resource.Name] = resource;
                    }
                }
            }

            return resourceMap;
        }
    }
}
