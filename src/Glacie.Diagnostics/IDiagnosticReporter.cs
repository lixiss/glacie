namespace Glacie.Diagnostics
{
    public interface IDiagnosticReporter
    {
        void Report(Diagnostic diagnostic);
    }
}
