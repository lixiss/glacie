using Glacie.Data.Arz;

namespace Glacie.Infrastructure
{
    internal interface IDatabaseProvider
    {
        bool CanProvideDatabase { get; }

        ArzDatabase GetDatabase();
    }
}
