using System.Collections.Generic;
using System.Linq;

namespace Glacie.Data.Metadata
{
    public sealed class FieldGroupDefinition
    {
        // Hold reference to parent FieldGroupDefinition or DeclaringDatabaseDefinition.
        private object? _parent;

        private readonly int _id;
        private readonly string? _name;
        private readonly bool _system;
        private List<FieldGroupDefinition>? _children;

        #region Construction

        internal FieldGroupDefinition(
            int id,
            string? name,
            bool system)
        {
            _id = id;
            _name = name;
            _system = system;
        }

        internal void Add(FieldGroupDefinition value)
        {
            if (_children == null) _children = new List<FieldGroupDefinition>();

            _children.Add(value);
            value.AttachTo(this);
        }

        internal void AttachTo(FieldGroupDefinition parent)
        {
            Check.That(_parent == null);
            _parent = parent;
        }

        internal void AttachTo(DatabaseType parent)
        {
            Check.That(_parent == null);
            _parent = parent;
        }

        #endregion

        public DatabaseType DeclaringDatabaseDefinition
        {
            get
            {
                var n = _parent;
                while (n is FieldGroupDefinition x) n = x.Parent;
                return (n as DatabaseType)!;
            }
        }

        public FieldGroupDefinition? Parent => _parent as FieldGroupDefinition;

        public IEnumerable<FieldGroupDefinition> Children => _children ?? Enumerable.Empty<FieldGroupDefinition>();

        /// <summary>
        /// Unique group identifier (in DatabaseDefinition scope).
        /// </summary>
        public int Id => _id;

        public string? Name => _name;

        public bool System => _system;
    }
}
