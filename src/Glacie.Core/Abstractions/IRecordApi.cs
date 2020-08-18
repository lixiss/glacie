namespace Glacie.Abstractions
{
    // TODO: (Low) Rename to IRecordApiContract.
    // TODO: (Low) Include this interface only as conditional compile-time feature.

    internal interface IRecordApi
    {
        string Name { get; }
        string Class { get; set; }

        int Count { get; }

        Variant this[string name] { get; set; }

        void Set(string name, int value);
        void Set(string name, float value);
        void Set(string name, double value);
        void Set(string name, bool value);
        void Set(string name, string value);
        void Set(string name, int[] values);
        void Set(string name, float[] values);
        void Set(string name, double[] values);
        void Set(string name, bool[] values);
        void Set(string name, string[] values);
        void Set(string name, Variant variant);

        void Add(string name, int value);
        void Add(string name, float value);
        void Add(string name, double value);
        void Add(string name, bool value);
        void Add(string name, string value);
        void Add(string name, int[] value);
        void Add(string name, float[] value);
        void Add(string name, double[] value);
        void Add(string name, bool[] value);
        void Add(string name, string[] value);
        void Add(string name, Variant value);

        bool Remove(string name);
    }
}
