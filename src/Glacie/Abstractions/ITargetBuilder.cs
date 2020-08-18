using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    public interface ITargetBuilder
    {
        void Path(string path);
        void Database(ArzDatabase database);
    }
}
