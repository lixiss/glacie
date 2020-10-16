using Glacie.Data.Resources;
using Glacie.Diagnostics;

namespace Glacie.Resources
{
    internal static class DiagnosticFactory
    {
        // Resource bundles with same priority may not override resources from bundles with same priority.
        private static readonly DiagnosticDefinition s_attemptToOverrideResourceWithSamePriorityWasBlocked = Create(
            id: "GXR0001",
            message: "Attempt to override resource with same priority was blocked." +
            " Resource path: \"{0}\", currently defined by bundle: \"{1}\", override by bundle: \"{2}\"."
            );

        public static Diagnostic AttemptToOverrideResourceWithSamePriorityWasBlocked(Path path, string currentBundleName, string overrideByBundleName)
        {
            return s_attemptToOverrideResourceWithSamePriorityWasBlocked.Create(Location.None,
                path.ToString(), currentBundleName, overrideByBundleName);
        }

        #region Helpers

        private static DiagnosticDefinition Create(string id,
            string message,
            DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error)
        {
            var def = new DiagnosticDefinition(
                id: id,
                messageFormat: message,
                defaultSeverity: defaultSeverity);

            return def;
        }

        #endregion
    }
}
