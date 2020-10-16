using Glacie.Diagnostics;

namespace Glacie.Data.Templates
{
    // TODO: (Low) Use simplified diagnostic definitions factory.
    internal static class DiagnosticDefinitions
    {
        private static DiagnosticDefinition? s_unexpectedEndOfStream;
        private static DiagnosticDefinition? s_unknownToken;
        private static DiagnosticDefinition? s_unexpectedToken;
        private static DiagnosticDefinition? s_propertyRequired;

        public static DiagnosticDefinition UnexpectedEndOfStream
            => Create(ref s_unexpectedEndOfStream,
            id: "GXT0001",
            message: "Unexpected end of stream."
            );

        public static DiagnosticDefinition UnknownToken
            => Create(ref s_unknownToken,
            id: "GXT0002",
            message: "Unknown token \"{0}\"."
            );

        public static DiagnosticDefinition UnexpectedToken
            => Create(ref s_unexpectedToken,
            id: "GXT0003",
            message: "Unexpected token \"{0}\"."
            );

        public static DiagnosticDefinition PropertyRequired
            => Create(ref s_propertyRequired,
            id: "GXT0004",
            message: "Property \"{0}\" required."
            );

        private static DiagnosticDefinition Create(ref DiagnosticDefinition? cachedValue,
            string id, string message, DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error)
        {
            if (cachedValue != null) return cachedValue;

            var def = new DiagnosticDefinition(
                id: id,
                messageFormat: message,
                defaultSeverity: defaultSeverity);

            return cachedValue = def;
        }
    }
}
