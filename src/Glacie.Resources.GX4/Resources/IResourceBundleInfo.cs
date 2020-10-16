using Glacie.Localization;

namespace Glacie.Resources
{
    public interface IResourceBundleInfo
    {
        public ResourceBundleKind Kind { get; }
        public string? PhysicalPath { get; }
        public short Priority { get; }

        // TODO: Rename prefix into something more meaningful (e.g. virtual root path),
        // or mount path (also known as mountpoint)
        public string Prefix { get; }

        public LanguageSymbol Language { get; }
    }
}
