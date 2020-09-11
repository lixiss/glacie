using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    public interface IArzContext
    {
        string? Path { get; }

        ArzDatabase Database { get; }

        ArzStringTable StringTable { get; }

        ArzRecordClassTable RecordClassTable { get; }

        ArzFileFormat Format { get; }

        bool CanInferFormat { get; }

        bool TryInferFormat(out ArzFileFormat result);
    }
}
