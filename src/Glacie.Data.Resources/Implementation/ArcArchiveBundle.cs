using System.Collections.Generic;

using Glacie.Abstractions;
using Glacie.Data.Arc;

using IO = System.IO;

namespace Glacie.Data.Resources.Providers
{
    public sealed class ArcArchiveBundle : ResourceBundle
    {
        private ArcArchive? _archive;
        private readonly bool _disposeArchive;

        public ArcArchiveBundle(
            string? name,
            string? physicalPath,
            IEnumerable<ResourceType>? supportedResourceTypes,
            ArcArchive archive,
            bool disposeArchive)
            : base(name,
                  GetPhysicalPath(physicalPath, archive),
                  supportedResourceTypes)
        {
            Check.Argument.NotNull(archive, nameof(archive));
            _archive = archive;
            _disposeArchive = disposeArchive;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!Disposed)
                {
                    if (_disposeArchive)
                    {
                        _archive?.Dispose();
                        _archive = null;
                    }
                }
            }
            base.Dispose(disposing);
        }


        public override IEnumerable<string> SelectAll()
        {
            ThrowIfDisposed();

            foreach (var archiveEntry in _archive!.SelectAll())
            {
                var archiveEntryName = archiveEntry.Name;
                if (IsPathSupported(archiveEntryName))
                {
                    yield return archiveEntryName;
                }
            }
        }

        public override bool Exists(string path)
        {
            ThrowIfDisposed();

            if (IsPathSupported(path))
            {
                return _archive!.TryGet(path, out var _);
            }
            else return false;
        }

        public override IO.Stream Open(string path)
        {
            ThrowIfDisposed();

            ThrowIfPathNotSupported(path);

            if (_archive!.TryGet(path, out var archiveEntry))
            {
                return archiveEntry.Open();
            }
            else
            {
                // TODO: Throw right exception
                throw Error.InvalidOperation("Resource \"{0}\" in bundle not found.", path);
            }
        }

        private static string? GetPhysicalPath(string? physicalPath, ArcArchive archive)
        {
            if (physicalPath != null) return physicalPath;
            // TODO: (Low) (ArcArchiveBundle) get physical path from ArcArchive instance directly.
            return null;
        }
    }
}
