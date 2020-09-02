using System;
using System.Buffers;
using Glacie.Data.Arc.Infrastructure;

namespace Glacie.Data.Arc
{
    internal sealed class ReadingChunkedEntryStream : ArcEntryStream
    {
        private readonly long _length;
        private readonly ArcEntryChunkCollection _chunks;
        private long _position;
        private int _chunkIndex;

        private byte[]? _buffer;
        private int _bufferLength;
        private int _bufferPosition;

        public ReadingChunkedEntryStream(ArcArchive archive,
            arc_entry_id entryId,
            long length,
            ArcEntryChunkCollection chunks)
            : base(archive, entryId)
        {
            _length = length;
            _chunks = chunks;

            _position = 0;
            _chunkIndex = 0;
            _buffer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (_buffer != null)
                        {
                            ArrayPool<byte>.Shared.Return(_buffer);
                            _buffer = null;
                        }

                        Archive.CloseReadingStream(this);
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        public override bool CanRead => true;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw Error.NotSupported();
        }

        public override int Read(Span<byte> buffer)
        {
            // TODO: (VeryLow) (ArcEntryReadChunkedStream) We can skip intermediate buffer if data can fit directly into output buffer.

            var bytesRead = 0;
            int offset = 0;
            int count = buffer.Length;

            while (bytesRead < count)
            {
                if (_bufferPosition < _bufferLength)
                {
                    var bytesInBuffer = _bufferLength - _bufferPosition;
                    var bytesToRead = Math.Min((count - bytesRead), bytesInBuffer);

                    new ReadOnlySpan<byte>(_buffer, _bufferPosition, bytesToRead)
                        .CopyTo(buffer.Slice(offset));

                    _bufferPosition += bytesToRead;
                    offset += bytesToRead;
                    bytesRead += bytesToRead;

                    if (bytesRead == count) break;
                }

                var hasNextChunk = ReadNextChunk();
                if (!hasNextChunk) break;
            }

            _position += bytesRead;
            return bytesRead;
        }

        private bool ReadNextChunk()
        {
            if (_chunkIndex < _chunks.Count)
            {
                var chunk = _chunks[_chunkIndex];
                _chunkIndex++;

                if (_buffer == null)
                {
                    _buffer = ArrayPool<byte>.Shared.Rent(chunk.Length);
                }
                else if (_buffer.Length < chunk.Length)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = ArrayPool<byte>.Shared.Rent(chunk.Length);
                }
                _bufferPosition = 0;
                _bufferLength = Archive.ReadChunk(in chunk, _buffer);
                return true;
            }
            else
            {
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null;
                }
                _bufferPosition = 0;
                _bufferLength = 0;
                return false;
            }
        }
    }
}
