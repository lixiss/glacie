using System;
using System.Collections.Generic;
using System.Text;
using Glacie.Abstractions;
using Glacie.Data.Arz;

namespace Glacie.Infrastructure
{
    internal sealed class TargetBuilder : ITargetBuilder
    {
        private string? _path;
        private ArzDatabase? _database;

        public Target Build()
        {
            // Doesn't dispose database when it provided explicitly.
            var shouldDisposeDatabase = _database == null;

            return new Target(
                path: _path,
                database: _database,
                shouldDisposeDatabase: shouldDisposeDatabase);
        }

        public void Validate()
        {
            if (_path == null && _database == null)
            {
                throw GxError.TargetConfigurationInvalid("Context have target, but it not configured. Target requires path or database be specified.");
            }

            if (_path != null && _database != null)
            {
                throw GxError.TargetConfigurationInvalid("Target configured with path and database. Use path or database instead.");
            }
        }

        void ITargetBuilder.Path(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));
            _path = path;
        }

        void ITargetBuilder.Database(ArzDatabase database)
        {
            Check.Argument.NotNull(database, nameof(database));
            _database = database;
        }
    }
}
