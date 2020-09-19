using System;

namespace Glacie.Data.Metadata.Emit
{
    public sealed class ExpressionVariableDeclarationBuilder
    {
        private readonly RecordTypeBuilder _declaringRecordDefinition;
        private readonly string _name;
        private string? _description;

        internal ExpressionVariableDeclarationBuilder(
            RecordTypeBuilder declaringRecordDefinition,
            string name)
        {
            _declaringRecordDefinition = declaringRecordDefinition;
            _name = name;
        }

        public RecordTypeBuilder DeclaringRecordDefinition => _declaringRecordDefinition;

        public string Name => _name;

        public string? Description
        {
            get => _description;
            set => _description = DescriptionUtilities.Normalize(value);
        }

        internal ExpressionVariableDeclaration Build()
        {
            return new ExpressionVariableDeclaration(
                name: _name,
                description: _description);
        }
    }
}
