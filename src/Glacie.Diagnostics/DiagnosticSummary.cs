using System.Collections.Generic;
using System.Linq;

namespace Glacie.Diagnostics
{
    public sealed class DiagnosticSummary
    {
        private readonly DiagnosticBag _bag;

        private bool _calculated;
        private int[]? _countBySeverity;

        public DiagnosticSummary(DiagnosticBag bag)
        {
            _bag = bag;
        }

        public int Count => _bag.Count;

        public int GetCountBySeverity(DiagnosticSeverity severity)
        {
            if (!_calculated) Calculate();
            if (_countBySeverity == null) return 0;
            return _countBySeverity[(int)severity];
        }

        private void Calculate()
        {
            var countBySeverity = new int[4]; // TODO: use some internal const

            foreach (var item in _bag)
            {
                countBySeverity[(int)item.Severity]++;
            }

            _countBySeverity = countBySeverity;
            _calculated = true;
        }
    }
}
