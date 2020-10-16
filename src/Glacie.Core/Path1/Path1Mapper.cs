using System;

namespace Glacie
{
    [Obsolete("Remove IPathMapper, there is not needed, as this functionality should be covered by resolvers.")]
    public abstract class Path1Mapper : IPath1Mapper
    {
        public static Path1Mapper Default => DefaultPathMapper.Instance;

        public static Path1Mapper CreateFor(Path1Form form)
        {
            return new PathFormMapper(form);
        }

        /// <summary>
        /// Map one path onto another.
        /// Returns false if given path in not in valid format.
        /// </summary>
        public abstract bool TryMap(in Path1 path, out Path1 result);

        /// <summary>
        /// Map one path onto another.
        /// Returns <see langword="true"/> if path was mapped (changed).
        /// </summary>
        public virtual bool Map(in Path1 path, out Path1 result)
        {
            if (TryMap(in path, out result))
            {
                return (object?)result.Value != path.Value;
            }

            throw Error.InvalidOperation("Failed to map path \"{0}\".", path.Value);
        }

        public virtual Path1 Map(in Path1 path)
        {
            if (Map(in path, out var result)) return result;
            else return path;
        }

        private sealed class DefaultPathMapper : Path1Mapper
        {
            private static readonly DefaultPathMapper s_instance = new DefaultPathMapper();

            public static Path1Mapper Instance => s_instance;

            public override bool TryMap(in Path1 path, out Path1 result)
            {
                result = path;
                return true;
            }

            public override bool Map(in Path1 path, out Path1 result)
            {
                result = path;
                return false;
            }
        }

        private sealed class PathFormMapper : Path1Mapper
        {
            private readonly Path1Form _form;

            public PathFormMapper(Path1Form form)
            {
                _form = form;
            }

            public override bool TryMap(in Path1 path, out Path1 result)
            {
                result = path.ToForm(_form);
                return result.Form == _form;
            }

            public override bool Map(in Path1 path, out Path1 result)
            {
                if (TryMap(in path, out result))
                {
                    return (object?)result.Value != path.Value;
                }

                throw Error.InvalidOperation("Failed to map path \"{0}\" to the form \"{1}\".", path.Value, _form);
            }
        }
    }
}
