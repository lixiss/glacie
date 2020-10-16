using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Data.Metadata.V1.Emit
{
    public sealed class FieldGroupBuilder
    {
        private FieldGroupDefinition? _built;

        private DatabaseTypeBuilder _declaringDatabaseDefinition;
        private FieldGroupBuilder? _parent;
        private List<FieldGroupBuilder>? _children;
        private readonly int _id;
        private string? _name;
        private bool _system;

        internal FieldGroupBuilder(
            DatabaseTypeBuilder declaringDatabaseDefinition,
            FieldGroupBuilder? parent,
            string? name,
            bool system)
        {
            _declaringDatabaseDefinition = declaringDatabaseDefinition;
            _parent = parent;
            _id = declaringDatabaseDefinition.GetNextFieldGroupIdentifier();
            _name = name;
            _system = system;
        }

        public DatabaseTypeBuilder DeclaringDatabaseDefinition => _declaringDatabaseDefinition;

        public FieldGroupBuilder? Parent => _parent;

        public IEnumerable<FieldGroupBuilder> Children => _children
            ?? Enumerable.Empty<FieldGroupBuilder>();

        public int Id => _id;

        public string? Name => _name;

        public bool System => _system;

        public bool TryGetFieldGroupDefinition(string name,
            [NotNullWhen(returnValue: true)] out FieldGroupBuilder? result)
        {
            if (_children == null)
            {
                result = null;
                return false;
            }

            foreach (var x in _children)
            {
                if (x.Name == name)
                {
                    result = x;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public FieldGroupBuilder DefineFieldGroupDefinition(string name, bool system)
        {
            var result = new FieldGroupBuilder(DeclaringDatabaseDefinition, this, name, system);
            if (_children == null) _children = new List<FieldGroupBuilder>();
            _children.Add(result);
            return result;
        }

        internal FieldGroupDefinition Build()
        {
            // Check.That(_built == null); // TODO: Might be called multiple times.
            if (_built != null) return _built;

            var fieldGroupDefinition = new FieldGroupDefinition(
                id: _id,
                name: _name,
                system: _system);

            if (_children != null)
            {
                foreach (var x in _children)
                {
                    fieldGroupDefinition.Add(x.Build());
                }
            }

            return _built = fieldGroupDefinition;
        }
    }
}
