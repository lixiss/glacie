using IO = System.IO;

namespace Glacie
{
    internal static class PathUtilities
    {
        public static string GetPhysicalPath(string path)
        {
            return IO.Path.GetFullPath(path);
        }

        public static string GetRelativePath(string? physicalPath, string path)
        {
            if (string.IsNullOrEmpty(physicalPath))
            {
                return GetPhysicalPath(path);
            }

            return IO.Path.GetRelativePath(physicalPath, path);
        }
    }
}
