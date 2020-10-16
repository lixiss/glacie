using System;

namespace Glacie
{
    [Obsolete("Remove IPathMapper, there is not needed, as this functionality should be covered by resolvers.")]
    public interface IPath1Mapper
    {
        /// <summary>
        /// Map one path onto another.
        /// Returns false if given path in not in valid format.
        /// </summary>
        bool TryMap(in Path1 path, out Path1 result);

        /// <summary>
        /// Map one path onto another.
        /// Returns <see langword="true"/> if path was mapped (changed).
        /// </summary>
        bool Map(in Path1 path, out Path1 result);

        Path1 Map(in Path1 path);
    }
}
