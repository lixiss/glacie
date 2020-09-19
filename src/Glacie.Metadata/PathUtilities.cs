using System;

namespace Glacie.Metadata
{
    [Obsolete("Move this functionality into VirtualPath.")]
    internal static class PathUtilities
    {
        public static string TrimStartSegment(string path, string segment)
        {
            if (path.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
            {
                var segmentLength = segment.Length;
                if (path.Length > segmentLength)
                {
                    if (path[segmentLength] == '/' || path[segmentLength] == '\\')
                    {
                        // TODO: should trim multiple occurencies of '/' or '\\'
                        return path.Substring(segmentLength + 1);
                    }
                    else
                    {
                        return path.Substring(segmentLength);
                    }
                }
            }
            return path;
        }

        public static string TrimStartSegments(string path, int count)
        {
            if (count == 0) return path;

            var i = 0;
            while (i < path.Length && count > 0)
            {
                while (i < path.Length && path[i] != '/' && path[i] != '\\') i++;
                i++;
                count--;
            }

            return path.Substring(i, path.Length - i);
        }
    }
}
