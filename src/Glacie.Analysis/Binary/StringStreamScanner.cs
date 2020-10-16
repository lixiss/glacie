using System;
using System.Buffers;
using System.Buffers.Binary;

using IO = System.IO;

namespace Glacie.Analysis.Binary
{
    // TODO: Create specialized scanners and somehow mix them.


    // Scan stream for ascii string(s) and attempt to detect their format.
    // Raw, Length-Prefixed (int32/int64?), Zero-ended.
    /// <summary>
    /// Scan for strings in the binary stream.
    /// </summary>
    public sealed class StringStreamScanner : IDisposable
    {
        private const int MaxStringLength = 1024;
        private const int MinInt32LEStringLength = 4;

        private IO.Stream _stream;
        private bool _disposeStream;

        private byte[] _buffer;
        private int _bufferLength;

        private long _bufferPosition; // buffer position in stream

        private int _pos; // position in buffer

        public StringStreamScanner(IO.Stream stream, bool disposeStream = true)
        {
            _stream = stream;
            _disposeStream = disposeStream;

            _buffer = ArrayPool<byte>.Shared.Rent(256 * 1024);
            _bufferLength = 0;
        }

        public void Dispose()
        {
            if (_stream != null && _disposeStream)
            {
                _stream.Dispose();
                _stream = null!;
            }

            var buffer = _buffer;
            if (buffer != null)
            {
                _buffer = null!;
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public bool ReadNext(out StringToken result)
        {
            while (ReadBufferIfNeed(MaxStringLength, 8))
            {
                ReadOnlySpan<byte> buffer = _buffer.AsSpan().Slice(0, _bufferLength);

                //if (TryMatchInt32LEString(buffer, _pos, out result))
                //{
                //    return true;
                //}

                int pos = _pos;

                // TODO: Try read Length-prefixed strings in successive way, e.g. get length, and
                // try match it with string.

                while (pos < buffer.Length && !IsAsciiNonControlCharacter(buffer[pos])) pos++;

                if (pos == buffer.Length)
                {
                    _pos = pos;
                    continue;
                }

                // Found ascii character.
                if (pos > 4 && TryMatchInt32LEString(buffer, pos - 4, out result))
                {
                    return true;
                }

                var valueStartPos = pos;
                var streamPosition = _bufferPosition + valueStartPos;

                while (pos < buffer.Length && IsAsciiNonControlCharacter(buffer[pos])) pos++;

                var valueEndPos = pos;
                var valueLength = valueEndPos - valueStartPos;

                var tokenType = StringTokenType.RawAsciiString;

                if (valueStartPos >= 4)
                {
                    var int32PrefixedLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(valueStartPos - 4, 4));
                    if (int32PrefixedLength > 0 && int32PrefixedLength <= valueLength)
                    {
                        if (valueLength == int32PrefixedLength)
                        {
                            tokenType = StringTokenType.Int32LeleAsciiString;
                        }
                        else if (valueLength > int32PrefixedLength && ((valueLength - int32PrefixedLength) < 4))
                        {
                            tokenType = StringTokenType.Int32LeleAsciiString;

                            // adjust value length
                            valueLength = int32PrefixedLength;
                            // adjust position
                            pos = valueStartPos + valueLength;
                        }
                    }
                }

                _pos = pos;

                result = new StringToken(tokenType, streamPosition, valueLength,
                    buffer.Slice(valueStartPos, valueLength));
                return true;
            }

            result = new StringToken(StringTokenType.EndOfStream, _bufferPosition + _pos, 0, default);
            return false;
        }

        private bool TryMatchInt32LEString(ReadOnlySpan<byte> buffer, int pos, out StringToken result)
        {
            if ((_bufferLength - pos) > 4)
            {
                var int32LEPrefixedLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(pos, 4));
                if (int32LEPrefixedLength <= MaxStringLength
                    && int32LEPrefixedLength >= MinInt32LEStringLength
                    && (_bufferLength - (pos + 4)) > int32LEPrefixedLength)
                {
                    var valueSpan = buffer.Slice(pos + 4, int32LEPrefixedLength);
                    if (MatchAsciiString(valueSpan))
                    {
                        result = new StringToken(StringTokenType.Int32LeleAsciiString,
                            _bufferPosition + _pos, int32LEPrefixedLength,
                            valueSpan);
                        _pos = pos + 4 + int32LEPrefixedLength;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private bool MatchAsciiString(ReadOnlySpan<byte> buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (!IsAsciiNonControlCharacter(buffer[i])) return false;
            }
            return true;
        }


        private bool IsAsciiNonControlCharacter(byte ch)
        {
            return 0x20 <= ch && ch <= 0x7F;
        }

        private bool ReadBufferIfNeed(int minimumLengthAfterPosition, int minimumLengthBeforePosition)
        {
            var lengthAfterPosition = _bufferLength - _pos;
            var lengthBeforePosition = _pos;

            if (lengthAfterPosition > minimumLengthAfterPosition) return true;


            if (lengthAfterPosition < minimumLengthAfterPosition)
            {
                // move buffer if can
                if (_pos > minimumLengthBeforePosition)
                {
                    var length = _bufferLength - (_pos - minimumLengthBeforePosition);
                    var sourcePos = _pos - minimumLengthBeforePosition;
                    Array.Copy(_buffer, sourcePos,
                        _buffer, 0, length);
                    _bufferLength = length;
                    _pos = minimumLengthBeforePosition;
                    _bufferPosition += sourcePos;
                }
            }

            DebugCheck.That(lengthAfterPosition == _bufferLength - _pos);

            if (_bufferLength < _buffer.Length)
            {
                var freeBufferSpace = _buffer.Length - _bufferLength;
                if (freeBufferSpace > 0)
                {
                    var bytesToRead = freeBufferSpace;
                    var actualBytesRead = 0;
                    while (bytesToRead > 0)
                    {
                        var bytesRead = _stream.Read(_buffer, _bufferLength, _buffer.Length - _bufferLength);
                        if (bytesRead == 0) break;
                        bytesToRead -= bytesRead;
                        actualBytesRead += bytesRead;
                        _bufferLength += bytesRead;
                    }
                    // return bytesRead != 0;
                }
            }

            // Check.That(_bufferLength - _pos)

            //if (_pos >= _bufferLength)
            //{
            //    var bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
            //    _pos = 0;
            //    _bufferLength = bytesRead;
            //    _bufferPosition += bytesRead;
            //    return bytesRead != 0;
            //}
            //else 

            // Array.Clear(_buffer, _bufferLength, _buffer.Length - _bufferLength);

            Check.That(_bufferPosition + _bufferLength == _stream.Position);

            return _pos < _bufferLength;
        }
    }
}
