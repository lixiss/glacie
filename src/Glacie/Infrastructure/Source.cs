using System;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using IO = System.IO;

namespace Glacie.Infrastructure
{
    internal sealed class Source : SourceOrTarget, IDisposable, ISource
    {
        private readonly int _identifier;
        private readonly string? _path;
        private ArzDatabase? _database;
        private bool _shouldDisposeDatabase;

        public Source(int identifier, string? path, ArzDatabase? database, bool shouldDisposeDatabase)
        {
            Check.True(identifier > 0);

            _identifier = identifier;
            _path = path;
            _database = database;

            _shouldDisposeDatabase = shouldDisposeDatabase;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_shouldDisposeDatabase)
                {
                    _database?.Dispose();
                }
                _database = null;
            }
        }

        public override int Identifier => _identifier;

        public string? Path => _path;

        public ArzDatabase? Database => _database;

        #region IDatabaseProvider

        public override bool CanProvideDatabase => true;

        public override ArzDatabase GetDatabase()
        {
            if (_database != null) return _database;

            if (_path != null)
            {
                Check.True(_database == null);
                if (IO.File.Exists(_path))
                {
                    _database = ArzDatabase.Open(_path, new ArzReaderOptions { Mode = ArzReadingMode.Lazy });
                    _shouldDisposeDatabase = true;
                    return _database;
                }
                else
                {
                    // TODO: (Gx) Handle source/target paths.
                    throw Error.NotImplemented();
                }
            }
            else if (_database != null)
            {
                return _database;
            }
            else throw Error.Unreachable();
        }

        #endregion
    }
}
