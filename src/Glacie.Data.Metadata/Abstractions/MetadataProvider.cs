using System;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata
{
    public abstract class MetadataProvider
        : IMetadataProvider
        , IDisposable
    {
        private bool _isDisposed;

        protected MetadataProvider() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed => _isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        public abstract DatabaseType GetDatabaseType();

        public abstract bool TryGetRecordTypeByName(string name, [NotNullWhen(true)] out RecordType? result);

        public abstract bool TryGetRecordTypeByPath(Path path, [NotNullWhen(true)] out RecordType? result);

        public abstract bool TryGetRecordTypeByTemplateName(Path templateName, [NotNullWhen(true)] out RecordType? result);

        public RecordType GetRecordTypeByName(string name)
        {
            if (TryGetRecordTypeByName(name, out var recordType)) { return recordType; }
            else
            {
                throw Error.InvalidOperation("Record type for name \"{0}\" not found.", name);
            }
        }

        public RecordType RecordTypeByPath(Path path)
        {
            if (TryGetRecordTypeByPath(path, out var recordType)) { return recordType; }
            else
            {
                throw Error.InvalidOperation("Record type for path \"{0}\" not found.", path.ToString());
            }
        }

        public RecordType GetRecordTypeByTemplateName(Path templateName)
        {
            if (TryGetRecordTypeByTemplateName(templateName, out var recordType)) { return recordType; }
            else
            {
                throw Error.InvalidOperation("Record type for template name \"{0}\" not found.", templateName.ToString());
            }
        }
    }
}
