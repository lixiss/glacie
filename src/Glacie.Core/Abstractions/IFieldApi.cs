using Glacie.Data.Arz;

namespace Glacie.Abstractions
{
    // TODO: (Low) Rename to IFieldApiContract.
    // TODO: (Low) Include this interface only as conditional compile-time feature.

    internal interface IFieldApi<TRecordType>
    {
        TRecordType Record { get; }

        string Name { get; }

        ArzValueType ValueType { get; }

        int Count { get; }

        T Get<T>();
        T Get<T>(int index);

        // bool Is<T>();
        // T To<T>();

        void Set(int value);
        void Set(float value);
        void Set(double value);
        void Set(bool value);
        void Set(string value);
        void Set(int[] value);
        void Set(float[] value);
        void Set(double[] value);
        void Set(bool[] value);
        void Set(string[] value);
        void Set(Variant value);
    }
}
