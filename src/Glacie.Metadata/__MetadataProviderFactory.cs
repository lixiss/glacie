using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Metadata.Builders.Templates;
using Glacie.Metadata.Builders.Ephemeral;
using Glacie.Metadata.Builders.Glacie;
using Glacie.Resources;
using Glacie.Targeting;
using Glacie.Logging;

namespace Glacie.Metadata
{
    public static class __MetadataProviderFactory
    {
        public static __MetadataProvider Create(string path, EngineType? engineType, Logger? logger)
        {
            // TODO: ...
            return CreateTemplates(path, engineType, logger);
        }

        public static __MetadataProvider CreateEphemeral(ArzDatabase database)
        {
            throw Error.NotImplemented();
        }

        // TODO: still want engine autodetection
        public static __MetadataProvider CreateTemplates(string path, EngineType? engineType, Logger? logger)
        {
            var templatesLoader = new __TemplatesLoader(engineType, logger);
            return templatesLoader.CreateProvider(path);
        }

        public static __MetadataProvider CreateTemplates(ResourceProvider resourceProvider)
        {
            throw Error.NotImplemented();
        }
    }
}
