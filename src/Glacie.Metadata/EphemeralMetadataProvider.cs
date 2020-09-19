using System;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Arz;
using Glacie.Data.Metadata;
using Glacie.Data.Metadata.Emit;
using Glacie.Logging;

namespace Glacie.Metadata
{
    // TODO: MetadataBuilderProvider?
    public sealed class EphemeralMetadataProvider : MetadataProvider
    {
        private ArzDatabase? _database;
        private readonly bool _disposeDatabase;
        private readonly Logger _log;

        private DatabaseTypeBuilder _databaseTypeBuilder;

        public EphemeralMetadataProvider(ArzDatabase database, Logger? logger, bool disposeDatabase)
        {
            _database = database;
            _disposeDatabase = disposeDatabase;
            _log = logger ?? Logger.Null;

            _databaseTypeBuilder = new DatabaseTypeBuilder();
            // _recordDefinitionMap = new Dictionary<string, RecordDefinition>(StringComparer.Ordinal);
            // _cachedStringTransform = new CachedStringTransform();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposeDatabase)
            {
                _database?.Dispose();
                _database = null;
            }
            base.Dispose(disposing);
        }

        public override RecordType GetByName(string name)
        {
            throw Error.NotImplemented();
        }

        public override DatabaseType GetDatabaseType()
        {
            throw Error.NotImplemented();
        }

        public override bool TryGetByTemplateName(in VirtualPath templateName,
            [NotNullWhen(returnValue: true)] out RecordType? result)
        {
            // TODO: we can avoid normalization if we check map before normalization, and
            // fallback to normalization only when record is not found?

            // TODO: need templateName normalization
            VirtualPath vp = templateName;
            vp = vp.Normalize(VirtualPathNormalization.Standard);

            if (!_databaseTypeBuilder.TryGetRecordDefinition(vp, out var recordTypeBuilder))
            {
                recordTypeBuilder = CreateRecordTypeBuilderOrDefault(vp);
            }
            result = recordTypeBuilder?.CreateRecordType();
            return result != null;
        }

        private RecordTypeBuilder? CreateRecordTypeBuilderOrDefault(VirtualPath templateName)
        {
            Check.That(_database != null);

            // TODO: (Low) EmphemeralMetadataProvider should raise diagnostic message.
            // Normally it should be warning, but sometimes it is might be suppressed...
            _log.Trace("Creating ephemeral record type: \"{0}\"", templateName);

            RecordTypeBuilder? builder = null;

            foreach (var record in _database.GetAll()) // TODO: SelectByTemplateName()
            {
                if (record.TryGet(WellKnownFieldNames.TemplateName, ArzRecordOptions.NoFieldMap, out var templateNameField))
                {
                    var templateNameValue = templateNameField.Get<string>();
                    if (templateName.Equals(templateNameValue, VirtualPathComparison.NonStandard))
                    {
                        if (builder == null)
                        {
                            builder = _databaseTypeBuilder.DefineRecordDefinition(templateName);
                        }

                        foreach (var field in record.GetAll())
                        {
                            if (!builder.TryGetFieldType(field.Name, out var _))
                            {
                                var fieldType = builder.DefineFieldDefinition(
                                    _databaseTypeBuilder.RootFieldGroupDefinition,
                                    field.Name);

                                fieldType.ValueType = field.ValueType;
                                fieldType.Array = true;
                            }
                        }
                    }
                }
            }

            return builder;
        }
    }
}
