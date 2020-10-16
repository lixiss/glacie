using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Metadata.Builder;

namespace Glacie.Metadata
{
    // Requirements: Should provide diagnostics, but still might throw exceptions.

    [Obsolete("See correct interface in Glacie.Data.Metadata.Abstractions.IMetadataProvider and IMetadataResolver.")]
    public abstract class __MetadataProvider
        : __IMetadataProvider
        , IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract MetadataBuilder GetDatabaseTypeBuilder();

        public abstract bool TryGetRecordTypeBuilder(in Path1 path,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result);

        public RecordTypeBuilder GetRecordTypeBuilder(in Path1 path)
        {
            if (TryGetRecordTypeBuilder(in path, out var result)) return result;
            else throw Error.InvalidOperation("Unable to get record type: \"{0}\".", path);
        }

        //public DatabaseType GetDatabaseType() => GetDatabaseTypeBuilder().Build();
    }
}
