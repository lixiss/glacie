using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Metadata.Builder
{
    public sealed class FieldVarGroupBuilder
        : Infrastructure.Builder<FieldVarGroup>
        , IFieldVarGroupBuilderContract
    {
        private MetadataBuilder _declaringDatabaseType;
        private FieldVarGroupBuilder? _parent;
        private List<FieldVarGroupBuilder>? _children;
        private readonly int _id;
        private string? _name;
        private bool _system;

        internal FieldVarGroupBuilder(
            MetadataBuilder declaringDatabaseType,
            FieldVarGroupBuilder? parent,
            string? name,
            bool system)
        {
            _declaringDatabaseType = declaringDatabaseType;
            _parent = parent;
            _id = declaringDatabaseType.GetNextFieldVarGroupIdentifier();
            _name = name;
            _system = system;
        }

        public MetadataBuilder DeclaringDatabaseType => _declaringDatabaseType;

        public FieldVarGroupBuilder? Parent => _parent;

        public IEnumerable<FieldVarGroupBuilder> Children => _children
            ?? Enumerable.Empty<FieldVarGroupBuilder>();

        public int Id => _id;

        public string? Name
        {
            get => _name;
            set { ThrowIfBuilt(); _name = value; }
        }

        public bool System
        {
            get => _system;
            set { ThrowIfBuilt(); _system = value; }
        }

        // TODO: (Glacie.Data.Metadata) Expose method in interface & concrete FieldVarGroup.
        public bool TryGetFieldVarGroup(string name,
            [NotNullWhen(returnValue: true)] out FieldVarGroupBuilder? result)
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

        public FieldVarGroupBuilder DefineFieldVarGroup(string name, bool system = false)
        {
            ThrowIfBuilt();

            var result = new FieldVarGroupBuilder(DeclaringDatabaseType, this, name, system);
            if (_children == null) _children = new List<FieldVarGroupBuilder>();
            _children.Add(result);
            return result;
        }

        protected override FieldVarGroup BuildCore()
        {
            var result = new FieldVarGroup(
                id: _id,
                name: _name,
                system: _system);

            if (_children != null)
            {
                foreach (var x in _children)
                {
                    result.Add(x.Build());
                }
            }

            return result;
        }
    }
}
