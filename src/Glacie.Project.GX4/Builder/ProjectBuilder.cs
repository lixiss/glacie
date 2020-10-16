using System.Collections.Generic;
using System.Runtime.InteropServices;

using Glacie.Diagnostics;
using Glacie.Logging;
using Glacie.Metadata;
using Glacie.Metadata.Factories;
using Glacie.Modules;
using Glacie.Services;
using Glacie.Targeting;
using Glacie.Targeting.Default;
using Glacie.Targeting.TQAE;

using IO = System.IO;

namespace Glacie.ProjectSystem.Builder
{
    public sealed class ProjectBuilder
    {
        private List<string> _sources;
        private Logger? _logger;

        private string? _metadataPath;
        private MetadataProvider? _metadataProvider;
        private DiagnosticListener? _diagnosticListener;

        public ProjectBuilder()
        {
            _sources = new List<string>();
        }

        // TODO: string? PhysicalPath
        // TODO: string? ProjectDirectory

        public ProjectBuilder WithLogger(Logger logger)
        {
            if (_logger != null) throw Error.InvalidOperation("Logger already specified.");
            _logger = logger;
            return this;
        }

        public ProjectBuilder WithMetadata(string path)
        {
            if (_metadataPath != null) throw Error.InvalidOperation("MetadataPath already assigned.");
            if (_metadataProvider != null) throw Error.InvalidOperation("MetadataProvider already assigned.");
            _metadataPath = path;
            return this;
        }

        public ProjectBuilder WithMetadata(MetadataProvider metadataProvider)
        {
            if (_metadataProvider != null) throw Error.InvalidOperation("MetadataProvider already assigned.");
            if (_metadataPath != null) throw Error.InvalidOperation("MetadataPath already assigned.");
            _metadataProvider = metadataProvider;
            return this;
        }

        public ProjectBuilder WithDiagnosticListener(DiagnosticListener diagnosticListener)
        {
            if (_diagnosticListener != null) throw Error.InvalidOperation("DiagnosticListener already assigned.");
            _diagnosticListener = diagnosticListener;
            return this;
        }

        public ProjectBuilder AddSource(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));
            path = NormalizePath(path);
            _sources.Add(path);
            return this;
        }

        public Project Build()
        {
            // Determines engine, creates project context.
            var projectContext = new ProjectContext();

            // register service(s) required for module discovery
            // once service collection is complete, it must not be modified later...
            // otherwise dependent resolutions will not be updated.
            // projectContext.ServiceCollection.AddTransient<ILanguageProvider, DefaultLanguageProvider>();
            projectContext.ServiceCollection.AddSingleton<Logger>(_logger ?? Logger.Null);
            projectContext.ServiceCollection.AddSingleton<IDiagnosticListener>(_diagnosticListener ?? DiagnosticListener.Null);
            projectContext.ServiceCollection.AddTransient<ILanguageProvider, TqaeLanguageProvider>();
            projectContext.ServiceCollection.AddTransient<IModuleDiscoverer, ModuleDiscoverer>();

            projectContext.ServiceCollection.AddSingleton<EngineType, TqaeEngineType>();

            if (_metadataPath != null)
            {
                // TODO: register factory instead of direct
                var engineType = projectContext.Services.Resolve<EngineType>();
                var logger = projectContext.Services.Resolve<Logger>();
                _metadataProvider = MetadataProviderFactory.Create(_metadataPath, engineType: engineType,
                    logger);
            }

            if (_metadataProvider != null)
            {
                projectContext.ServiceCollection.AddSingleton<MetadataProvider>(_metadataProvider);

                // TODO: register factory instead of direct
                var engineType = projectContext.Services.Resolve<EngineType>();
                projectContext.ServiceCollection.AddSingleton<MetadataResolver>(
                    engineType.CreateMetadataResolver(_metadataProvider)
                    );
            }

            var moduleDiscoverer = projectContext.Services.Resolve<IModuleDiscoverer>();

            var sourceModules = new List<SourceModule>();
            foreach (var sourcePath in _sources)
            {
                var moduleBuilder = moduleDiscoverer.DiscoverModule(sourcePath);
                var sourceModule = moduleBuilder.CreateSourceModule();
                sourceModules.Add(sourceModule);
            }

            return new Project(projectContext, sourceModules);
        }

        private string NormalizePath(string path)
        {
            return IO.Path.GetFullPath(path);
        }
    }
}
