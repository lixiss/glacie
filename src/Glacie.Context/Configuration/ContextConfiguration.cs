using System.Collections.Generic;

using Glacie.Diagnostics;
using Glacie.Logging;
using Glacie.Resources;

namespace Glacie.Configuration
{
    // TODO: Fluent extensions for context configuration would be nice.

    public sealed class ContextConfiguration
    {
        private readonly List<ContextSourceConfiguration> _sources;

        public ContextConfiguration()
        {
            _sources = new List<ContextSourceConfiguration>();
        }

        public Logger? Logger { get; set; }

        public DiagnosticListener? DiagnosticReporter { get; set; }

        public IList<ContextSourceConfiguration> Sources => _sources;

        public ContextTargetConfiguration? Target { get; set; }

        // TODO: IntermediateOutputPath

        // public Path1Mapper? RecordNameMapper { get; set; }

        public ContextMetadataConfiguration? Metadata { get; set; }

        /// TODO: There is currently hack. Context should take control over whole VFS or ResourceManager. Should be configured automatically.
        public ResourceResolver? ResourceResolver { get; set; }

        // TODO: EnginePath and EngineType
    }
}
