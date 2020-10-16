using System;
using Glacie.Abstractions;
using Glacie.Data.Arz;

namespace Glacie.Infrastructure
{
    internal sealed class Target : SourceOrTarget, IDisposable, ITarget
    {
        private readonly string? _path;
        private ArzDatabase? _database;
        private bool _disposeDatabase;

        public Target(string? path, ArzDatabase? database, bool disposeDatabase)
        {
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

        public override int Identifier => 0;

        public string? Path => _path;

        public ArzDatabase? Database => _database;

        #region IDatabaseProvider

        public override bool CanProvideDatabase => true;

        public override ArzDatabase GetDatabase()
        {
            if (_database != null) return _database;

            // TODO: (Gx) Target: Should open/assign database from specified path,
            // but it has other meaning - database should be clean anyway.
            // E.g. we may import something for hybrid like string table,
            // but not any data.
            // TODO: (Gx) Because Glacie.Database has record mapping itself,
            // we might want suppress record map in this database, need option
            // for this. And generally record maps will not need with other
            // databases, because only sequential scan is need for importing.
            _database = ArzDatabase.Create();
            _disposeDatabase = true;
            return _database;
        }

        #endregion
    }
}
