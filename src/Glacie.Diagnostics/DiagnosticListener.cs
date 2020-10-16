using System;

namespace Glacie.Diagnostics
{
    // TODO: Disallow use this class, e.g. it needs utility, but use only interface.
    public abstract class DiagnosticListener : IDiagnosticListener
    {
        #region Factory

        public static DiagnosticListener Null => NullDiagnosticReporter.Instance;

        public static DiagnosticListener Create(Action<Diagnostic> action)
        {
            return new ActionDiagnosticReporter(action);
        }

        #endregion

        protected DiagnosticListener() { }


        public abstract void Write(Diagnostic diagnostic);


        private sealed class NullDiagnosticReporter : DiagnosticListener
        {
            private readonly static NullDiagnosticReporter s_instance = new NullDiagnosticReporter();

            public static DiagnosticListener Instance => s_instance;

            public override void Write(Diagnostic diagnostic) { }
        }

        private sealed class ActionDiagnosticReporter : DiagnosticListener
        {
            private readonly Action<Diagnostic> _action;

            public ActionDiagnosticReporter(Action<Diagnostic> action)
            {
                _action = action;
            }

            public override void Write(Diagnostic diagnostic)
            {
                _action(diagnostic);
            }
        }
    }
}
