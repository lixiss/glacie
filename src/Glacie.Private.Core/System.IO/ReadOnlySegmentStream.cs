using System;
using Glacie;

namespace System.IO
{
    /// <summary>
    /// Allow read up to specified numbed of bytes from underlying stream.
    /// </summary>
    public sealed class ReadOnlySegmentStream : Stream
    {
        private readonly Stream _stream;
        private int _bytesToRead;

        public ReadOnlySegmentStream(Stream stream, int bytesToRead)
        {
            _stream = stream;
            _bytesToRead = bytesToRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToRead = Math.Min(_bytesToRead, count);
            var bytesRead = _stream.Read(buffer, offset, bytesToRead);
            _bytesToRead -= bytesRead;
            return bytesRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw Error.NotSupported();

        public override long Position { get => throw Error.NotSupported(); set => throw Error.NotSupported(); }

        public override void Flush()
        {
            throw Error.NotSupported();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw Error.NotSupported();
        }

        public override void SetLength(long value)
        {
            throw Error.NotSupported();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw Error.NotSupported();
        }
    }
}
