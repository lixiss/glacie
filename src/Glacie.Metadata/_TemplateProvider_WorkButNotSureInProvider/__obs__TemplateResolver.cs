using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Templates;

namespace Glacie.Metadata
{
    using TemplateMap = Dictionary<string, Template>;

    // TODO: create it not sealed for easy deriving

    [Obsolete("This classes seems obsolete, TemplateMetadataReader and Provider doesn't use them (however need something).")]
    public sealed class __obs__TemplateResolver
        : __obs__ITemplateResolver
        , IDisposable
    {
        private __obs__TemplateProvider _provider;
        private TemplateMap _templateMap;

        public __obs__TemplateResolver(__obs__TemplateProvider provider)
        {
            _provider = provider;
            _templateMap = new TemplateMap(StringComparer.Ordinal);
        }

        public void Dispose()
        {
            _provider?.Dispose();
            _provider = null!;
        }

        public IEnumerable<Template> SelectAll()
        {
            // TODO: logic is incorrect, because it will parse it twice even
            // when cached. To avoid this we need access to resource resolver
            // instead, and create template when need.

            foreach (var template in _provider.SelectAll())
            {
                yield return template; // CacheAndReturn(template);
            }
        }

        public bool TryResolve(in Path1 path,
            [NotNullWhen(returnValue: true)] out Template? result)
        {
            // TODO: logic is incorrect.

            if (_provider.TryGet(in path, out var x))
            {
                result = x; // CacheAndReturn(x);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public Template? ResolveOrDefault(in Path1 path)
        {
            if (TryResolve(in path, out var template)) return template;
            else return null;
        }

        public Template Resolve(in Path1 path)
        {
            if (TryResolve(in path, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Unable to resolve template \"{0}\".", path);
            }
        }
    }
}
