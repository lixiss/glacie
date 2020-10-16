using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata
{
    public interface IMetadataProvider
    {
        DatabaseType GetDatabaseType();

        bool TryGetRecordTypeByName(string name, [NotNullWhen(returnValue: true)] out RecordType? result);
        RecordType GetRecordTypeByName(string name);

        bool TryGetRecordTypeByPath(Path path, [NotNullWhen(returnValue: true)] out RecordType? result);
        RecordType RecordTypeByPath(Path path);

        bool TryGetRecordTypeByTemplateName(Path templateName, [NotNullWhen(returnValue: true)] out RecordType? result);
        RecordType GetRecordTypeByTemplateName(Path templateName);
    }
}
