using System;
using System.Collections.Generic;

using Glacie.Abstractions;

namespace Glacie.Data.Resources.V1
{
    // TODO: Expose this, might be useful for something.
    internal static class ResourceTypeUtilities
    {
        public static ResourceType FromPath(in Path1 path)
        {
            return FromName(path.Value.AsSpan());
        }

        public static ResourceType FromName(string name)
        {
            return FromName(name.AsSpan());
        }

        public static ResourceType FromName(ReadOnlySpan<char> name)
        {
            if (name.EndsWith(".tpl", StringComparison.OrdinalIgnoreCase))
                return ResourceType.Template;

            return ResourceType.None;
        }

        public static string GetSearchPattern(ResourceType resourceType)
            => resourceType switch
            {
                ResourceType.Template => "*.tpl",
                _ => throw Error.Unreachable(),
            };

        public static bool IsDevelopment(ResourceType type)
        {
            return type == ResourceType.Template;
        }

        public static string GetSearchPattern(IReadOnlyList<ResourceType> resourceTypes)
        {
            if (resourceTypes.Count == 1)
            {
                return GetSearchPattern(resourceTypes[0]);
            }
            return "*";
        }
    }
}
