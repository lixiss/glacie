using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Metadata;

namespace Glacie.Metadata
{
    public interface IRecordTypeProvider
    {
        [Obsolete("Not implemented. Not sure what this API call needed.", true)]
        RecordType GetByName(string name);

        bool TryGetByTemplateName(in VirtualPath templateName,
            [NotNullWhen(returnValue: true)] out RecordType? result);

        RecordType? GetByTemplateNameOrDefault(in VirtualPath templateName);

        RecordType GetByTemplateName(in VirtualPath templateName);
    }
}
