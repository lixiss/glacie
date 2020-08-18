using Glacie.Data.Arz.FileFormat;
using IO = System.IO;

namespace Glacie.Data.Arz.Utilities
{
    // TODO: (High) (ArzVerifier) Move at higher level.
    // TODO: (Medium) (ArzVerifier) ArzVerifier should act as reader in various modes,
    // like just checksum or in pedantic mode.
    // It is basically just simple reader, but implemented separately.
    // For example, reading a strings should verify what their length is correct (fits in file)
    // and all characters in strings are ASCII chars.
    // TODO: (Low) (ArzVerifier) Need API which will provide diagnostic reports, over different levels.
    public sealed class ArzVerifier
    {
        public static void Verify(string path)
        {
            // TODO: use sequential reading
            using var stream = IO.File.Open(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);
            Verify(stream);
        }

        public static void Verify(IO.Stream stream)
        {
            ArzChecksum.Compute(stream, out var expected);

            var afStream = new ArzFileStream(stream);
            afStream.Seek(stream.Length - ArzFileFooter.Size);

            afStream.ReadFooter(out var actual);

            if (actual.Hash != expected.Hash)
            {
                throw ArzError.InvalidChecksum("Invalid checksum (Hash).");
            }

            if (actual.StringTableHash != expected.StringTableHash)
            {
                throw ArzError.InvalidChecksum("Invalid checksum (StringTableHash).");
            }

            if (actual.RecordDataHash != expected.RecordDataHash)
            {
                throw ArzError.InvalidChecksum("Invalid checksum (RecordDataHash).");
            }

            if (actual.RecordTableHash != expected.RecordTableHash)
            {
                throw ArzError.InvalidChecksum("Invalid checksum (RecordTableHash).");
            }
        }
    }
}
