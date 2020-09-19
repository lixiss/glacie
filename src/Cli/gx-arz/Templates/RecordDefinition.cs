using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Glacie.Cli.Arz.Templates
{
    [Obsolete("Remove me.", true)]
    public sealed class RecordDefinition
    {
        private readonly string _name;
        private readonly List<FieldDefinition> _fields;
        private readonly Dictionary<string, FieldDefinition> _map;

        internal RecordDefinition(string name, List<FieldDefinition> fields)
        {
            Check.Argument.NotNull(name, nameof(name));
            Check.Argument.NotNull(fields, nameof(fields));

            _name = name;
            _fields = fields;
            _map = CreateFieldDefinitionMap(fields);
        }

        internal RecordDefinition(string name, List<FieldDefinition> fields, Dictionary<string, FieldDefinition> map)
        {
            Check.Argument.NotNull(name, nameof(name));
            Check.Argument.NotNull(fields, nameof(fields));
            Check.Argument.NotNull(map, nameof(map));

            _name = name;
            _fields = fields;
            _map = map;
        }

        public string Name => _name;

        public IEnumerable<FieldDefinition> GetFieldDefinitions()
        {
            return _fields;
        }

        public FieldDefinition GetFieldDefinition(string name)
        {
            return _map[name];
        }

        public bool TryGetFieldDefinition(string name, [NotNullWhen(true)] out FieldDefinition? fieldDefinition)
        {
            return _map.TryGetValue(name, out fieldDefinition);
        }

        private static Dictionary<string, FieldDefinition> CreateFieldDefinitionMap(List<FieldDefinition> fieldDefinitions)
        {
            var map = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
            foreach (var x in fieldDefinitions)
            {
                map.Add(x.Name, x);
            }
            return map;
        }
    }
}