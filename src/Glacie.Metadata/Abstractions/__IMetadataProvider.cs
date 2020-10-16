using System.Diagnostics.CodeAnalysis;

using Glacie.Metadata.Builder;

namespace Glacie.Metadata
{
    // TODO: not providers => they should be MetadataBuilder,
    // Providers instead should only provide final metadata types.


    // 1. All providers should provide metadata builders.
    // 2. Support for request single RecordTypeBuilder
    // 3. Support for request complete DatabaseTypeBuilder

    // Should provide next functionality:
    // 1. Query for record type (caching not needed?), but path mapping might be needed.
    // 2. Query for database type

    internal interface __IMetadataProvider
    {
        MetadataBuilder GetDatabaseTypeBuilder();

        bool TryGetRecordTypeBuilder(in Path1 path,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result);

        RecordTypeBuilder GetRecordTypeBuilder(in Path1 path);
    }
}
