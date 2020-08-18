using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    // TODO: (Gx) In source or target we need BaseDir / BasePath / DatabasePath / etc.

    public interface ITarget
    {
        string? Path { get; }

        ArzDatabase? Database { get; }
    }
}
