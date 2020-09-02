using System;
using IO = System.IO;

namespace Glacie.Data.Arc
{
    internal abstract class ArcEntryStream : IO.Stream
    {
        private ArcArchive _archive;
        private readonly arc_entry_id _entryId;
        private bool _disposed;

        protected ArcEntryStream(ArcArchive archive, arc_entry_id entryId)
        {
            Check.Argument.NotNull(archive, nameof(archive));

            _archive = archive;
            _entryId = entryId;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _archive = null!;
                }

                base.Dispose(disposing);

                _disposed = true;
            }
        }

        protected internal ArcArchive Archive => _archive;

        protected internal arc_entry_id EntryId => _entryId;

        protected internal bool Disposed => _disposed;

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw Error.ObjectDisposed(GetType().ToString());
        }

        public sealed override bool CanSeek => false;
        public override bool CanRead => false;
        public override bool CanWrite => false;

        public override int Read(Span<byte> buffer) => throw Error.NotSupported();

        public override void Write(ReadOnlySpan<byte> buffer) => throw Error.NotSupported();

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            return Read(new Span<byte>(buffer, offset, count));
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public sealed override long Seek(long offset, IO.SeekOrigin origin) => throw Error.NotSupported();

        public sealed override void SetLength(long value) => throw Error.NotSupported();

        public sealed override void Flush() { }
    }
}
