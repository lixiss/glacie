using System.Collections.Generic;
using System.Linq;

namespace Glacie.Metadata
{
    /// <inheritdoc cref="IFieldVarGroupContract"/>
    public sealed class FieldVarGroup : IFieldVarGroupContract
    {
        /// <summary>Hold reference to parent <see cref="FieldVarGroup"/> or <see cref="DatabaseType"/> (for <see cref="DeclaringDatabaseType"/>).</summary>
        private object? _parent;

        private readonly int _id;
        private readonly string? _name;
        private readonly bool _system;
        private List<FieldVarGroup>? _children;

        #region Construction

        internal FieldVarGroup(
            int id,
            string? name,
            bool system)
        {
            _id = id;
            _name = name;
            _system = system;
        }

        internal void Add(FieldVarGroup value)
        {
            if (_children == null) _children = new List<FieldVarGroup>();

            _children.Add(value);
            value.AttachTo(this);
        }

        internal void AttachTo(FieldVarGroup parent)
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

        public DatabaseType DeclaringDatabaseType
        {
            get
            {
                var n = _parent;
                while (n is FieldVarGroup x) n = x._parent;
                return (n as DatabaseType)!;
            }
        }

        public FieldVarGroup? Parent => _parent as FieldVarGroup;

        public IEnumerable<FieldVarGroup> Children
            => _children ?? Enumerable.Empty<FieldVarGroup>();

        public int Id => _id;

        public string? Name => _name;

        public bool System => _system;
    }
}
