using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Glacie.Abstractions;
using Glacie.Data.Resources;
using Glacie.Logging;

namespace Glacie.Resources
{
    partial class ResourceManager
    {
        private sealed class Resolver : ResourceResolver
        {
            private ResourceManager _resourceManager;
            private bool _disposeResourceManager;

            public Resolver(ResourceManager resourceManager, bool disposeResourceManager)
            {
                _resourceManager = resourceManager;
                _disposeResourceManager = disposeResourceManager;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_disposeResourceManager)
                    {
                        var resourceManager = _resourceManager;
                        if (resourceManager != null)
                        {
                            resourceManager.Dispose();
                            _resourceManager = null!;
                        }
                    }
                }
                base.Dispose(disposing);
            }

            public override IEnumerable<Resource> SelectAll()
            {
                return _resourceManager.SelectAll();
            }

            public override Resolution<Resource> ResolveResource(Path path)
            {
                if (_resourceManager.TryGetResource(path, out var result))
                {
                    return new Resolution<Resource>(result, resolved: true);
                }
                else return default;
            }
        }
    }
}
