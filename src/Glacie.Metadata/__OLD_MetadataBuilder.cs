using System;

using Glacie.Logging;
using Glacie.Metadata.Builder;
using Glacie.Resources;
using Glacie.Targeting;

namespace Glacie.Metadata
{
    // TODO: MetadataBuilder probably can be MetadataProvider (abstract)
    // there is more like a factory.........
    // Keep it for a while, but later can be removed if not need.

    internal sealed class __OLD_MetadataBuilder : IDisposable
    {
        private bool _disposed;
        private readonly Logger? _log;

        private MetadataBuilder? _databaseTypeBuilder;
        private DatabaseType? _databaseType;

        public __OLD_MetadataBuilder(Logger? logger)
        {
            _log = logger ?? Logger.Null;

            // If databaseTypeBuilder need any data - there is time to configure it.
            _databaseTypeBuilder = new MetadataBuilder();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Add disposal logic here.

                _disposed = true;
            }
        }

        // TODO: probably just return null
        public MetadataBuilder GetDatabaseTypeBuilder()
        {
            if (_databaseTypeBuilder != null)
            {
                return _databaseTypeBuilder;
            }

            throw Error.InvalidOperation("DatabaseType was already requested, and access to DatabaseTypeBuilder is blocked.");
        }

        public DatabaseType GetDatabaseType()
        {
            if (_databaseType != null)
            {
                return _databaseType;
            }

            if (_databaseTypeBuilder != null)
            {
                _databaseType = _databaseTypeBuilder.Build();
                _databaseTypeBuilder = null;
                return _databaseType;
            }

            throw Error.Unreachable();
        }

        public void Load(string path)
        {
            var engineType = new TqaeEngineType();
            // TODO: should be part of MetadataBuilder configuration,
            // generally only answer to some questions.

        }
    }
}
