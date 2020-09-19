using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Metadata;

namespace Glacie.Metadata
{
    public abstract class MetadataProvider : IMetadataProvider, IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract RecordType GetByName(string name);

        public abstract DatabaseType GetDatabaseType();

        public abstract bool TryGetByTemplateName(in VirtualPath templateName,
            [NotNullWhen(returnValue: true)] out RecordType? result);

        public virtual RecordType? GetByTemplateNameOrDefault(in VirtualPath templateName)
        {
            if (TryGetByTemplateName(in templateName, out var result)) return result;
            else return null;
        }

        public virtual RecordType GetByTemplateName(in VirtualPath templateName)
        {
            if (TryGetByTemplateName(in templateName, out var result)) return result;
            else
            {
                throw Error.InvalidOperation("Unable to get record type: \"{0}\".", templateName.ToString());
            }
        }
    }
}
