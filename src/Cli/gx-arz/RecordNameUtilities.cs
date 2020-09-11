using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Glacie.Cli.Arz
{
    internal static class RecordNameUtilities
    {
        public static void Validate(string value)
        {
            // TODO: use
            // Path.GetInvalidFileNameChars()
            // Path.GetInvalidPathChars()
            // and validate over them too...

            if (!value.EndsWith(".dbr"))
            {
                throw Error.InvalidOperation("Record name must have .dbr extension.");
            }
            else if (value.StartsWith("/") || value.StartsWith("\\"))
            {
                throw Error.InvalidOperation("Entry name must not be rooted. You should specify relative-to option.");
            }
            else if (value.StartsWith("./") || value.StartsWith(".\\"))
            {
                throw Error.InvalidOperation("Entry name is relative.");
            }
            else if (value.StartsWith("../") || value.StartsWith("..\\"))
            {
                throw Error.InvalidOperation("Entry name is relative.");
            }
            else if (value.Contains("/./") || value.Contains("\\.\\"))
            {
                throw Error.InvalidOperation("Entry name contains relative segment.");
            }
            else if (value.Contains("/../") || value.Contains("\\..\\"))
            {
                throw Error.InvalidOperation("Entry name contains relative segment.");
            }
            else if (value.EndsWith("/.") || value.EndsWith("\\."))
            {
                throw Error.InvalidOperation("Entry name ends with special name.");
            }
            else if (value.EndsWith("/..") || value.EndsWith("\\.."))
            {
                throw Error.InvalidOperation("Entry name ends with special name.");
            }
            else if (Path.IsPathFullyQualified(value))
            {
                throw Error.InvalidOperation("Entry name must not be fully qualified. You should specify relative-to option.");
            }
            else if (Path.IsPathRooted(value))
            {
                throw Error.InvalidOperation("Entry name must not be rooted. You should specify relative-to option.");
            }
        }

        internal static string NormalizeToFileSystemPath(string name)
        {
            // Replace any back slashes with forward slashes: windows support
            // both slashes, but non-windows systems uses only forward slashes.
            return name.Replace('\\', '/');
        }
    }
}
