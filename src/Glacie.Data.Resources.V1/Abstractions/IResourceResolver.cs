using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Data.Resources.V1
{
    public interface IResourceResolver
    {
        // TODO: IEnumerable<Resource> SelectAll(); ?

        bool TryResolve(string path, [NotNullWhen(returnValue: true)] out Resource? result);
        Resource? ResolveOrDefault(string path);
        Resource Resolve(string path);

        bool TryResolve(in Path1 path, [NotNullWhen(returnValue: true)] out Resource? result);
        Resource? ResolveOrDefault(in Path1 path);
        Resource Resolve(in Path1 path);
    }
}
