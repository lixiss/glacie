using System.Collections.Generic;
using System.Collections.Immutable;

using Glacie.Abstractions;
using Glacie.Data.Arz;
using Glacie.Diagnostics;
using Glacie.Localization;
using Glacie.Resources;
using Glacie.Resources.Builder;

using IO = System.IO;

namespace Glacie.Modules.Builder
{
    public sealed class ModuleBuilder : IModuleInfo
    {
        private string? _physicalPath;
        private ModuleDatabaseBuilder? _databaseBuilder;
        private readonly ResourceBundleSetBuilder _resourceBundleSet;
        private readonly DiagnosticBag _diagnostics;

        public ModuleBuilder()
        {
            _resourceBundleSet = new ResourceBundleSetBuilder();
            _diagnostics = new DiagnosticBag();
        }

        public ImmutableArray<Diagnostic> GetDiagnostics()
            => _diagnostics.ToReadOnly();

        public string? PhysicalPath => _physicalPath;

        public IDatabaseInfo? DatabaseInfo => _databaseBuilder;

        public bool HasDatabase => _databaseBuilder != null;

        public IResourceBundleSet ResourceSet => _resourceBundleSet;

        //public IReadOnlyCollection<LanguageSymbol> Languages
        //    => _resourceBundlesMap.Keys;

        //public IReadOnlyCollection<IResourceBundleInfo> GetResourceBundles(LanguageSymbol languageSymbol)
        //    => _resourceBundlesMap[languageSymbol];

        public ModuleBuilder WithPhysicalPath(string path)
        {
            Check.Argument.NotNull(path, nameof(path));
            ThrowIfPhysicalPathAlreadyAssigned();
            path = IO.Path.GetFullPath(path);
            _physicalPath = path;
            return this;
        }

        public ModuleBuilder WithDatabase(string path)
        {
            Check.Argument.NotNull(path, nameof(path));
            ThrowIfDatabaseAlreadyAssigned();
            path = CheckAndNormalizePath(path);
            _databaseBuilder = new ModuleDatabaseBuilder(physicalPath: path);
            return this;
        }

        public ModuleBuilder WithDatabase(ArzDatabase database, bool disposeDatabase = false)
        {
            Check.Argument.NotNull(database, nameof(database));
            ThrowIfDatabaseAlreadyAssigned();
            _databaseBuilder = new ModuleDatabaseBuilder(physicalPath: null, database, disposeDatabase);
            return this;
        }

        // TODO: (Low) Need a way to pass explicit bundles, like with databases
        public ModuleBuilder AddResourceBundle(ResourceBundleKind kind,
            string path,
            string prefix,
            LanguageInfo languageInfo,
            bool isOverride) // TODO: instead of isOverride, pass priority
        {
            Check.Argument.NotNull(path, nameof(path));
            Check.Argument.NotNull(languageInfo, nameof(languageInfo));

            path = CheckAndNormalizePath(path);

            var languageSymbol = languageInfo.Symbol;

            var priority = GetPriority(isOverride, languageSymbol);

            var resourceBundle = new ResourceBundleBuilder(kind, path, priority, prefix, languageSymbol);
            _resourceBundleSet.Add(resourceBundle);

            return this;
        }

        public SourceModule CreateSourceModule()
        {
            var result = new SourceModule(_physicalPath,
                _databaseBuilder?.Build(),
                _resourceBundleSet.Build());
            return result;
        }

        private short GetPriority(bool isOverride, LanguageSymbol languageSymbol)
        {
            short priority = isOverride ? (short)2 : (short)0;
            if (languageSymbol != LanguageSymbol.Invariant) priority++;
            return priority;
        }

        private void CheckPathIsFullyQualified(string value)
        {
            if (!IO.Path.IsPathFullyQualified(value))
                throw Error.Argument(nameof(value), "Path must be fully qualified.");
        }

        private void ThrowIfPhysicalPathAlreadyAssigned()
        {
            if (_physicalPath != null)
            {
                throw Error.InvalidOperation("PhysicalPath already assigned.");
            }
        }

        private void ThrowIfDatabaseAlreadyAssigned()
        {
            if (_databaseBuilder != null)
            {
                throw Error.InvalidOperation("Database already assigned.");
            }
        }

        private string CheckAndNormalizePath(string path)
        {
            if (_physicalPath == null)
            {
                CheckPathIsFullyQualified(path);
            }
            else
            {
                path = IO.Path.GetFullPath(IO.Path.Combine(_physicalPath, path));
            }

            return path;
        }
    }
}
