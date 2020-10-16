using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Glacie.Data.Arz;
using Glacie.Diagnostics;
using Glacie.Localization;
using Glacie.Resources;

using IO = System.IO;

namespace Glacie.Modules
{
    public abstract class Module : IModuleInfo
    {
        private readonly string? _physicalPath;
        private readonly ModuleDatabase? _database;
        private readonly ResourceBundleSet _resourceBundleSet;
        private readonly DiagnosticBag _diagnostics;
        private bool _disposed;

        internal Module(string? physicalPath, ModuleDatabase? database, ResourceBundleSet resourceBundleSet)
        {
            if (physicalPath != null) { Check.That(IO.Path.IsPathFullyQualified(physicalPath)); }

            _physicalPath = physicalPath;
            _database = database;
            _resourceBundleSet = resourceBundleSet;
            _diagnostics = new DiagnosticBag();
        }

        protected void DisposeInternal()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                List<Exception>? exceptions = null;
                if (disposing)
                {
                    if (_database != null)
                    {
                        try
                        {
                            _database.Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (exceptions == null) exceptions = new List<Exception>();
                            if (ex is AggregateException x)
                            {
                                exceptions.AddRange(x.Flatten().InnerExceptions);
                            }
                            else
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }

                    if (_resourceBundleSet != null)
                    {
                        try
                        {
                            _resourceBundleSet.Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (exceptions == null) exceptions = new List<Exception>();
                            if (ex is AggregateException x)
                            {
                                exceptions.AddRange(x.Flatten().InnerExceptions);
                            }
                            else
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }
                }

                _disposed = true;

                if (disposing)
                {
                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
                    }
                }
            }
        }

        public ImmutableArray<Diagnostic> GetDiagnostics()
            => _diagnostics.ToReadOnly();

        public string? PhysicalPath => _physicalPath;

        public bool HasDatabase => _database != null;

        // TODO: Expose DatabaseSet, and let Context open it.
        public IDatabaseInfo? DatabaseInfo => _database;

        public IResourceBundleSet ResourceSet => _resourceBundleSet;

        //public IReadOnlyCollection<LanguageSymbol> Languages
        //    => _resourceBundleSet.Keys;

        //public IReadOnlyCollection<IResourceBundleInfo> GetResourceBundles(LanguageSymbol language)
        //    => _resourceBundleSet[language];

        public void Open()
        {
            if (_database != null)
            {
                _database.Open();
            }

            if (_resourceBundleSet != null)
            {
                _resourceBundleSet.Open();
            }
        }

        public ArzDatabase? GetDatabase()
        {
            return _database.Open();
        }
    }
}
