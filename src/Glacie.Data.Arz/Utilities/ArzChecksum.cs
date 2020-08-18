using System;
using Glacie.ChecksumAlgorithms;
using Glacie.Data.Arz.FileFormat;
using IO = System.IO;

namespace Glacie.Data.Arz.Utilities
{
    // TODO: (Low) (Decision) Move it to ArzVerifier? This used by verifier and by writer.
    public static class ArzChecksum
    {
        public static void Compute(IO.Stream stream, out ArzFileFooter result)
        {
            Compute(stream, stream.Length, out result);
        }

        public static void Compute(IO.Stream stream, long length, out ArzFileFooter result)
        {
            Check.True(length >= 0);

            var afStream = new ArzFileStream(stream);

            Span<byte> headerBytes = stackalloc byte[ArzFileHeader.Size];
            var bytesRead = afStream.Read(headerBytes);
            Check.True(bytesRead == headerBytes.Length);

            ArzFileStream.ReadHeader(headerBytes, out var afHeader);

            var allDataHash = new Adler32();
            var stringTableHash = new Adler32();
            var recordDataHash = new Adler32();
            var recordTableHash = new Adler32();

            allDataHash.ComputeHash(headerBytes);

            // process field data
            {
                var areaLength = Math.Min(afHeader.RecordTableOffset, afHeader.StringTableOffset)
                    - ArzFileHeader.Size;
                ComputeHash(stream, areaLength, allDataHash, recordDataHash);
            }

            // process record table
            {
                afStream.Seek(afHeader.RecordTableOffset);
                ComputeHash(stream, afHeader.RecordTableSize, allDataHash, recordTableHash);
            }

            // process string table
            {
                afStream.Seek(afHeader.StringTableOffset);
                ComputeHash(stream, afHeader.StringTableSize, allDataHash, stringTableHash);
            }

            result = new ArzFileFooter
            {
                Hash = allDataHash.Hash,
                StringTableHash = stringTableHash.Hash,
                RecordDataHash = recordDataHash.Hash,
                RecordTableHash = recordTableHash.Hash,
            };

            static void ComputeHash(IO.Stream stream, long length, Adler32 hash1, Adler32 hash2)
            {
                Span<byte> buffer = stackalloc byte[4 * 1024];

                while (length > 0)
                {
                    var blockLength = checked((int)Math.Min(buffer.Length, length));
                    var blockSpan = buffer.Slice(0, blockLength);

                    var bytesRead = stream.Read(blockSpan);
                    Check.True(bytesRead > 0);

                    var actualBlockSpan = blockSpan.Slice(0, bytesRead);
                    hash1.ComputeHash(actualBlockSpan);
                    hash2.ComputeHash(actualBlockSpan);

                    length -= bytesRead;
                }
            }
        }
    }
}
