using IO = System.IO;

namespace Glacie.Data.Arz
{
    public static class ArzReader
    {
        public static ArzDatabase Read(string path, ArzReaderOptions? options = null)
            => ArzFileContext.Open(path, options);

        public static ArzDatabase Read(IO.Stream stream, ArzReaderOptions? options = null)
            => ArzFileContext.Open(stream, options);
    }
}
