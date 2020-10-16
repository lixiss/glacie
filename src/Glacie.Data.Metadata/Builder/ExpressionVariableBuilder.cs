namespace Glacie.Metadata.Builder
{
    public sealed class ExpressionVariableBuilder
        : Infrastructure.Builder<ExpressionVariable>
        , IExpressionVariableBuilderContract
    {
        private readonly RecordTypeBuilder _declaringRecordType;
        private readonly string _name;
        private string? _description;

        internal ExpressionVariableBuilder(
            RecordTypeBuilder declaringRecordType,
            string name)
        {
            Check.Argument.NotNull(declaringRecordType, nameof(declaringRecordType));
            Check.Argument.NotNullNorEmpty(name, nameof(name));

            _declaringRecordType = declaringRecordType;
            _name = name;
        }

        public RecordTypeBuilder DeclaringRecordType => _declaringRecordType;

        public string Name => _name;

        public string? Documentation
        {
            get => _description;
            set { ThrowIfBuilt(); _description = DocumentationUtilities.Normalize(value); }
        }

        protected override ExpressionVariable BuildCore()
        {
            return new ExpressionVariable(name: _name,
                description: _description);
        }
    }
}
