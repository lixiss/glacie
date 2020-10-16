using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Utilities;

using IO = System.IO;

namespace Glacie.Data.Resources.Providers
{
    public sealed class FileSystemBundle : ResourceBundle
    {
        public FileSystemBundle(
            string? name,
            string physicalPath,
            IEnumerable<ResourceType>? supportedResourceTypes)
            : base(name,
                  GetPhysicalPath(physicalPath),
                  supportedResourceTypes)
        {
            Check.Argument.NotNull(physicalPath, nameof(physicalPath));

            // Ensure what given physicalPath is not resolves into file,
            // but directory (might not necessary to exist) is allowed.
            if (IO.File.Exists(PhysicalBasePath))
            {
                throw Error.Argument(nameof(physicalPath), "Given argument should be directory, but file found: \"{0}\".", physicalPath);
            }
        }

        private string PhysicalBasePath => PhysicalPath!;

        public override IEnumerable<string> SelectAll()
        {
            ThrowIfDisposed();

            var basePath = PhysicalBasePath;

            if (IO.Directory.Exists(basePath))
            {
                return SelectAllInternal(basePath);
            }

            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> SelectAllInternal(string basePath)
        {
            var searchPattern = ResourceTypeUtilities.GetSearchPattern(GetSupportedResourceTypes());

            var files = IO.Directory.EnumerateFiles(basePath, searchPattern, IO.SearchOption.AllDirectories);
            foreach (var fullPath in files)
            {
                DebugCheck.That(IO.Path.IsPathFullyQualified(fullPath));

                if (IsPathSupported(fullPath))
                {
                    var name = IO.Path.GetRelativePath(PhysicalBasePath, fullPath);
                    yield return name;
                }
            }
        }

        public override bool Exists(string path)
        {
            ThrowIfDisposed();

            if (!IsPathSupported(path))
            {
                return false;
            }

            if (TryMapToPhysicalPath(path, out var physicalPath))
            {
                return IO.File.Exists(physicalPath);
            }
            else return false;
        }

        public override IO.Stream Open(string path)
        {
            ThrowIfDisposed();

            ThrowIfPathNotSupported(path);

            if (TryMapToPhysicalPath(path, out var physicalPath))
            {
                return IO.File.OpenRead(physicalPath);
            }
            else
            {
                // TODO: Throw right exception
                throw Error.InvalidOperation("Resource \"{0}\" in bundle not found.", path);
            }
        }

        private bool TryMapToPhysicalPath(string path,
            [NotNullWhen(returnValue: true)] out string? physicalPath)
        {
            // Ensure what path is correct, and can be safely combined.
            // (Relative | Strict | Normalized) doesn't allow to reference anything
            // out of base path scope.
            const Path1Form requiredForm = Path1Form.Relative | Path1Form.Strict | Path1Form.Normalized;
            var normalizedPath = Path1.From(path).ToForm(requiredForm);
            if (!normalizedPath.IsInForm(requiredForm))
            {
                physicalPath = null;
                return false;
            }

            physicalPath = IO.Path.GetFullPath(IO.Path.Combine(PhysicalBasePath, path));
            return true;
        }

        private static string GetPhysicalPath(string physicalPath)
        {
            return IO.Path.GetFullPath(physicalPath);
        }
    }
}
