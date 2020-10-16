using System;
using System.Text;

namespace Glacie.Analysis.Binary
{
    public readonly ref struct StringToken
    {
        private readonly static Encoding s_latin1Encoding = Encoding.GetEncoding("iso-8859-1");

        private readonly StringTokenType _type;
        private readonly long _position;
        private readonly int _length;
        private readonly ReadOnlySpan<byte> _value;

        public StringToken(StringTokenType type, long position, int length, ReadOnlySpan<byte> value)
        {
            _type = type;
            _position = position;
            _length = length;
            _value = value;
        }

        public StringTokenType Type => _type;

        public long Position => _position;

        public int Length => _length;

        public string GetValue()
        {
            return s_latin1Encoding.GetString(_value);
        }
    }
}
