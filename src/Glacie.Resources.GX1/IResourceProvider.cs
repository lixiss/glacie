using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Resources
{
    /// <summary>
    /// Provide resources by their path.
    /// No additional path mapping is applied, except if underlying storage uses
    /// own access rules (like path normalization, or case sensitivity).
    /// This may end in situation, what actual name of resource which was
    /// requested is different from path, which has been used.
    /// </summary>
    public interface IResourceProvider
    {
        IEnumerable<Resource> SelectAll();

        bool TryGetResource(Path path, [NotNullWhen(returnValue: true)] out Resource? result);
        Resource GetResource(Path path);

        bool Exists(Path path);
    }
}
