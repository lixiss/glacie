using System;

namespace Glacie.Metadata.V1
{
    internal static class Constants
    {
        [Obsolete("This should come from TargetType.")]
        public const Path1Form TemplatePathForm = Path1Form.Relative
                | Path1Form.Strict
                | Path1Form.Normalized
                | Path1Form.DirectorySeparator
                | Path1Form.LowerInvariant;
    }
}
