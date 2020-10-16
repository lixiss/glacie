using System;

using Glacie.Data.Arz;

namespace Glacie.Modules
{
    internal sealed class ModuleDatabase : IDatabaseInfo, IDisposable
    {
        private readonly string? _physicalPath;
        private ArzDatabase? _database;
        private readonly bool _disposeDatabase;
        private bool _disposed;

        public ModuleDatabase(string? physicalPath, ArzDatabase? database, bool disposeDatabase)
        {
            Check.That(physicalPath != null || database != null);

            _physicalPath = physicalPath;
            _database = database;
            _disposeDatabase = database != null ? disposeDatabase : true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var database = _database;
                if (database != null)
                {
                    _database = null;

                    if (_disposeDatabase)
                    {
                        database.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        public string? PhysicalPath => _physicalPath;

        public ArzDatabase? Database => _database;

        // TODO: Add opening hints. Also libdeflate configuration are also will be good. How pass this info into this class?
        public ArzDatabase? Open()
        {
            if (_database != null) return _database;

            lock (this)
            {
                if (_database != null) return _database;
                Check.That(_physicalPath != null);

                // TODO: Expose Diagnostics if it failed.
                return _database = ArzDatabase.Open(_physicalPath, new ArzReaderOptions
                {
                    Mode = ArzReadingMode.Raw,
                });
            }
        }
    }
}
