using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    public interface IArzRecordProvider
    {
        bool TryGet(string name, [NotNullWhen(returnValue: true)] out ArzRecord? result);

        ArzRecord? GetOrDefault(string name);

        ArzRecord Get(string name);
    }
}
