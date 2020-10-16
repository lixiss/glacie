using System;
using System.Buffers;

using Glacie.Collections;

using IO = System.IO;

namespace Glacie.Data.Templates
{
    internal struct TokenReader : IDisposable
    {
        private IO.TextReader _reader;
        private bool _endOfStream;

        private char[] _buffer;
        private int _pos;
        private int _epos;

        private int _lineNumber;
        private int _linePosition;

        private int _maximumTokenLenght;

        public TokenReader(IO.TextReader reader, int bufferSize = 4096, int maximumTokenLenght = 16 * 1024)
        {
            Check.Argument.NotNull(reader, nameof(reader));
            Check.That(bufferSize > 0);
            Check.That(maximumTokenLenght > 0);
            if (maximumTokenLenght < bufferSize) maximumTokenLenght = bufferSize;

            _reader = reader;
            _endOfStream = false;

            _buffer = AllocateBuffer(bufferSize);
            _pos = 0;
            _epos = 0;

            _lineNumber = 1;
            _linePosition = 1;

            _maximumTokenLenght = maximumTokenLenght;
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null!;

            if (_buffer != null)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = null!;
            }
        }

        public bool Disposed => _buffer == null;

        internal int BufferLength => _buffer.Length;

        private static char[] AllocateBuffer(int minimumLength)
        {
            return ArrayPool<char>.Shared.Rent(minimumLength);
        }

