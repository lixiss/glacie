using System;

using Glacie.Data.Compression;

namespace Glacie.Data.Arz
{
    // TODO: aggressive inlining

    public readonly struct ArzFileFormat : IEquatable<ArzFileFormat>
    {
        private const string STRING_TQ = "TQ";
        private const string STRING_TQIT = "TQIT";
        private const string STRING_TQAE = "TQAE";
        private const string STRING_GD = "GD";
        private const string STRING_AUTO = "Automatic";
        private const string STRING_UNKNOWN = "Unknown";

        private const int FLAG_RECORD_HAS_DECOMPRESSED_LENGTH = 1 << 0;
        private const int FLAG_COMPRESSION_ZLIB = 1 << 1;
        private const int FLAG_COMPRESSION_LZ4 = 1 << 2;

        private const int MASK_COMPRESSION_ALGORITHM = FLAG_COMPRESSION_ZLIB | FLAG_COMPRESSION_LZ4;

        private readonly static ArzFileFormat FORMAT_AUTO = new ArzFileFormat(0);
        private readonly static ArzFileFormat FORMAT_TQ = new ArzFileFormat(FLAG_RECORD_HAS_DECOMPRESSED_LENGTH | FLAG_COMPRESSION_ZLIB);
        private readonly static ArzFileFormat FORMAT_TQAE = new ArzFileFormat(FLAG_COMPRESSION_ZLIB);
        private readonly static ArzFileFormat FORMAT_GD = new ArzFileFormat(FLAG_RECORD_HAS_DECOMPRESSED_LENGTH | FLAG_COMPRESSION_LZ4);

        private readonly static ArzFileFormat FORMAT_TQ_OR_GD = new ArzFileFormat(FLAG_RECORD_HAS_DECOMPRESSED_LENGTH);

        #region Static

        public static ArzFileFormat Automatic => FORMAT_AUTO;
        public static ArzFileFormat TitanQuest => FORMAT_TQ;
        public static ArzFileFormat TitanQuestAnniversaryEdition => FORMAT_TQAE;
        public static ArzFileFormat GrimDawn => FORMAT_GD;

        public static bool TryParse(string? value, out ArzFileFormat result)
        {
            if (value == null)
            {
                result = default;
                return false;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            if (comparer.Equals(value, STRING_AUTO))
            {
                result = FORMAT_TQ;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQ) || comparer.Equals(value, STRING_TQIT))
            {
                result = FORMAT_TQ;
                return true;
            }
            else if (comparer.Equals(value, STRING_TQAE))
            {
                result = FORMAT_TQAE;
                return true;
            }
            else if (comparer.Equals(value, STRING_GD))
            {
                result = FORMAT_GD;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static ArzFileFormat Parse(string value)
        {
            if (TryParse(value, out var result)) return result;
            throw Error.Argument(nameof(value));
        }

        public static bool TryGetFromHeader(int magic, int version, out ArzFileFormat result)
        {
            if (magic == 2 && version == 3)
            {
                // Titan Quest, Titan Quest: Immortal Throne
                // Grim Dawn 1.1.7.1
                result = FORMAT_TQ_OR_GD;
                return true;
            }
            else if (magic == 4 && version == 3)
            {
                // Titan Quest Anniversary Edition (2.9)
                result = FORMAT_TQAE;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        #endregion

        private readonly int _value;

        private ArzFileFormat(int value)
        {
            _value = value;
        }

        public bool RecordHasDecompressedLength => (_value & FLAG_RECORD_HAS_DECOMPRESSED_LENGTH) != 0;

        public bool UseZlibCompression => (_value & FLAG_COMPRESSION_ZLIB) != 0;

        public bool UseLz4Compression => (_value & FLAG_COMPRESSION_LZ4) != 0;

        public bool HasCompressionAlgorithm => (_value & MASK_COMPRESSION_ALGORITHM) != 0;

        public bool CompressionAlgorithmCompatibleWith(ArzFileFormat other)
        {
            var thisAlg = _value & MASK_COMPRESSION_ALGORITHM;
            var otherAlg = other._value & MASK_COMPRESSION_ALGORITHM;
            return thisAlg == otherAlg;
        }

        public bool MustSetDecompressedLength => (_value & FLAG_COMPRESSION_LZ4) != 0;

        /// <summary>
        /// When true, record and fields should use forward slash as path separator.
        /// </summary>
        public bool StandardPathSeparator => UseLz4Compression;

        public bool BackslashPathSeparator => !StandardPathSeparator;

        public bool Complete => HasCompressionAlgorithm;

        public bool Valid
        {
            get
            {
                var compressionAlgorithm = _value & MASK_COMPRESSION_ALGORITHM;

                if (compressionAlgorithm == 0)
                {
                    return true;
                }
                else if (compressionAlgorithm == FLAG_COMPRESSION_ZLIB)
                {
                    return true;
                }
                else if (compressionAlgorithm == FLAG_COMPRESSION_LZ4)
                {
                    return RecordHasDecompressedLength;
                }

                return false;
            }
        }

        public ArzFileFormat WithCompressionAlgorithm(CompressionAlgorithm value)
        {
            var algorithm = _value & MASK_COMPRESSION_ALGORITHM;
            switch (value)
            {
                case CompressionAlgorithm.Zlib:
                    Check.That(algorithm == 0 || algorithm == FLAG_COMPRESSION_ZLIB);
                    return new ArzFileFormat(_value | FLAG_COMPRESSION_ZLIB);

                case CompressionAlgorithm.Lz4:
                    Check.That(algorithm == 0 || algorithm == FLAG_COMPRESSION_LZ4);
                    return new ArzFileFormat(_value | FLAG_COMPRESSION_LZ4);

                default: throw Error.Argument(nameof(value));
            }
        }

        #region Equatable

        public override string ToString()
        {
            if (this == FORMAT_AUTO) return STRING_AUTO;
            else if (this == FORMAT_TQ) return STRING_TQ;
            else if (this == FORMAT_TQAE) return STRING_TQAE;
            else if (this == FORMAT_GD) return STRING_GD;
            else return STRING_UNKNOWN;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override bool Equals(object obj)
        {
            if (obj is ArzFileFormat other)
            {
                return _value == other._value;
            }
            return false;
        }

        public bool Equals(ArzFileFormat other)
        {
            return _value == other._value;
        }

        public static bool operator ==(ArzFileFormat a, ArzFileFormat b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(ArzFileFormat a, ArzFileFormat b)
        {
            return a._value != b._value;
        }

        #endregion
    }
}
