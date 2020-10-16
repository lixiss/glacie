using System;

using IO = System.IO;

namespace Glacie.Metadata.Serialization
{
    public sealed class MetadataReaderOptions
    {
        // TODO: better to have some interface instead delegate, it might be
        // more explicit and more understandable. Also consider to get other
        // name. Source/File/etc.
        public Func<string, IO.Stream?>? ResourceResolver { get; set; }
    }
}
