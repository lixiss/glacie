using Glacie.Abstractions;
using Glacie.Diagnostics;
using Glacie.Logging;
using Glacie.Metadata;
using Glacie.Resources;

namespace Glacie.Validation
{
    public sealed class ValidationConfiguration
    {
        public Logger? Logger { get; set; }

        public IDiagnosticReporter DiagnosticReporter { get; set; } = default!;

        public MetadataResolver? MetadataResolver { get; set; }

        public RecordResolver? RecordResolver { get; set; }

        public ResourceResolver? ResourceResolver { get; set; }

        public bool ResolveReferences { get; set; }
    }
}
