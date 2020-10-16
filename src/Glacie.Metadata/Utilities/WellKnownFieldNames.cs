using System;

namespace Glacie.Metadata
{
    // TODO: There is 3 or 4 places of this class..
    // Data.Arz, Cli.Arz and Glacie.Metadata.

    // TODO: ArzSpecialFieldNames? and move to Glacie.Core/Glacie.Data.

    [Obsolete("Give this class good name. ArzSpecialFieldNames.")]
    internal static class WellKnownFieldNames
    {
        public const string TemplateName = "templateName";
        public const string Class = "Class";
    }
}
