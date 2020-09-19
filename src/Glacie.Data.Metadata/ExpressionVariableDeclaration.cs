namespace Glacie.Data.Metadata
{
    public sealed class ExpressionVariableDeclaration
    {
        private readonly string _name;
        private readonly string? _description;
        private RecordType? _declaringRecordDefinition;

        #region Construction

        internal ExpressionVariableDeclaration(string name, string? description)
        {
            _name = name;
            _description = description;
        }

        internal void AttachTo(RecordType declaringRecordDefinition)
        {
            Check.That(_declaringRecordDefinition == null);
            _declaringRecordDefinition = declaringRecordDefinition;
        }

        #endregion

        public RecordType DeclaringRecordDefinition => _declaringRecordDefinition!;

        public string Name => _name;

        public string? Description => _description;
    }
}
