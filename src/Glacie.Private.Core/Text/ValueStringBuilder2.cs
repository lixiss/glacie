using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Glacie.Text
{
    public ref partial struct ValueStringBuilder2 // : IDisposable
    {
        private Span<char> _chars;
        private int _pos;
        private char[]? _arrayToReturnToPool;

        public ValueStringBuilder2(Span<char> buffer)
        {
            _chars = buffer;
            _pos = 0;
            _arrayToReturnToPool = null;
        }

        public ValueStringBuilder2(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            _pos = 0;
        }

        public int Length
        {
            get => _pos;
            set
            {
                DebugCheck.That(value >= 0);
                DebugCheck.That(value <= _chars.Length);
                _pos = value;
            }
        }

        public int Capacity => _chars.Length;
    }
}
