using System;

using Glacie.Data.Arz;

namespace Glacie.Cli.Arz.Templates
{
    [Obsolete("Remove me.", true)]
    public sealed class FieldDefinition
    {
        private readonly string _name;
        private readonly ArzValueType _valueType;

        internal FieldDefinition(string name, ArzValueType valueType)
        {
            Check.Argument.NotNull(name, nameof(name));

            _name = name;
            _valueType = valueType;
        }

        public string Name => _name;

        public bool HasDefaultValue => DefaultValue != null;

        public string? DefaultValue => null;

        public ArzValueType ValueType => _valueType;
    }
}
