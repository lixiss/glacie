using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    // TODO: (Gx) Source might be a set of DBR files, this is valid case.
    public interface ISourceBuilder
    {
        void Path(string path);
        void Database(ArzDatabase database);
    }
}
