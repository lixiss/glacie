using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

namespace Glacie.Resources
{
    public interface IResourceResolver
    {
        IEnumerable<Resource> SelectAll();

        Resolution<Resource> ResolveResource(Path path);
        bool TryResolveResource(Path path, [NotNullWhen(returnValue: true)] out Resource? result);
    }
}
