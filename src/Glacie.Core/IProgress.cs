namespace Glacie
{
    public interface IProgress
    {
        /// <summary>
        /// Progress in indeterminate state when <see cref="MaxValue"/> is <c>&lt;=</c> 0.
        /// </summary>
        bool Indeterminate { get; }

        double Percentage { get; }

        long Value { get; set; }

        long MaxValue { get; set; }

        string? Title { get; set; }

        string? Message { get; set; }

        void AddValue(long value);

        void AddMaxValue(long value);

        void Report(long value, string message);
    }
}
