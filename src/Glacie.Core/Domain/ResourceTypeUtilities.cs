using System;
using System.Collections.Generic;
using System.Linq;

using Glacie.Abstractions;

namespace Glacie.Utilities
{
    public static class ResourceTypeUtilities
    {
        public static ResourceType FromPath(Path path)
        {
            return FromName(path.Value.AsSpan());
        }

        public static ResourceType FromName(string name)
        {
            return FromName(name.AsSpan());
        }

        public static ResourceType FromName(ReadOnlySpan<char> name)
        {
            var extension = Path.GetExtension(name);

            if (extension.Equals(".tpl", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Template;
            else if (extension.Equals(".anm", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_ANM;
            else if (extension.Equals(".msh", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_MSH;
            else if (extension.Equals(".tex", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_TEX;
            else if (extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_PFX;
            else if (extension.Equals(".fnt", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_FNT;
            else if (extension.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_TGA;
            else if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_TXT;
            else if (extension.Equals(".map", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_MAP;
            else if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_MP3;
            else if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_WAV;
            else if (extension.Equals(".qst", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_QST;
            else if (extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_BIN;
            else if (extension.Equals(".ssh", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_SSH;
            else if (extension.Equals(".cnv", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_CNV;
            else if (extension.Equals(".lua", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_LUA;
            else if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Unknown_OGG;
            else if (extension.Equals(".gxm", StringComparison.OrdinalIgnoreCase))
                return ResourceType.GlacieMetadataModule;
            else if (extension.Equals(".gxmd", StringComparison.OrdinalIgnoreCase))
                return ResourceType.GlacieMetadata;
            else if (extension.Equals(".gxmdi", StringComparison.OrdinalIgnoreCase))
                return ResourceType.GlacieMetadataInclude;
            else if (extension.Equals(".gxmp", StringComparison.OrdinalIgnoreCase))
                return ResourceType.GlacieMetadataPatch;
            else if (extension.Equals(".gxmpi", StringComparison.OrdinalIgnoreCase))
                return ResourceType.GlacieMetadataPatchInclude;
            else return ResourceType.None;
        }

        public static string GetSearchPattern(ResourceType resourceType)
            => resourceType switch
            {
                ResourceType.Template => "*.tpl",
                ResourceType.GlacieMetadataModule => "*.gxm",
                ResourceType.GlacieMetadata => "*.gxmd",
                ResourceType.GlacieMetadataInclude => "*.gxmdi",
                ResourceType.GlacieMetadataPatch => "*.gxmp",
                ResourceType.GlacieMetadataPatchInclude => "*.gxmpi",
                _ => throw Error.Unreachable(),
            };

        public static bool IsDevelopment(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Template:
                case ResourceType.GlacieMetadataModule:
                case ResourceType.GlacieMetadata:
                case ResourceType.GlacieMetadataInclude:
                case ResourceType.GlacieMetadataPatch:
                case ResourceType.GlacieMetadataPatchInclude:
                    return true;

                default:
                    return false;
            }
        }

        public static string GetSearchPattern(IReadOnlyList<ResourceType> resourceTypes)
        {
            if (resourceTypes.Count == 1)
            {
                return GetSearchPattern(resourceTypes[0]);
            }

            if (resourceTypes.Count == 2)
            {
                if (resourceTypes.Contains(ResourceType.GlacieMetadata)
                    && resourceTypes.Contains(ResourceType.GlacieMetadataInclude))
                {
                    return "*.gxmd*";
                }
                else if (resourceTypes.Contains(ResourceType.GlacieMetadataPatch)
                    && resourceTypes.Contains(ResourceType.GlacieMetadataPatchInclude))
                {
                    return "*.gxmp*";
                }
            }

            if (resourceTypes.Count == 4)
            {
                if (resourceTypes.Contains(ResourceType.GlacieMetadata)
                    && resourceTypes.Contains(ResourceType.GlacieMetadataInclude)
                    && resourceTypes.Contains(ResourceType.GlacieMetadataPatch)
                    && resourceTypes.Contains(ResourceType.GlacieMetadataPatchInclude))
                {
                    return "*.gxm*";
                }
            }

            return "*";
        }
    }
}
