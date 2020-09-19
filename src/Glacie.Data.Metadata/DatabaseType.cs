using System;
using System.Collections.Generic;

namespace Glacie.Data.Metadata
{
    using RecordDefinitionMap = Dictionary<string, RecordType>;

    // TODO: IRecordTypeProvider
    public sealed class DatabaseType
    {
        private readonly RecordDefinitionMap _recordDefinitionMap;
        private FieldGroupDefinition? _rootFieldGroupDefinition;

        #region Construction

        internal DatabaseType()
        {
            _recordDefinitionMap = new RecordDefinitionMap(StringComparer.Ordinal);
        }

        internal void Add(RecordType recordDefinition)
        {
            _recordDefinitionMap.Add(recordDefinition.Name, recordDefinition);
            recordDefinition.AttachTo(this);
        }

        internal void Add(FieldGroupDefinition fieldGroupDefinition)
        {
            Check.That(_rootFieldGroupDefinition == null);
            _rootFieldGroupDefinition = fieldGroupDefinition;
            fieldGroupDefinition.AttachTo(this);
        }

        #endregion

        public FieldGroupDefinition RootFieldGroupDefinition => _rootFieldGroupDefinition!;

        public IEnumerable<RecordType> RecordTypes => _recordDefinitionMap.Values;

        // Lookup by record name
        // Lookup by template name
    }
}
