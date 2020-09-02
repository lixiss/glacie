using System;
using System.Buffers;

namespace Glacie.Data.Arc
{
    internal sealed class ReadingStoreEntryStream : ArcEntryStream
    {
        private readonly long _length;
        private readonly long _offset;
        private readonly int _bufferSize;
        private long _position;
        private long _consumedLength;

        private byte[]? _buffer;
        private int _bufferLength;
        private int _bufferPosition;


        public ReadingStoreEntryStream(ArcArchive archive,
            arc_entry_id entryId,
            long length,
            int offset, int maxBufferSize)
            : base(archive, entryId)
        {
            _length = length;
            _offset = offset;
            _bufferSize = Math.Min(maxBufferSize, checked((int)_length));
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

        // TODO: (ArcEntryStream) can hold common logic
        public override int Read(Span<byte> buffer)
        {
            // TODO: (VeryLow) (ArcEntryReadStoreStream) We can skip intermediate buffer if data can fit directly into output buffer. We can read from file directly into target buffer.

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
            if (_consumedLength < _length)
            {
                if (_buffer == null)
                {
                    _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                }
                _bufferPosition = 0;
                var bytesToRead = checked((int)Math.Min(_length - _consumedLength, _buffer.Length));
                _bufferLength = Archive.ReadBytes(_buffer, checked((uint)(_offset + _consumedLength)), bytesToRead);
                _consumedLength += _bufferLength;
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
