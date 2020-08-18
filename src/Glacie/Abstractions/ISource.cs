using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    public interface ISource
    {
        string? Path { get; }

        ArzDatabase? Database { get; }
    }
}
