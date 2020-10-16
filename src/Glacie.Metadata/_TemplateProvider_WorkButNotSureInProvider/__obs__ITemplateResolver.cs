using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Templates;

namespace Glacie.Metadata
{
    public interface __obs__ITemplateResolver
    {
        IEnumerable<Template> SelectAll();

        bool TryResolve(in Path1 path,
            [NotNullWhen(returnValue: true)] out Template? result);

        Template? ResolveOrDefault(in Path1 path);

        Template Resolve(in Path1 path);
    }
}
