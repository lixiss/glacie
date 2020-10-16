using System;
using System.Collections.Generic;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Configuration;
using Glacie.Data.Arz;
using Glacie.Diagnostics;
using Glacie.Infrastructure;
using Glacie.Logging;
using Glacie.Metadata;
using Glacie.Modules;
using Glacie.Resources;
using Glacie.Targeting;

namespace Glacie
{
    // TODO: Not sure how DiagnosticReporter should work... Context already hold diagnostic bag...
    // TODO: With build pipeline, context probably is not a top-level class. Declare responsibility of context.
    // Access to: Records, Resources, Text resources, reporting errors.

    public sealed class Context : IDisposable, IDiagnosticReporter
    {
        private Database? _database;

        // TODO: Move out. Deal with diagnostics.
        private readonly EngineType _engineType;
        private readonly Logger _log;
        private readonly IDiagnosticListener _diagnosticListener;
        private readonly DiagnosticBag _diagnosticBag;

        private readonly SourceModule[] _sources;
        private readonly TargetModule _target;

        private readonly IProjectContext _projectContext;

        // TODO: There is wrong.
        private readonly ResourceProvider _resourceProvider;
        private readonly ResourceResolver _resourceResolver;

        private bool _disposed;

        #region Construction

        // TODO: EngineType should not be passed in this way, it should be transferred in other way,
        // e.g. only necessary parts or pass ProjectContext.
        public Context(
            IProjectContext projectContext,
            //// TODO: this parameters are obsolete
            //EngineType engineType,
            //Logger logger,
            //DiagnosticReporter diagnosticReporter,

            TargetModule targetModule, SourceModule[] sourceModules,

            ResourceManager ___resourceManager___SHOULD_NOT_BE_PASSED_HERE)
        {
            Check.Argument.NotNull(projectContext, nameof(projectContext));
            Check.Argument.NotNull(targetModule, nameof(targetModule));
            Check.Argument.NotNull(sourceModules, nameof(sourceModules));
            _projectContext = projectContext;
            _target = targetModule;
            _sources = sourceModules;

            // TODO: resolve logger, and rest
            _engineType = projectContext.Services.Resolve<EngineType>();
            _log = projectContext.Services.Resolve<Logger>(); // TODO: this should be Logger<Context> or so,
                                                              // also database should use Logger<Database>
            _diagnosticListener = projectContext.Services.Resolve<IDiagnosticListener>();
            _diagnosticBag = new DiagnosticBag();

            // TODO: Create context-specific resource provider and resource resolver, based on modules
            // but they should be in DI context. (generally project/host factory may do that for
            // context).
            _resourceProvider = ___resourceManager___SHOULD_NOT_BE_PASSED_HERE;
            _resourceResolver = ___resourceManager___SHOULD_NOT_BE_PASSED_HERE.AsResolver();

            Open();

            // TODO: do we want open context immediately or better do it in half-open state?
            // probably better to go over ProjectContext (which act as Context-factory)
        }

        private void Open()
        {
            // TODO: ensure what all databases in the same format. diagnostics...


            // Should read records from sources (from target and sources in backward)
            var database = new Database(this,
                _target.GetDatabase(),
                GetEngineType().DatabaseConventions);

            // TODO: (Gx) alternatively, instead of making record mapping, we might
            // just merge down into single in-memory ArzDatabase. However this
            // may block some features (this would require almost full reencoding).

            int moduleIndex = 0;
            foreach (var module in GetAllModules())
            {
                if (module.HasDatabase)
                {
                    Log.Information("#{0}: {1}", moduleIndex, module.DatabaseInfo?.PhysicalPath ?? "<in-memory>");

                    // TODO: This might need some optimization or configuration, about database reading mode.
                    // 1. For hybrid mode, we might use alternative approaches: target database exist partially.
                    // 2. We should understand if we can derive string table and which.
                    //    Database which hold most of records is most likely to derive from.
                    // 3. If string table can be derived -> we can use Raw mode for record data importing.
                    // 4. Generally, when multithreading enabled, database better to be in full mode. (it depends)
                    // 5. Generally, best way is to open database in Lazy mode, and then perform bulk-reads if need. (Need support in ArzDatabase)
                    // Generally priority should be to avoid less re-encodings as possible.
                    var db = module.GetDatabase();
                    database.LinkRecordReferences(db);
                    // TODO: instead of Import -> use Adopt: adopt should transfer record(s) from
                    // another database, and remove it from owner (e.g. it may modify source), however
                    // this might be done semi-transparent (file-based databased can lazily recover
                    // non-modified content)
                }
                else
                {
                    // TODO: this is obsolete, i doesn't want to support custom module types.
                    // even if they will be supported -> they should be done in transparent way for
                    // context module reading.

                    // When reading DBR's we doesn't want to materialize them as separate
                    // database, so and this will need special support.
                    // Also DBR's can be built into ArzDatabase in form of cache (so they might be actually imported/used easier)
                    // Combining read/save + timestamps can achieve this relatively easy (rebuilding as cache).
                    throw Error.NotImplemented("IDatabaseProvider.CanProvideDatabase == false");
                }

                moduleIndex++;
            }

            _database = database;

            Check.True(_database != null);

            IEnumerable<Module> GetAllModules()
            {
                yield return _target;
                for (var i = 0; i < _sources.Length; i++)
                {
                    yield return _sources[i];
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var source in _sources)
                {
                    source?.Dispose();
                }
                _target?.Dispose();

                _disposed = true;
            }
        }

        public Logger Log => _log;

        // public IReadOnlyCollection<ISource> Sources => _sources;

        // public ITarget Target => _target;

        // TODO: Context should expose own services, ResourceResolver is specific
        // for context scope.
        public Glacie.Abstractions.IServiceProvider Services => _projectContext.Services;

        [Obsolete]
        public ResourceProvider ResourceProvider => _resourceProvider;

        [Obsolete]
        public ResourceResolver ResourceResolver => _resourceResolver;


        public Database Database => _database!;

        // There is Context Services...
        // public MetadataResolver MetadataResolver => _metadataProvider;
        // public ResourceResolver ResourceResolver => _resourceProvider;
        // Need record resolver

        // TODO: Not sure what this should be exposed...
        public DiagnosticBag GetDiagnosticBag() => _diagnosticBag;

        public void Report(Diagnostic diagnostic)
        {
            _diagnosticBag.Add(diagnostic);
            _diagnosticListener.Write(diagnostic);
        }

        // TODO: Probably it will have this call, if appropriate service will be
        // registered.


        // TODO: (Gx) Select Records
        // TODO: (Gx) Access to Resources

        [Obsolete("Should be part of Project Context.")]
        private EngineType GetEngineType() => _engineType;
    }
}
