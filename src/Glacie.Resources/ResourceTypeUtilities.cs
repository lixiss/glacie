using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Glacie.Abstractions;

namespace Glacie.Resources
{
    internal static class ResourceTypeUtilities
    {
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

        public static string GetSearchPattern(IReadOnlyList<ResourceType> resourceTypes)
        {
            if (resourceTypes.Count == 1)
            {
                return GetSearchPattern(resourceTypes[0]);
            }
            return "*.*"; // TODO: Should return any file. Does it work on posix systems?
        }
    }
}
