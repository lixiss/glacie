namespace Glacie.Cli.Arz.Templates
{
    // TODO: template name is case-sensitive, also do path normalization...
    public interface IRecordDefinitionProvider
    {
        RecordDefinition GetRecordDefinition(string name);
    }
}
