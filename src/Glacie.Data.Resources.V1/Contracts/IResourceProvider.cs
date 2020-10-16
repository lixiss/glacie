using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// using IO = System.IO;

namespace Glacie.Data.Resources.V1
{
    // TODO: Should be part of Glacie.Abstractions?
    // TODO: IAssetProvider / IResourceProvider? (Source -> Assets -> Resource?)
    // "Right-click on the ‘.wrl’ file and auto-create its asset. It and the accompanying level files
    // will be automatically incorporated into a ‘.map’ asset."
    // TODO: Eventually need right types / entities.
    // For example .txt file - is asset, and there is exist text tags and texts (GD).
    // "Database records are ways of arranging assets into objects that the player can interact with in-game."

    /// <summary>
    /// Provides access to resources in unified way.
    /// </summary>
    internal interface IResourceProvider
    {
        /// <summary>Name of this resource provider. Only for informational purposes.</summary>
        string? Name { get; }

        /// <summary>
        /// Virtual base path. Resources provided by this resource provider
        /// will have it as prefix in <see cref="IResource.VirtualPath"/>.
        /// </summary>
        ref readonly Path1 VirtualBasePath { get; }

        Path1Form VirtualPathForm { get; }

        /// <summary>
        /// Physical base path. Usually empty for archives.
        /// Might be non-empty for file system.
        /// </summary>
        ref readonly Path1 InternalBasePath { get; }

        Path1Form InternalPathForm { get; }

        IEnumerable<Resource> SelectAll();

        /// <summary>
        /// Returns <see langword="false"/> if resource not found or <paramref name="physicalPath"/> is invalid.
        /// </summary>
        bool TryGetByPhysicalPath(string physicalPath,
            [NotNullWhen(returnValue: true)] out Resource? result);

        Resource? GetByPhysicalPathOrDefault(string physicalPath);

        Resource GetByPhysicalPath(string physicalPath);

        IReadOnlyList<ResourceType> GetSupportedTypes();
        bool IsSupported(ResourceType value);
    }
}
