namespace Glacie.Data.Arz.Infrastructure
{
    // TODO: (VeryLow) (IArzRecordMetrics) Where to place? It currently used only for tests. Make it internal?

    /// <summary>
    /// Interface for getting access for some internal record's data, for tests.
    /// </summary>
    public interface IArzRecordMetrics
    {
        public int Version { get; }

        public int NumberOfRemovedFields { get; }
    }
}
