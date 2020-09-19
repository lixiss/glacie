using System;

namespace Glacie.Cli.Arz.Templates
{
    [Obsolete("Remove me.", true)]
    public interface IRecordDefinitionProvider
    {
        RecordDefinition GetRecordDefinition(string name);
    }
}
