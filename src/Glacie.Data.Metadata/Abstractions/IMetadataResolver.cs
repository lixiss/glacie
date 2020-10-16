using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

namespace Glacie.Metadata
{
    public interface IMetadataResolver
    {
        DatabaseType GetDatabaseType();

        Resolution<RecordType> ResolveRecordTypeByName(string name);
        Resolution<RecordType> ResolveRecordTypeByPath(Path path);
        Resolution<RecordType> ResolveRecordTypeByTemplateName(Path templateName);

        bool TryResolveRecordTypeByName(string name, [NotNullWhen(returnValue: true)] out RecordType? result);
        bool TryResolveRecordTypeByPath(Path path, [NotNullWhen(returnValue: true)] out RecordType? result);
        bool TryResolveRecordTypeByTemplateName(Path templateName, [NotNullWhen(returnValue: true)] out RecordType? result);
    }
}
