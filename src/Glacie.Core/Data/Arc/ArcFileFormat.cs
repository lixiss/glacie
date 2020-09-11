using System;
using System.Runtime.CompilerServices;

namespace Glacie.Data.Arc
{
    public readonly struct ArcFileFormat : IEquatable<ArcFileFormat>
    {
        // TODO: complete implementation similar to ArzFileFormat

        public static bool TryParse(string value, out ArcFileFormat result)
        {
            // TODO: complete implementation

            result = default;
            return false;
        }

        public static ArcFileFormat FromVersion(int version)
        {
            if (version == 1)
            {
                return new ArcFileFormat(ZlibCompressionFlag);
            }
            else if (version == 3)
            {
                return new ArcFileFormat(Lz4CompressionFlag);
            }
            else throw Error.Argument(nameof(version));
        }

        private const int ZlibCompressionFlag = 1 << 0;
        private const int Lz4CompressionFlag = 1 << 1;

        private readonly int _value;

        private ArcFileFormat(int value)
        {
            _value = value;
        }

        public int Version
        {
            get
            {
                if (ZlibCompression) return 1;
                else if (Lz4Compression) return 3;
                else throw Error.InvalidOperation($"Unknown or invalid {nameof(ArcFileFormat)} value.");
            }
        }

        public bool ZlibCompression
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value & ZlibCompressionFlag) != 0;
        }

        public bool Lz4Compression
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value & Lz4CompressionFlag) != 0;
        }

        public bool SupportStoreChunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Lz4Compression;
        }

        public bool Complete
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ZlibCompression || Lz4Compression;
        }

        #region IEquatable

        public bool Equals(ArcFileFormat other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ArcFileFormat other) return _value == other._value;
            return false;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(ArcFileFormat x, ArcFileFormat y)
        {
            return x._value == y._value;
        }

        public static bool operator !=(ArcFileFormat x, ArcFileFormat y)
        {
            return x._value != y._value;
        }

        public override string ToString()
        {
            if (!Complete) return "auto";

            var version = Version;
            if (version == 1) return "tq";
            else if (version == 3) return "gd";
            else throw Error.Unreachable();
        }

        #endregion
    }
}
