using System;
using System.Xml.Linq;

using Glacie.Diagnostics;

namespace Glacie.ProjectSystem
{
    internal static class DiagnosticFactory
    {
        private static readonly DiagnosticDefinition s_unexpectedXmlElement = Create(
            id: "GXP0001",
            message: "Unexpected element \"{0}\"."
            );

        public static Diagnostic UnexpectedElement(Location location, XElement element)
        {
            return s_unexpectedXmlElement.Create(location, element.Name.LocalName);
        }

        private static readonly DiagnosticDefinition s_unexpectedXmlAttribute = Create(
            id: "GXP0002",
            message: "Unexpected attribute \"{0}\"."
            );

        public static Diagnostic UnexpectedAttribute(Location location, XAttribute attribute)
        {
            return s_unexpectedXmlAttribute.Create(location, attribute.Name.LocalName);
        }

        private static readonly DiagnosticDefinition s_elementMustBeUsedOnlyOnce = Create(
            id: "GXP0003",
            message: "Element \"{0}\" must be used only once."
            );

        public static Diagnostic ElementMustBeUsedOnlyOnce(Location location, XElement element)
        {
            return s_elementMustBeUsedOnlyOnce.Create(location, element.Name.LocalName);
        }

        // TODO: ... remove not needed
        /*
        private static readonly DiagnosticDefinition s_xmlMetadataReaderGenericError = Create(
            id: "GXM0001",
            message: "Metadata reading error: {0}"
            );

        public static Diagnostic ReaderError(Location location, string message)
        {
            return s_xmlMetadataReaderGenericError.Create(location, message);
        }

        private static readonly DiagnosticDefinition s_attributeCanNotBeEmpty = Create(
            id: "GXM0004",
            message: "Attribute \"{0}\" must not be empty."
            );

        public static Diagnostic AttributeCannotBeEmpty(Location location, XAttribute attribute)
        {
            return s_attributeCanNotBeEmpty.Create(location, attribute.Name.LocalName);
        }

        private static readonly DiagnosticDefinition s_missedRequiredAttribute = Create(
            id: "GXM0005",
            message: "Element \"{0}\" must have attribute \"{1}\"."
            );

        public static Diagnostic ElementMustHaveAttribute(Location location, XElement element, string attributeName)
        {
            return s_missedRequiredAttribute.Create(location, element.Name.LocalName, attributeName);
        }

        private static readonly DiagnosticDefinition s_invalidAttributeValue = Create(
            id: "GXM0006",
            message: "Invalid attribute value. {0}"
            );

        public static Diagnostic InvalidAttributeValue(Location location, string? message)
        {
            return s_invalidAttributeValue.Create(location, message);
        }

        private static readonly DiagnosticDefinition s_youMustSpecifyElementOrAttributeButNotBoth = Create(
            id: "GXM0008",
            message: "Element \"{0}\" conflicts with previously used attribute. You must use attribute or element, but not both."
            );

        public static Diagnostic YouMustSpecifyElementOrAttributeButNotBoth(Location location, XElement element)
        {
            return s_youMustSpecifyElementOrAttributeButNotBoth.Create(location, element.Name.LocalName);
        }

        private static readonly DiagnosticDefinition s_patchingConflict = Create(
            id: "GXM0009",
            message: "Metadata patching conflict: attempt to change property \"{0}\" from value \"{1}\" to \"{2}\" without specifying patch mode."
            );

        public static Diagnostic PatchingConflict(Location location, string propertyName, string? currentValue, string? newValue)
        {
            return s_patchingConflict.Create(location, propertyName, currentValue, newValue);
        }

        */

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
