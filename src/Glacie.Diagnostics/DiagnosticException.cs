using System;

namespace Glacie.Diagnostics
{
    public sealed class DiagnosticException : Exception
    {
        private readonly Diagnostic _diagnostic;

        public DiagnosticException(Diagnostic diagnostic)
            : this(diagnostic, innerException: null) { }

        public DiagnosticException(Diagnostic diagnostic, Exception? innerException)
            : base(null, innerException)
        {
            Check.That(diagnostic != null);

            _diagnostic = diagnostic;
        }

        public Diagnostic Diagnostic => _diagnostic;

        public override string Message => _diagnostic.ToString();
    }
}
