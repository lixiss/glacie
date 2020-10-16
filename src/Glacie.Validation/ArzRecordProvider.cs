using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    public abstract class ArzRecordProvider : IArzRecordProvider
    {
        protected ArzRecordProvider() { }

        public abstract bool TryGet(string name, [NotNullWhen(returnValue: true)] out ArzRecord? result);

        public ArzRecord? GetOrDefault(string name)
        {
            if (TryGet(name, out var result)) return result;
            else return null;
        }

        public ArzRecord Get(string name)
        {
            if (TryGet(name, out var result)) return result;
            throw Error.InvalidOperation("Record not found: \"{0}\".", name);
        }
    }
}
