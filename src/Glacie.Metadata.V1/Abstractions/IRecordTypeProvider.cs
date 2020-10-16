using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Metadata.V1;

namespace Glacie.Metadata.V1
{
    public interface IRecordTypeProvider
    {
        [Obsolete("Not implemented. Not sure what this API call needed.", true)]
        RecordType GetByName(string name);

        bool TryGetByTemplateName(in Path1 templateName,
            [NotNullWhen(returnValue: true)] out RecordType? result);

        RecordType? GetByTemplateNameOrDefault(in Path1 templateName);

        RecordType GetByTemplateName(in Path1 templateName);
    }
}
