using Glacie.Data.Metadata.V1;

namespace Glacie.Metadata.V1
{
    public interface IDatabaseTypeProvider
    {
        DatabaseType GetDatabaseType();
    }
}
