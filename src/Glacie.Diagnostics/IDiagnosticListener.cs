namespace Glacie.Diagnostics
{
    /// <summary>
    /// Used to notify client what new diagnostic event is added,
    /// useful for immediate logging on screen.
    /// </summary>
    public interface IDiagnosticListener
    {
        void Write(Diagnostic diagnostic);
    }
}
