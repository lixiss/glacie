using System;

namespace Glacie
{
    internal static class GxError
    {
        public static GlacieException ContextTargetIsNotSpecified()
        {
            return new GlacieException("ContextTargetIsNotSpecified",
                "Context should have specified target.");
        }

        public static GlacieException ContextMultipleTargets()
        {
            return new GlacieException("ContextMultipleTargets",
                "Context may have only single target.");
        }

        public static GlacieException ContextConfigurationFailed(Exception exception)
        {
            return new GlacieException("ContextConfigurationFailed",
                "Failed to configure context.", exception);
        }

        public static GlacieException TargetConfigurationInvalid(string message)
        {
            return new GlacieException("TargetConfigurationInvalid", message);
        }

        public static GlacieException TargetConfigurationFailed(Exception exception)
        {
            return new GlacieException("TargetConfigurationFailed",
                "Failed to configure target.", exception);
        }

        public static GlacieException SourceConfigurationInvalid(string message)
        {
            return new GlacieException("SourceConfigurationInvalid", message);
        }

        public static GlacieException SourceConfigurationFailed(Exception exception)
        {
            return new GlacieException("SourceConfigurationFailed",
                "Failed to configure source.", exception);
        }

        public static GlacieException RecordNotFound(string recordName)
        {
            return new GlacieException("RecordNotFound", "Record \"{0}\" not found.".FormatWith(recordName));
        }

        public static GlacieException RecordAlreadyExist(string recordName)
        {
            return new GlacieException("RecordAlreadyExist", "Record \"{0}\" already exist.".FormatWith(recordName));
        }

        public static GlacieException ForeignRecord()
        {
            return new GlacieException("ForeignRecord", "Record owned by another database.");
        }

        public static GlacieException FieldNotFound(string fieldName)
        {
            return new GlacieException("FieldNotFound", "Field \"{0}\" not found.".FormatWith(fieldName));
        }

        public static GlacieException FieldAlreadyExist(string fieldName)
        {
            return new GlacieException("FieldAlreadyExist", "Field \"{0}\" not found.".FormatWith(fieldName));
        }
    }
}
