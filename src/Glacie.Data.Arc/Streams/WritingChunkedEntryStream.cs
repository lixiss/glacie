using System;
using System.Buffers;
using Glacie.Data.Compression;

namespace Glacie.Data.Arc
{
    internal sealed class WritingChunkedEntryStream : ArcEntryStream
    {
        private byte[]? _buffer;
        private int _bufferLength;
        private readonly int _chunkLength;

        private long _position;

        private readonly CompressionLevel _compressionLevel;

        public WritingChunkedEntryStream(ArcArchive archive,
            arc_entry_id entryId,
            int chunkLength,
            CompressionLevel compressionLevel)
            : base(archive, entryId)
        {
            _buffer = null;
            _bufferLength = 0;
            _chunkLength = chunkLength;
            _position = 0;
            _compressionLevel = compressionLevel;
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
                            Archive.WriteChunkBytes(new ReadOnlySpan<byte>(_buffer, 0, _bufferLength), _compressionLevel, true);
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

                // Flush buffer if it was filled.
                if (_chunkLength - _bufferLength == 0)
                {
                    Archive.WriteChunkBytes(_buffer, _compressionLevel, false);
                    _bufferLength = 0;
                }

                var bytesInBufferAvailable = _chunkLength - _bufferLength;

                // If we write less than buffer capacity, then buffer it.
                // If buffer already has data, then also fill it.
                if (totalBytesToWrite < bytesInBufferAvailable || _bufferLength > 0)
                {
                    var bytesToWrite = Math.Min(totalBytesToWrite, _chunkLength - _bufferLength);

                    // Must write into buffer.
                    if (_buffer == null)
                    {
                        _buffer = ArrayPool<byte>.Shared.Rent(_chunkLength);
                        // Do not update chunk length.
                        // _chunkLength = _buffer.Length;
                    }

                    input.Slice(0, bytesToWrite)
                        .CopyTo(new Span<byte>(_buffer, _bufferLength, bytesToWrite));
                    _bufferLength += bytesToWrite;
                    totalBytesToWrite -= bytesToWrite;
                    bytesWritten += bytesToWrite;
                }
                else
                {
                    // Write without buffering, but preserving chunk length.
                    var bytesToWrite = Math.Min(input.Length, _chunkLength);
                    Archive.WriteChunkBytes(input.Slice(0, bytesToWrite), _compressionLevel, false);
                    totalBytesToWrite -= bytesToWrite;
                    bytesWritten += bytesToWrite;
                }
            }

            _position += bytesWritten;
        }
    }
}
