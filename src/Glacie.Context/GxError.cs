using System;

namespace Glacie
{
    internal static class GxError
    {
        public static GlacieException SourceConfigurationInvalid(string message)
        {
            return new GlacieException("SourceConfigurationInvalid", message);
        }

        public static GlacieException TargetConfigurationInvalid(string message)
        {
            return new GlacieException("TargetConfigurationInvalid", message);
        }

        public static Exception ContextNoSourceOrTargetSpecified()
        {
            return new GlacieException("ContextNoSourceOrTargetSpecified",
                "Context configuration should specify source or target.");
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

        internal static Exception MetadataConfigurationInvalid(string message)
        {
            return new GlacieException("MetadataConfigurationInvalid", message);
        }
    }
}
