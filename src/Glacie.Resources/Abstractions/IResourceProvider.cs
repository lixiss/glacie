using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using IO = System.IO;

namespace Glacie.Abstractions
{
    // TODO: There is generic resource provider
    // TODO: Should be part of Glacie.Abstractions?
    // TODO: IAssetProvider? (Source -> Assets -> Resource?)
    // "Right-click on the ‘.wrl’ file and auto-create its asset. It and the accompanying level files
    // will be automatically incorporated into a ‘.map’ asset."
    // TODO: Eventually need right types / entities.
    // For example .txt file - is asset.
    // "Database records are ways of arranging assets into objects that the player can interact with in-game."

    public interface IResourceProvider
    {
        IReadOnlyList<ResourceType> GetSupportedResourceTypes();

        bool IsResourceTypeSupported(ResourceType value);

        IEnumerable<IResource> SelectAll();

        bool TryGet(in VirtualPath name,
            [NotNullWhen(returnValue: true)] out IResource? result);

        IResource? GetOrDefault(in VirtualPath name);

        IResource Get(in VirtualPath name);

        IO.Stream Open(in VirtualPath name);

        IO.Stream Open(IResource resource);
    }
}
