using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Data.Metadata.V1.Emit
{
    using RecordDefinitionMap = Dictionary<string, RecordTypeBuilder>;

    public sealed class DatabaseTypeBuilder
    {
        private DatabaseType? _built;

        private readonly RecordDefinitionMap _recordDefinitionMap;
        private readonly FieldGroupBuilder _rootFieldGroupDefinition;
        private int _fieldGroupDefinitionIdGenerator;

        public DatabaseTypeBuilder()
        {
            _recordDefinitionMap = new RecordDefinitionMap(StringComparer.Ordinal);
            _rootFieldGroupDefinition = new FieldGroupBuilder(this, parent: null, name: null, system: false);
        }

        public FieldGroupBuilder RootFieldGroupDefinition => _rootFieldGroupDefinition;

        //public bool TryGetFieldGroupDefinition(int id, [NotNullWhen(returnValue: true)] out FieldGroupDefinition? result)
        //    => throw Error.NotImplemented();

        //public FieldGroupDefinition GetFieldGroupDefinition(int id) => throw Error.NotImplemented();

        public bool TryGetRecordDefinition(string name,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result)
        {
            return _recordDefinitionMap.TryGetValue(name, out result);
        }

        //public bool TryGetRecordDefinition(NameSymbol nameSymbol,
        //    [NotNullWhen(returnValue: true)] out RecordDefinitionBuilder? result)
        //{
        //    throw Error.NotImplemented();
        //}

        public RecordTypeBuilder GetRecordDefinition(string name)
        {
            if (TryGetRecordDefinition(name, out var result))
            {
                return result;
            }
            throw Error.InvalidOperation("RecordDefinitionBuilder not found: \"{0}\".", name);
        }


        //public RecordDefinitionBuilder GetRecordDefinition(NameSymbol nameSymbol)
        //{
        //    if (TryGetRecordDefinition(nameSymbol, out var result))
        //    {
        //        return result;
        //    }
        //    throw Error.InvalidOperation("RecordDefinitionBuilder not found: \"{0}\".", nameSymbol.GetValue());
        //}

        public RecordTypeBuilder DefineRecordDefinition(string name) // TODO: also add NameSymbol version
        {
            if (TryGetRecordDefinition(name, out var _))
            {
                throw Error.InvalidOperation("Record definition \"{0}\" already defined.", name);
            }

            var result = new RecordTypeBuilder(this, name);
            _recordDefinitionMap.Add(result.Name, result);
            return result;
        }

        internal int GetNextFieldGroupIdentifier()
        {
            return _fieldGroupDefinitionIdGenerator++;
        }

        public DatabaseType CreateDatabaseDefinition()
        {
            if (_built != null) return _built;

            var databaseDefinition = new DatabaseType();

            foreach (var x in _recordDefinitionMap.Values)
            {
                databaseDefinition.Add(x.CreateRecordType());
            }

            databaseDefinition.Add(_rootFieldGroupDefinition.Build());

            return _built = databaseDefinition;
        }
    }
}
