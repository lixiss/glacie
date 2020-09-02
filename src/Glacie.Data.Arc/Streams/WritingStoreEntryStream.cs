using System;
using System.Buffers;

namespace Glacie.Data.Arc
{
    internal sealed class WritingStoreEntryStream : ArcEntryStream
    {
        private byte[]? _buffer;
        private int _bufferLength;
        private int _bufferCapacity;

        private long _position;

        public WritingStoreEntryStream(ArcArchive archive,
            arc_entry_id entryId,
            int bufferCapacity)
            : base(archive, entryId)
        {
            _buffer = null;
            _bufferCapacity = bufferCapacity;
            _bufferLength = 0;
            _position = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (_bufferLength > 0)
                        {
                            Archive.WriteStoreBytes(new ReadOnlySpan<byte>(_buffer, 0, _bufferLength), final: true);
                            _bufferLength = 0;
                        }

                        if (_buffer != null)
                        {
                            ArrayPool<byte>.Shared.Return(_buffer);
                            _buffer = null;
                        }

                        Archive.CloseWritingStream(this);
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override bool CanWrite => true;

        public override long Length => throw Error.NotSupported();

        public override long Position
        {
            get => _position;
            set => throw Error.NotSupported();
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var bytesWritten = 0;
            var totalBytesToWrite = buffer.Length;

            while (totalBytesToWrite > 0)
            {
                var input = buffer.Slice(bytesWritten);

                var bytesInBufferAvailable = _bufferCapacity - _bufferLength;

                // If we write less than buffer capacity, then buffer it.
                // If we buffer already allocated, then also fill it.
                if (totalBytesToWrite <= bytesInBufferAvailable || _bufferLength > 0)
                {
                    var bytesToWrite = Math.Min(totalBytesToWrite, _bufferCapacity - _bufferLength);

                    // Must write into buffer.
                    if (_buffer == null)
                    {
                        _buffer = ArrayPool<byte>.Shared.Rent(_bufferCapacity);
                        _bufferCapacity = _buffer.Length;
                    }

                    input.Slice(0, bytesToWrite)
                        .CopyTo(new Span<byte>(_buffer, _bufferLength, bytesToWrite));
                    _bufferLength += bytesToWrite;
                    totalBytesToWrite -= bytesToWrite;
                    bytesWritten += bytesToWrite;

                    // Flush buffer if it was filled.
                    if (_bufferCapacity - _bufferLength == 0)
                    {
                        Archive.WriteStoreBytes(new ReadOnlySpan<byte>(_buffer, 0, _bufferLength), final: false);
                        _bufferLength = 0;
                    }
                }
                else
                {
                    // Write without buffering.
                    Archive.WriteStoreBytes(input, final: false);
                    totalBytesToWrite -= buffer.Length;
                    bytesWritten += buffer.Length;
                }
            }

            _position += bytesWritten;
        }
    }
}