        private void ResizeBuffer()
        {
            DebugCheck.That(!Disposed);
            DebugCheck.That(_buffer.Length > 0);

            var newBufferLength = checked(_buffer.Length * 2);

            var newBuffer = ArrayPool<char>.Shared.Rent(newBufferLength);
            Array.Copy(_buffer, newBuffer, _buffer.Length);

            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        public Token Read()
        {
            // Eat leading whitespace.
            while (true)
            {
                if (_pos < _epos)
                {
                    EatWhitespace();
                }

                if (_pos < _epos)
                {
                    break;
                }
                else
                {
                    var charsRead = ReadBuffer(0, _buffer.Length);
                    _pos = 0;
                    _epos = charsRead;

                    if (charsRead == 0)
                    {
                        _endOfStream = true;
                        return CreateToken(TokenType.EndOfStream, 0);
                    }
                }
            }

            DebugCheck.That(_pos < _epos);
            DebugCheck.That(!char.IsWhiteSpace(_buffer[_pos]));

            // Read Token
            var pos = _pos;
            var startPos = pos;
            var lpos = pos;

            if (pos < _epos)
            {
                var ch = _buffer[pos];
                if (ch == '=')
                {
                    pos++;
                    _pos = pos;
                    return CreateToken(TokenType.Assignment, pos - lpos);
                }
                else if (ch == '{')
                {
                    pos++;
                    _pos = pos;
                    return CreateToken(TokenType.BeginBlock, pos - lpos);
                }
                else if (ch == '}')
                {
                    pos++;
                    _pos = pos;
                    return CreateToken(TokenType.EndBlock, pos - lpos);
                }
                else if (ch == '"')
                {
                    return ReadStringLiteral(pos);
                }
                else
                {
                    pos++;
                }
            }

        // Consume token characters
        readChunk:
            while (pos < _epos)
            {
                var ch = _buffer[pos];

                if (IsTokenDelimiter(ch))
                {
                    break;
                }

                pos++;
            }

            if (pos < _epos
                || _endOfStream
                || ((pos - startPos) >= _maximumTokenLenght))
            {
                _pos = pos;

                var tokenSpan = new ReadOnlySpan<char>(_buffer, startPos, pos - startPos);

                switch (tokenSpan.Length)
                {
                    case 4:
                        switch (tokenSpan[0])
                        {
                            case 'n':
                            case 'N':
                                if (tokenSpan.CompareTo("name", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return CreateToken(TokenType.Name, pos - lpos);
                                }
                                break;

                            case 't':
                            case 'T':
                                if (tokenSpan.CompareTo("type", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return CreateToken(TokenType.Type, pos - lpos);
                                }
                                break;
                        }
                        break;

                    case 5:
                        switch (tokenSpan[0])
                        {
                            case 'g':
                            case 'G':
                                if (tokenSpan.CompareTo("group", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return CreateToken(TokenType.Group, pos - lpos);
                                }
                                break;

                            case 'c':
                            case 'C':
                                if (tokenSpan.CompareTo("class", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return CreateToken(TokenType.Class, pos - lpos);
                                }
                                break;

                            case 'v':
                            case 'V':
                                if (tokenSpan.CompareTo("value", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return CreateToken(TokenType.Value, pos - lpos);
                                }
                                break;
                        }
                        break;

                    case 8:
                        if (tokenSpan.CompareTo("variable", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return CreateToken(TokenType.Variable, pos - lpos);
                        }
                        break;

                    case 11:
                        if (tokenSpan.CompareTo("description", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return CreateToken(TokenType.Description, pos - lpos);
                        }
                        break;

                    case 12:
                        if (tokenSpan.CompareTo("defaultValue", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return CreateToken(TokenType.DefaultValue, pos - lpos);
                        }
                        break;

                    case 20:
                        if (tokenSpan.CompareTo("fileNameHistoryEntry", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return CreateToken(TokenType.FileNameHistoryEntry, pos - lpos);
                        }
                        break;
                }

                return CreateToken(TokenType.Unknown, 0, pos - lpos, startPos, pos - startPos);
            }
            else
            {
                ReadNextBufferChunk(ref startPos, ref pos);
                goto readChunk;
            }
        }

        private Token ReadStringLiteral(int pos)
        {
            var ch = _buffer[pos];
            if (ch == '"')
            {
                return ReadStringLiteralInternal(pos + 1);
            }
            else throw Error.Unreachable();
        }

        private Token ReadStringLiteralInternal(int pos)
        {
            var initialLinePosition = _linePosition;
            var startPos = pos;
            var lpos = pos - 1;
            var advanceLineNumberBy = 0;

        readChunk:
            while (pos < _epos)
            {
                var ch = _buffer[pos];
                if (ch == '\n')
                {
                    pos++;
                    advanceLineNumberBy++;
                    lpos = pos;
                }
                else if (ch == '"')
                {
                    var endPos = pos;
                    pos++;
                    _pos = pos;
                    return CreateToken(TokenType.StringLiteral,
                        advanceLineNumberBy,
                        (pos - lpos) - 1 + initialLinePosition,
                        startPos, endPos - startPos);
                }
                else
                {
                    pos++;
                }
            }

            if (pos >= _epos && _endOfStream)
            {
                _pos = pos;
                return CreateToken(TokenType.UnexpectedEndOfStreamInStringLiteral,
                    advanceLineNumberBy,
                    (pos - lpos) - 1 + initialLinePosition);
            }

            // extend buffer if need
            ReadNextBufferChunk(ref startPos, ref pos);
            goto readChunk;
        }

        private void ReadNextBufferChunk(ref int startPos, ref int pos)
        {
            if (startPos > 0)
            {
                Array.Copy(_buffer, startPos, _buffer, 0, _epos - startPos);
                pos -= startPos;
                _epos -= startPos;
                startPos = 0;

                // read buffer to the end
                var charsRead = ReadBuffer(_epos, _buffer.Length - _epos);
                _epos += charsRead;
                _endOfStream = charsRead == 0;
            }
            else
            {
                DebugCheck.That(startPos == 0);
                DebugCheck.That(pos == _epos);

                ResizeBuffer();

                var charsRead = ReadBuffer(_epos, _buffer.Length - _epos);
                _epos += charsRead;
                _endOfStream = charsRead == 0;
            }
        }

        public string GetTokenValue(in Token token, IStringInterner? stringInterner = null)
        {
            if (token.Length == 0) return string.Empty;

            var tokenValue = new ReadOnlySpan<char>(_buffer, token.Index, token.Length);
            if (stringInterner != null)
            {
                return stringInterner.Intern(tokenValue);
            }
            else
            {
                return tokenValue.ToString();
            }
        }

        private Token CreateToken(TokenType type, int advanceLinePositionBy)
        {
            var linePosition = _linePosition;
            _linePosition += advanceLinePositionBy;
            return new Token(type, _lineNumber, linePosition);
        }

        private Token CreateToken(TokenType type, int advanceLineNumberBy, int advanceLinePositionBy)
        {
            var lineNumber = _lineNumber;
            _lineNumber += advanceLineNumberBy;

            var linePosition = _linePosition;
            _linePosition += advanceLinePositionBy;

            return new Token(type, lineNumber, linePosition);
        }

        private Token CreateToken(TokenType type, int advanceLineNumberBy, int advanceLinePositionBy, int index, int length)
        {
            var lineNumber = _lineNumber;
            _lineNumber += advanceLineNumberBy;

            var linePosition = _linePosition;
            _linePosition += advanceLinePositionBy;

            return new Token(type, lineNumber, linePosition, index, length);
        }

        private void EatWhitespace()
        {
            var lpos = _pos;
            var pos = _pos;
            while (pos < _epos)
            {
                var ch = _buffer[pos];
                if (ch == '\x20')
                {
                    pos++;
                    continue;
                }
                else if (ch == '\n')
                {
                    pos++;
                    lpos = pos;
                    _lineNumber++;
                    _linePosition = 1;
                    continue;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    pos++;
                    continue;
                }
                else
                {
                    break;
                }
            }
            _linePosition += pos - lpos;
            _pos = pos;
        }

        private int ReadBuffer(int index, int count)
        {
            return _reader.Read(_buffer, index, count);
        }

        private static bool IsTokenDelimiter(char ch)
        {
            return char.IsWhiteSpace(ch)
                || ch == '='
                || ch == '{'
                || ch == '}';
        }
    }
}
