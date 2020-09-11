namespace Glacie
{
    /// <remarks>
    /// Methods of this class should be thread-safe. It is intended to call this
    /// from multiple threads, so underlying implementation most likely may end
    /// with <see cref="System.Threading.Interlocked.Add(ref long, long)" />.
    /// </remarks>
    public interface IIncrementalProgress<T>
    {
        void AddValue(T value);

        void AddMaximumValue(T value);
    }
}
