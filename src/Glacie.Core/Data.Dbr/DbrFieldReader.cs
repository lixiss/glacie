using System;
using System.Collections.Generic;
using System.Globalization;

namespace Glacie.Data.Dbr
{
    /// <summary>
    /// Read DBR file in form of fields.
    /// Reader always return <c>"templateName"</c> field first, regardless to
    /// it's position.
    /// </summary>
    public sealed class DbrFieldReader : IDisposable
    {
        // TODO: support array or string as source of data
        private string _text;
        private int _pos;

        private bool _templateNameEmitted;
        private bool _eof;

        private TextSegment _fieldNameSegment;
        private List<TextSegment>? _valueSegments;
        private Queue<Field>? _queue;

        public DbrFieldReader(string text)
        {
            Check.Argument.NotNull(text, nameof(text));

            _text = text;
            _templateNameEmitted = false;
        }

        public void Dispose()
        {
            _text = null!;
        }

        public bool Read()
        {
            if (_templateNameEmitted)
            {
                if (_queue != null && _queue.Count > 0)
                {
                    return DequeueState();
                }

                var result = TryParseNextField();
                _eof = !result;
                return result;
            }
            else
            {
                while (TryParseNextField())
                {
                    if (NameEqualsTo("templateName"))
                    {
                        _templateNameEmitted = true;
                        return true;
                    }
                    else
                    {
                        EnqueueState();
                    }
                }

                return DequeueState();
            }
        }

        private void EnqueueState()
        {
            Field field;

            Check.That(_valueSegments != null);
            if (_valueSegments.Count == 1)
            {
                field = new Field(_fieldNameSegment, _valueSegments[0], null);
            }
            else
            {
                field = new Field(_fieldNameSegment, default, _valueSegments);
                _valueSegments = null;
            }

            if (_queue == null) _queue = new Queue<Field>();
            _queue.Enqueue(field);
        }

        private bool DequeueState()
        {
            if (_queue == null || _queue.Count == 0) return false;

            var field = _queue.Dequeue();

            _fieldNameSegment = field.NameSegment;
            if (field.ValueSegments != null)
            {
                _valueSegments = field.ValueSegments;
            }
            else
            {
                if (_valueSegments != null)
                {
                    _valueSegments.Clear();
                    _valueSegments.Add(field.ValueSegment);
                }
                else
                {
                    _valueSegments = new List<TextSegment>();
                    _valueSegments.Add(field.ValueSegment);
                }
            }

            return true;
        }

        private void ClearState()
        {
            _fieldNameSegment = default;
            if (_valueSegments == null)
            {
                _valueSegments = new List<TextSegment>();
            }
            else
            {
                _valueSegments.Clear();
            }
        }

        private bool TryParseNextField()
        {
            var text = _text;
            var epos = text.Length;
            var pos = _pos;

            ClearState();

            // Field Name
            // Eat any leading spaces
            while (pos < epos && char.IsWhiteSpace(text[pos])) pos++;
            if (pos >= epos)
            {
                // _eof = true;
                return false;
            }

            var fieldNameStartPos = pos;
            int fieldNameEndPos;
            while (true)
            {
                while (pos < epos && IsFieldNameChar(text[pos])) pos++;
                fieldNameEndPos = pos;

                while (pos < epos && char.IsWhiteSpace(text[pos])) pos++;
                if (pos >= epos)
                {
                    throw new DbrParseErrorException("Syntax error. Unexpected end. State 1.");
                }
                if (IsFieldNameDelimiter(text[pos]))
                {
                    pos++;
                    break;
                }
            }
            if (fieldNameEndPos == fieldNameStartPos) throw new DbrParseErrorException("Syntax error. Empty field name.");
            SetFieldName(fieldNameStartPos, fieldNameEndPos);

            DebugCheck.That(pos < epos);

            // Values
            while (true)
            {
                // Eat any leading spaces
                while (pos < epos && char.IsWhiteSpace(text[pos])) pos++;

                var valueStartPos = pos;
                int valueEndPos;
                while (true)
                {
                    while (pos < epos && IsValueChar(text[pos])) pos++;
                    valueEndPos = pos;

                    while (pos < epos && char.IsWhiteSpace(text[pos])) pos++;
                    if (pos >= epos)
                    {
                        throw new DbrParseErrorException("Syntax error. Unexpected end. State 2.");
                    }
                    if (IsValueDelimiter(text[pos]))
                    {
                        break;
                    }
                }
                AddValue(valueStartPos, valueEndPos);

                if (text[pos] == ',')
                {
                    pos++;
                    break;
                }
                else if (text[pos] == ';')
                {
                    // next value
                    pos++;
                    continue;
                }
                else throw new DbrParseErrorException("Syntax error. Unexpected character. State 3.");
            }

            _pos = pos;

            DebugCheck.That(_fieldNameSegment.Count > 0);
            DebugCheck.That(_valueSegments != null && _valueSegments.Count > 0);
            return true;
        }

