using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Data.Resources.Providers
{
    public sealed class ZipArchiveBundle : ResourceBundle
    {
        private ZipArchive? _archive;
        private readonly bool _disposeArchive;

        public ZipArchiveBundle(
            string? name,
            string? physicalPath,
            IEnumerable<ResourceType>? supportedResourceTypes,
            ZipArchive archive,
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

            foreach (var archiveEntry in _archive!.Entries)
            {
                var archiveEntryName = archiveEntry.FullName;
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
                return _archive!.GetEntry(path) != null;
            }
            else return false;
        }

        public override IO.Stream Open(string path)
        {
            ThrowIfDisposed();

            ThrowIfPathNotSupported(path);

            var archiveEntry = _archive!.GetEntry(path);
            if (archiveEntry != null)
            {
                return archiveEntry.Open();
            }
            else
            {
                // TODO: Throw right exception
                throw Error.InvalidOperation("Resource \"{0}\" in bundle not found.", path);
            }
        }

        private static string? GetPhysicalPath(string? physicalPath, ZipArchive archive)
        {
            if (physicalPath != null) return physicalPath;
            return null;
        }
    }
}
