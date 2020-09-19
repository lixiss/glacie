using System;
using System.Collections.Generic;
using System.Linq;

namespace Glacie.Cli.Arz.Templates
{
    [Obsolete("Remove me.", true)]
    public sealed class RecordDefinitionBuilder
    {
        private string? _name;
        private List<FieldDefinition> _fields = new List<FieldDefinition>();
        private Dictionary<string, FieldDefinition> _map = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);

        public RecordDefinition Build()
        {
            Check.That(_name != null);

            return new RecordDefinition(_name, _fields, _map);
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public void AddFieldDefinition(FieldDefinition fieldDefinition)
        {
            if (_map.ContainsKey(fieldDefinition.Name))
            {
                throw Error.InvalidOperation("Field already defined.");
            }
            else
            {
                _map.Add(fieldDefinition.Name, fieldDefinition);
                _fields.Add(fieldDefinition);
            }
        }

        public bool HasFieldDefinition(string name)
        {
            return _map.ContainsKey(name);
        }
    }
}