        public string Name
        {
            get
            {
                if (_eof) throw Error.InvalidOperation("End of stream.");
                return _text.Substring(_fieldNameSegment.Index, _fieldNameSegment.Count);
            }
        }

        public int ValueCount => _valueSegments != null ? _valueSegments.Count : 0;

        public bool NameEqualsTo(string value)
        {
            if (value == null) return false;

            var valueLength = value.Length;
            if (valueLength == _fieldNameSegment.Count)
            {
                if (valueLength == 0) return true;
                var nameSpan = GetTextSpan(_fieldNameSegment);
                ReadOnlySpan<char> valueSpan = value;
                return nameSpan.CompareTo(valueSpan, StringComparison.Ordinal) == 0;
            }
            return false;
        }

        private ReadOnlySpan<char> GetTextSpan(in TextSegment segment)
        {
            ReadOnlySpan<char> span = _text;
            return span.Slice(segment.Index, segment.Count);
        }

        // TODO: get values over string table, to avoid not necessary instancing string objects
        public string GetStringValue(int index)
        {
            if (_eof) throw Error.InvalidOperation("End of stream.");

            var valueSpan = GetTextSpan(_valueSegments[index]);
            return new string(valueSpan);
        }

        public bool GetBooleanValue(int index)
        {
            if (_eof) throw Error.InvalidOperation("End of stream.");

            var valueSpan = GetTextSpan(_valueSegments[index]);
            if (valueSpan.Length == 1)
            {
                var ch = valueSpan[0];
                if (ch == '0') return false;
                else if (ch == '1') return true;
                else throw Error.InvalidOperation("Can't parse boolean value.");
            }

            // TODO: relaxed booleans
            // TODO: compare with false and true
            if (valueSpan.Equals("false".AsSpan(), StringComparison.Ordinal))
            {
                return false;
            }
            else if (valueSpan.Equals("true".AsSpan(), StringComparison.Ordinal))
            {
                return true;
            }

            throw Error.InvalidOperation("Can't parse boolean value.");
        }

        public int GetInt32Value(int index)
        {
            if (_eof) throw Error.InvalidOperation("End of stream.");

            var valueSpan = GetTextSpan(_valueSegments[index]);

            var numberStyle = NumberStyles.AllowLeadingSign;
            if (int.TryParse(valueSpan, numberStyle, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            else
            {
                throw Error.InvalidOperation("Can't parse int32 value.");
            }
        }

        public float GetFloat32Value(int index)
        {
            if (_eof) throw Error.InvalidOperation("End of stream.");

            var valueSpan = GetTextSpan(_valueSegments[index]);

            var numberStyle = NumberStyles.AllowLeadingSign
                | NumberStyles.AllowDecimalPoint
                | NumberStyles.AllowExponent;
            if (float.TryParse(valueSpan, numberStyle, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            else
            {
                throw Error.InvalidOperation("Can't parse int32 value.");
            }
        }

        private void SetFieldName(int start, int end)
        {
            _fieldNameSegment = new TextSegment(start, end - start);

            DebugCheck.That(_fieldNameSegment.Count > 0);
        }

        private void AddValue(int start, int end)
        {
            _valueSegments.Add(new TextSegment(start, end - start));
        }

        private static bool IsFieldNameDelimiter(char ch)
        {
            return ch == ',';
        }

        private static bool IsFieldNameChar(char ch)
        {
            return !(IsFieldNameDelimiter(ch) || char.IsWhiteSpace(ch));
        }

        private static bool IsValueDelimiter(char ch)
        {
            return ch == ';' || ch == ',';
        }

        private static bool IsValueChar(char ch)
        {
            return !(IsValueDelimiter(ch) || char.IsWhiteSpace(ch));
        }

        private readonly struct TextSegment
        {
            public readonly int Index;
            public readonly int Count;

            public TextSegment(int index, int count)
            {
                DebugCheck.That(index >= 0);
                DebugCheck.That(count >= 0);

                Index = index;
                Count = count;
            }
        }

        private readonly struct Field
        {
            public readonly TextSegment NameSegment;
            public readonly TextSegment ValueSegment;
            public readonly List<TextSegment>? ValueSegments;

            public Field(TextSegment name, TextSegment value, List<TextSegment>? values)
            {
                NameSegment = name;
                ValueSegment = value;
                ValueSegments = values;
            }
        }
    }
}
