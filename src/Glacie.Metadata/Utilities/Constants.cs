namespace Glacie.Metadata
{
    internal static class Constants
    {
        public const Path1Form InternalTemplatePathForm
            = Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator
            | Path1Form.LowerInvariant;
    }
}
