using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata.Builder
{
    /// <inheritdoc cref="__IMetadataBuilder"/>
    public abstract class __MetadataBuilder
        : __IMetadataBuilder
        , IDisposable
    {
        protected __MetadataBuilder() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract MetadataBuilder GetDatabaseType();

        public abstract bool TryGetRecordTypeByName(string name, [NotNullWhen(true)] out RecordTypeBuilder? result);

        public abstract bool TryGetRecordTypeByPath(in Path1 path, [NotNullWhen(true)] out RecordTypeBuilder? result);

        public RecordTypeBuilder GetRecordTypeByName(string name)
        {
            if (TryGetRecordTypeByName(name, out var recordType)) { return recordType; }
            else throw Error.InvalidOperation("Can't get record type with name: \"{0}\".", name);
        }

        public RecordTypeBuilder GetRecordTypeByPath(in Path1 path)
        {
            if (TryGetRecordTypeByPath(path, out var recordType)) { return recordType; }
            else throw Error.InvalidOperation("Can't get record type with name: \"{0}\".", path.ToString());
        }
    }
}
