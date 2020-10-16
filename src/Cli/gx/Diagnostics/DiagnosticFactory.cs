using Glacie.Diagnostics;

namespace Glacie.Cli
{
    internal static class DiagnosticFactory
    {
        private static readonly DiagnosticDefinition s_projectFileDoesntExist = Create(
            id: "GXX0001",
            message: "Project file does not exist."
            );

        public static Diagnostic ProjectFileDoesntExist()
        {
            return s_projectFileDoesntExist.Create();
        }

        private static readonly DiagnosticDefinition s_directoryContainsMoreThanOneProjectFile = Create(
            id: "GXX0002",
            message: "Specify which project file to use because this folder contains more than one project file.");

        public static Diagnostic DirectoryContainsMoreThanOneProjectFile()
        {
            return s_directoryContainsMoreThanOneProjectFile.Create();
        }

        private static readonly DiagnosticDefinition s_currentDirectoryDoesNotContainProjectFile = Create(
            id: "GXX0003",
            message: "Specify a project file. The current working directory does not contain a project file.");

        public static Diagnostic CurrentDirectoryDoesNotContainProjectFile()
        {
            return s_currentDirectoryDoesNotContainProjectFile.Create();
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
