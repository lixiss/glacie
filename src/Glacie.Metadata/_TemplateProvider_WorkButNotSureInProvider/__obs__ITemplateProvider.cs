using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Templates;

namespace Glacie.Metadata
{
    internal interface __obs__ITemplateProvider
    {
        IEnumerable<Template> SelectAll();

        bool TryGet(in Path1 path,
            [NotNullWhen(returnValue: true)] out Template? result);

        Template? GetOrDefault(in Path1 path);

        Template Get(in Path1 path);
    }
}
