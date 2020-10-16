using Glacie.Data.Arz;
using Glacie.Diagnostics;

namespace Glacie.Metadata
{
    internal static class DiagnosticFactory
    {
        private static readonly DiagnosticDefinition s_invalidTemplateGroupType = Create(
            id: "GXM0001",
            message: "Invalid template group type \"{0}\"."
            );

        public static Diagnostic InvalidTemplateGroupType(Location location, string groupType)
        {
            return s_invalidTemplateGroupType.Create(location,
                groupType);
        }

        private static readonly DiagnosticDefinition s_invalidTemplateEqnVariableName = Create(
            id: "GXM0002",
            message: "Template variable of type \"eqnVariable\" has invalid property \"Name\" value: \"{0}\". " +
            "Expected value \"{1}\".");

        public static Diagnostic InvalidTemplateEqnVariableName(Location location,
            string actualValue, string expectedValue)
        {
            return s_invalidTemplateEqnVariableName.Create(location,
                actualValue, expectedValue);
        }

        private static readonly DiagnosticDefinition s_invalidTemplateEqnVariableClass = Create(
            id: "GXM0003",
            message: "Template variable of type \"eqnVariable\" has invalid property \"Class\" value: \"{0}\". " +
            "Expected value \"{1}\".");

        public static Diagnostic InvalidTemplateEqnVariableClass(Location location,
            string actualValue, string expectedValue)
        {
            return s_invalidTemplateEqnVariableClass.Create(location,
                actualValue, expectedValue);
        }

        private static readonly DiagnosticDefinition s_invalidTemplateEqnVariableValue = Create(
            id: "GXM0004",
            message: "Template variable of type \"eqnVariable\" has invalid property \"Value\" value: \"{0}\". " +
            "This property must be empty.");

        public static Diagnostic InvalidTemplateEqnVariableValue(Location location,
            string actualValue)
        {
            return s_invalidTemplateEqnVariableValue.Create(location,
                actualValue);
        }

        private static readonly DiagnosticDefinition s_invalidTemplateEqnVariableDefaultValue = Create(
            id: "GXM0005",
            message: "Template variable of type \"eqnVariable\" must specify \"DefaultValue\" property and it can not be empty.");

        public static Diagnostic InvalidTemplateEqnVariableDefaultValue(Location location)
        {
            return s_invalidTemplateEqnVariableValue.Create(location);
        }

        private static readonly DiagnosticDefinition s_invalidTemplateVariableName = Create(
            id: "GXM0006",
            message: "Template variable must specify name.");

        public static Diagnostic TemplateVariableNameCanNotBeEmpty(Location location)
        {
            return s_invalidTemplateVariableName.Create(location);
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
