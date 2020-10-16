using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Abstractions;

namespace Glacie.Metadata
{
    public abstract class MetadataResolver
        : IMetadataResolver
        , IDisposable
    {
        private bool _isDisposed;

        protected MetadataResolver() { }

        public void Dispose() { }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                _isDisposed = true;
            }
        }

        protected bool IsDisposed => _isDisposed;

        public abstract DatabaseType GetDatabaseType();

        public abstract Resolution<RecordType> ResolveRecordTypeByName(string name);

        public abstract Resolution<RecordType> ResolveRecordTypeByPath(Path path);

        public abstract Resolution<RecordType> ResolveRecordTypeByTemplateName(Path templateName);

        public bool TryResolveRecordTypeByName(string name, [NotNullWhen(returnValue: true)] out RecordType? result)
        {
            var resolution = ResolveRecordTypeByName(name);
            if (resolution.HasValue)
            {
                result = resolution.Value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryResolveRecordTypeByPath(Path path, [NotNullWhen(returnValue: true)] out RecordType? result)
        {
            var resolution = ResolveRecordTypeByPath(path);
            if (resolution.HasValue)
            {
                result = resolution.Value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryResolveRecordTypeByTemplateName(Path templateName, [NotNullWhen(returnValue: true)] out RecordType? result)
        {
            var resolution = ResolveRecordTypeByTemplateName(templateName);
            if (resolution.HasValue)
            {
                result = resolution.Value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
