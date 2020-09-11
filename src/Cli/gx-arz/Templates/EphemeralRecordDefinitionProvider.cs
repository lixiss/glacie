using System;
using System.Collections.Generic;

using Glacie.Data.Arz;
using Glacie.Logging;

namespace Glacie.Cli.Arz.Templates
{
    internal sealed class EphemeralRecordDefinitionProvider : IRecordDefinitionProvider
    {
        private readonly ArzDatabase _database;
        private readonly Logger _log;

        private Dictionary<string, RecordDefinition> _recordDefinitionMap;
        private CachedStringTransform _cachedStringTransform;

        public EphemeralRecordDefinitionProvider(ArzDatabase database, Logger logger)
        {
            _database = database;
            _log = logger;
            _recordDefinitionMap = new Dictionary<string, RecordDefinition>(StringComparer.Ordinal);
            _cachedStringTransform = new CachedStringTransform();
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        public RecordDefinition GetRecordDefinition(string name)
        {
            // TODO: we can avoid normalization if we check map before normalization, and
            // fallback to normalization only when record is not found?

            name = NormalizeTemplateName(name);

            if (!_recordDefinitionMap.TryGetValue(name, out var recordDefinition))
            {
                recordDefinition = CreateRecordDefinition(name);
                _recordDefinitionMap.Add(name, recordDefinition);
            }
            return recordDefinition;
        }

        private RecordDefinition CreateRecordDefinition(string name)
        {
            _log.Trace("Creating ephemeral record definition: \"{0}\"", name);

            var found = false;
            var builder = new RecordDefinitionBuilder();
            builder.SetName(name);

            foreach (var record in _database.GetAll()) // TODO: select all records filtered by templateName
            {
                if (record.TryGet(WellKnownFieldNames.TemplateName, ArzRecordOptions.NoFieldMap, out var templateNameField))
                {
                    var templateNameValue = templateNameField.Get<string>();
                    if (TemplateNameEquals(name, templateNameValue))
                    {
                        found = true;

                        foreach (var field in record.GetAll())
                        {
                            if (!builder.HasFieldDefinition(field.Name))
                            {
                                var fieldDefinition = new FieldDefinition(field.Name, field.ValueType);
                                builder.AddFieldDefinition(fieldDefinition);
                            }
                        }
                    }
                }
            }

            if (found)
            {
                return builder.Build();
            }
            else
            {
                // TODO: use diagnostics 
                throw Error.InvalidOperation("Can't find record definition.");
            }
        }

        private string NormalizeTemplateName(string value)
        {
            return _cachedStringTransform.Get(value);
        }

        private bool TemplateNameEquals(string a, string b)
        {
            // TODO: Here we need special comparer (case-insensitive and slash-insensitive).
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
