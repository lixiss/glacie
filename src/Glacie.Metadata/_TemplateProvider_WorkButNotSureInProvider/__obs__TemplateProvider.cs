using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;
using Glacie.Data.Templates;
using Glacie.Resources;

namespace Glacie.Metadata
{
    // TODO: simplify this shit. TemplateProvider should only do typed resource access
    // and not perform path mapping.

    public sealed class __obs__TemplateProvider
        : __obs__ITemplateProvider
        , IDisposable
    {
        private ResourceProvider _provider;
        private bool _disposeProvider;
        private readonly TemplateReader _templateReader;

        public __obs__TemplateProvider(ResourceProvider provider, bool disposeProvider)
        {
            Check.Argument.NotNull(provider, nameof(provider));

            _provider = provider;
            _disposeProvider = disposeProvider;
            _templateReader = new TemplateReader();
        }

        public void Dispose()
        {
            if (_disposeProvider)
            {
                _provider?.Dispose();
                _provider = null!;
            }
        }

        public IEnumerable<Template> SelectAll()
        {
            foreach (var resource in _provider.SelectAll()) // TODO: Provider should be able to request resources by type.
            {
                if (TryGetFromResource(resource, out var template))
                {
                    yield return template;
                }
            }
        }

        public bool TryGet(in Path1 path, [NotNullWhen(true)] out Template? result)
        {
            if (_provider.TryGetResource(path.ToString(), out var resource))
            {
                return TryGetFromResource(resource, out result);
            }
            else
            {
                result = null;
                return false;
            }
        }

        public Template? GetOrDefault(in Path1 path)
        {
            if (TryGet(in path, out var result)) return result;
            else return null;
        }

        public Template Get(in Path1 path)
        {
            if (TryGet(in path, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Unable to find template \"{0}\".", path);
            }
        }

        private bool TryGetFromResource(Resource resource,
            [NotNullWhen(returnValue: true)] out Template? result)
        {
            if (resource.Type != ResourceType.Template)
            {
                result = null;
                return false;
            }

            using var stream = resource.Open();
            result = _templateReader.Read(stream, Path1.From(resource.Path.ToString()));
            return true;
        }
    }
}
