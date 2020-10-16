using Glacie.Data.Arz;

namespace Glacie.Modules
{
    public interface IDatabaseInfo
    {
        string? PhysicalPath { get; }

        ArzDatabase? Database { get; }
    }
}
