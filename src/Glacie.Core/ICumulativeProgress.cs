namespace Glacie
{
    public interface ICumulativeProgress<T>
    {
        void SetValue(T value);

        void SetMaximumValue(T value);
    }
}
