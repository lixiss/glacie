using Glacie.Diagnostics;

namespace Glacie.Validation
{
    public sealed class ValidationResult
    {
        private DiagnosticSummary? _summary;

        internal ValidationResult(DiagnosticBag bag)
        {
            Bag = bag;
        }

        public DiagnosticBag Bag { get; }

        public DiagnosticSummary GetSummary()
        {
            if (_summary != null) return _summary;
            return (_summary = new DiagnosticSummary(Bag));
        }
    }
}
