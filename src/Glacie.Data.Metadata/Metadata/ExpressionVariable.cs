namespace Glacie.Metadata
{
    /// <inheritdoc cref="IExpressionVariableContract"/>
    public sealed class ExpressionVariable : IExpressionVariableContract
    {
        private readonly string _name;
        private readonly string? _description;
        private RecordType? _declaringRecordType;

        #region Construction

        internal ExpressionVariable(string name, string? description)
        {
            _name = name;
            _description = description;
        }

        internal void AttachTo(RecordType declaringRecordDefinition)
        {
            Check.That(_declaringRecordType == null);
            _declaringRecordType = declaringRecordDefinition;
        }

        #endregion

        public RecordType DeclaringRecordType => _declaringRecordType!;

        public string Name => _name;

        public string? Documentation => _description;
    }
}
