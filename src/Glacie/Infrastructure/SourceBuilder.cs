using Glacie.Abstractions;
using Glacie.Data.Arz;

namespace Glacie.Infrastructure
{
    internal sealed class SourceBuilder : ISourceBuilder
    {
        private int _identifier;
        private string? _path;
        private ArzDatabase? _database;

        public Source Build()
        {
            // Doesn't dispose database when it provided explicitly.
            var shouldDisposeDatabase = _database == null;

            return new Source(
                identifier: _identifier,
                path: _path,
                database: _database,
                shouldDisposeDatabase: shouldDisposeDatabase);
        }

        public void Validate()
        {
            if (_path == null && _database == null)
            {
                throw GxError.SourceConfigurationInvalid("Context have target, but it not configured. Target requires path or database be specified.");
            }

            if (_path != null && _database != null)
            {
                throw GxError.SourceConfigurationInvalid("Target configured with path and database. Use path or database instead.");
            }
        }

        public void SetIdentifier(int value)
        {
            _identifier = value;
        }

        #region API

        void ISourceBuilder.Path(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));
            _path = path;
        }

        void ISourceBuilder.Database(ArzDatabase database)
        {
            Check.Argument.NotNull(database, nameof(database));
            _database = database;
        }

        #endregion
    }
}
