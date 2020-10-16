using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Data.Arz;
using Glacie.Diagnostics;

namespace Glacie.Validation
{
    internal static class DiagnosticFactory
    {
        #region Path Forms

        private static readonly DiagnosticDefinition s_invalidPathRecordNameConfromance = Create(
            id: "GXP0001",
            message: "({0}) Record name is invalid path.",
            defaultSeverity: DiagnosticSeverity.Warning
            );

        private static readonly DiagnosticDefinition s_invalidPathRecordNameError = Create(
            id: "GXP0002",
            message: "({0}) Record name is invalid path.",
            defaultSeverity: DiagnosticSeverity.Error
            );

        private static readonly DiagnosticDefinition s_invalidPathReferenceConfromance = Create(
            id: "GXP0001",
            message: "({0}) Field value is invalid path: \"{1}\".",
            defaultSeverity: DiagnosticSeverity.Warning
            );

        private static readonly DiagnosticDefinition s_invalidPathReferenceError = Create(
            id: "GXP0002",
            message: "({0}) Record name is invalid path: \"{1}\".",
            defaultSeverity: DiagnosticSeverity.Error
            );

        public static Diagnostic InvalidRecordPathAsConformance(RecordLocation location, string tokenName)
        {
            return s_invalidPathRecordNameConfromance.Create(location, tokenName);
        }

        public static Diagnostic InvalidRecordPathAsError(RecordLocation location, string tokenName)
        {
            return s_invalidPathRecordNameError.Create(location, tokenName);
        }

        public static Diagnostic InvalidPathAsConformance(RecordFieldLocation location, string tokenName, Path path)
        {
            return s_invalidPathReferenceConfromance.Create(location, tokenName, path.ToString());
        }

        public static Diagnostic InvalidPathAsError(RecordFieldLocation location, string tokenName, Path path)
        {
            return s_invalidPathReferenceError.Create(location, tokenName, path.ToString());
        }

        #endregion


        private static readonly DiagnosticDefinition s_recordTypeByTemplateNameResolvedByRule = Create(
            id: "GXC0001",
            message: "({0}) Record type resolved by templateName: \"{1}\" => \"{2}\".",
            defaultSeverity: DiagnosticSeverity.Warning
            );

        public static Diagnostic RecordTypeByTemplateNameResolvedByRule(RecordFieldLocation location, string tokenName, string templateName, string resolvedTemplateName)
        {
            return s_recordTypeByTemplateNameResolvedByRule.Create(location, tokenName, templateName, resolvedTemplateName);
        }

        private static readonly DiagnosticDefinition s_recordResolvedByRule = Create(
            id: "GXC0002",
            message: "({0}) Record resolved: \"{1}\" => \"{2}\".",
            defaultSeverity: DiagnosticSeverity.Warning
            );

        public static Diagnostic RecordResolvedByRule(RecordFieldLocation location, string tokenName, string name, string resolvedName)
        {
            return s_recordResolvedByRule.Create(location, tokenName, name, resolvedName);
        }

        private static readonly DiagnosticDefinition s_resourceResolvedByRule = Create(
            id: "GXC0003",
            message: "({0}) Resource resolved: \"{1}\" => \"{2}\".",
            defaultSeverity: DiagnosticSeverity.Warning
            );

        public static Diagnostic ResourceResolvedByRule(RecordFieldLocation location, string tokenName, string path, string resolvedPath)
        {
            return s_resourceResolvedByRule.Create(location, tokenName, path, resolvedPath);
        }


        private static readonly DiagnosticDefinition s_recordMustHaveTemplateName = Create(
            id: "GXV0001",
            message: "Record doesn't have field \"templateName\"."
            );

        public static Diagnostic RecordFieldTemplateNameRequired(RecordLocation location)
        {
            return s_recordMustHaveTemplateName.Create(location);
        }

        private static readonly DiagnosticDefinition s_recordTemplateNameMustBeString = Create(
            id: "GXV0002",
            message: "Record templateName field must be string."
            );

        public static Diagnostic RecordTemplateNameMustBeString(RecordFieldLocation location)
        {
            return s_recordTemplateNameMustBeString.Create(location);
        }

        private static readonly DiagnosticDefinition s_recordTemplateNameMustBeSingleValue = Create(
            id: "GXV0003",
            message: "Record templateName should be single value, but it is array. Using first value \"{0}\" as templateName."
            );

        public static Diagnostic RecordTemplateNameMustBeSingleValue(RecordFieldLocation location, string templateName)
        {
            return s_recordTemplateNameMustBeSingleValue.Create(location, templateName);
        }



        private static readonly DiagnosticDefinition s_resourceNotFound = Create(
            id: "GXV1000",
            message: "The resource name \"{0}\" ({1}) could not be found."
            );

        private static readonly DiagnosticDefinition s_templateResourceNotFound = Create(
            id: "GXV1001",
            message: "The template name \"{0}\" could not be found."
            );

        private static readonly DiagnosticDefinition s_recordResourceNotFound = Create(
            id: "GXV1002",
            message: "The record name \"{0}\" ({1}) could not be found."
            );

        // TODO: report errors per-resource type (to have different diagnostic ids)
        // TODO: (instead of resourceType should be Variable Type / VarClass)
        public static Diagnostic ResourceNotFound(RecordFieldLocation location, string resourceName, string resourceType)
        {
            return s_resourceNotFound.Create(location, resourceName, resourceType);
        }

        public static Diagnostic TemplateResourceNotFound(RecordFieldLocation location, string resourceName)
        {
            return s_templateResourceNotFound.Create(location, resourceName);
        }

        public static Diagnostic RecordResourceNotFound(RecordFieldLocation location, string resourceName, string resourceType)
        {
            return s_recordResourceNotFound.Create(location, resourceName, resourceType);
        }


        private static readonly DiagnosticDefinition s_fieldTypeNotFound = Create(
            id: "GXV2001",
            message: "Field is not defined by record type: \"{0}\""
            );

        public static Diagnostic FieldTypeNotFound(RecordFieldLocation location, string recordType)
        {
            return s_fieldTypeNotFound.Create(location, recordType);
        }

        private static readonly DiagnosticDefinition s_invalidFieldValueType = Create(
            id: "GXV2002",
            message: "Invalid field value type: Must be \"{1}\", but actual is \"{0}\"."
            );

        public static Diagnostic InvalidFieldValueType(RecordFieldLocation location,
            ArzValueType actualType,
            ArzValueType expectedType)
        {
            return s_invalidFieldValueType.Create(location, actualType, expectedType);
        }

        private static readonly DiagnosticDefinition s_fieldMustBeSingleValue = Create(
            id: "GXV2003",
            message: "Field must be single value, but actual number of elements in field: {0}."
            );

        public static Diagnostic FieldMustBeSingleValue(RecordFieldLocation location, int actualNumberOfElements)
        {
            return s_fieldMustBeSingleValue.Create(location, actualNumberOfElements);
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
