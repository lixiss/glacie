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
        private bool _disposeDatabase;

        public Source(int identifier, string? path, ArzDatabase? database, bool disposeDatabase)
        {
            Check.True(identifier > 0);

            _identifier = identifier;
            _path = path;
            _database = database;

            _disposeDatabase = disposeDatabase;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposeDatabase)
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

            // TODO: Determine database reading mode

            if (_path != null)
            {
                Check.True(_database == null);
                if (IO.File.Exists(_path))
                {
                    _database = ArzDatabase.Open(_path, new ArzReaderOptions { Mode = ArzReadingMode.Raw });
                    _disposeDatabase = true;
                    return _database;
                }
                else
                {
                    // TODO: (Gx) Show proper error.
                    throw Error.InvalidOperation("Database file not found: \"{0}\".", _path);
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
