using IO = System.IO;

namespace Glacie.Data.Resources.V1
{
    internal interface IResource
    {
        /// <summary>Resource name: string representation of <see cref="VirtualPath"/>.</summary>
        string Name { get; }

        ref readonly Path1 VirtualPath { get; }

        /// <summary>
        /// Physical resource path, which can be used to access resource
        /// by provider which generated this resource.
        /// </summary>
        string PhysicalPath { get; }

        ResourceType Type { get; }

        /// <summary>
        /// Development-time resource.
        /// For example <see cref="ResourceType.Template"/> resource is one of such resource.
        /// </summary>
        bool Development { get; }

        IResourceProvider Provider { get; }

        // TODO: fully qualified location (for diagnostics) (e.g. archive / archive path / file system path)

        IO.Stream Open();
    }
}
