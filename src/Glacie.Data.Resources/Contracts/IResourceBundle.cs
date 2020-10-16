using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources // Put into Glacie.Resources?
{
    /// <summary>
    /// Provides access to resources in unified way.
    /// Resource names here are in the scope of bundle.
    /// </summary>
    internal interface IResourceBundle
    {
        /// <summary>
        /// Name of this resource bundle, for informational purposes.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Fully qualified physical path for current resource bundle.
        /// Might be null or empty if there is in-memory bundle.
        /// </summary>
        string? PhysicalPath { get; }

        /// <summary>
        /// Resource bundle can be configured to handle only specific resource types.
        /// </summary>
        bool IsResourceTypeSupported(ResourceType type);
        IReadOnlyList<ResourceType> GetSupportedResourceTypes();

        // TODO: IReadOnlyList<ResourceType> ResourceTypes { get; }
        /// <summary>
        /// Returns all resource keys. This keys can be treated as unique in the
        /// bundle, but they might not be varied depending on file system.
        /// </summary>
        IEnumerable<string> SelectAll();

        bool Exists(string path);

        /// <summary>
        /// Open resource for reading.
        /// </summary>
        IO.Stream Open(string path);

        // TODO: GetTimestamp(string path);
        // TODO: GetLastWriteTimeUtc(string path);
    }
}
