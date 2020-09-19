using Glacie.Data.Metadata;

namespace Glacie.Metadata
{
    public interface IDatabaseTypeProvider
    {
        DatabaseType GetDatabaseType();
    }
}
