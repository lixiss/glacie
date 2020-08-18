namespace Glacie.Data.Arz
{
    // TODO: (Low) ArzError should be in Glacie.Data.Arz assembly.

    /// <summary>
    /// ArzException factory.
    /// </summary>
    internal static class ArzError
    {
        public static ArzException RecordNotFound(string recordName)
        {
            return new ArzException("RecordNotFound", "Record \"{0}\" not found.".FormatWith(recordName));
        }

        public static ArzException RecordAlreadyExist(string recordName)
        {
            return new ArzException("RecordAlreadyExist", "Record \"{0}\" already exist.".FormatWith(recordName));
        }

        public static ArzException ExternalRecord()
        {
            return new ArzException("ExternalRecord", "External record: owned by another database.");
        }

        public static ArzException InternalRecord()
        {
            return new ArzException("InternalRecord", "Internal record: already owned by this database.");
        }

        public static ArzException FieldNotFound(string fieldName)
        {
            return new ArzException("FieldNotFound", "Field \"{0}\" not found.".FormatWith(fieldName));
        }

        public static ArzException FieldAlreadyExist(string fieldName)
        {
            return new ArzException("FieldAlreadyExist", "Field \"{0}\" not found.".FormatWith(fieldName));
        }

        public static ArzException FieldTypeMismatch()
        {
            return new ArzException("FieldTypeMismatch", "Field type mismatch.");
        }

        public static ArzException FieldTypeMismatch(string message)
        {
            return new ArzException("FieldTypeMismatch", message);
        }

        public static ArzException FieldNotSingleValue()
        {
            return new ArzException("FieldNotSingleValue", "Field is array, but accessed as single value. Use Get<T>(int index) method instead.");
        }

        public static ArzException UnknownLayout()
        {
            return new ArzException("UnknownLayout", "Can't determine file layout.");
        }

        public static ArzException LayoutRequired(string? message = null)
        {
            return new ArzException("LayoutRequired", message ?? "Layout is required.");
        }

        public static ArzException InvalidLayout(string? message = null)
        {
            return new ArzException("InvalidLayout", message ?? "Specified layout is invalid.");
        }

        public static ArzException RecordHasNoClass(string recordName)
        {
            return new ArzException("RecordHasNoClass", "Record \"{0}\" has no specified class.".FormatWith(recordName));
        }

        public static ArzException RecordHasNoAnyField(string recordName)
        {
            return new ArzException("RecordHasNoAnyField", "Record \"{0}\" has no any field.".FormatWith(recordName));
        }

        public static ArzException InvalidChecksum(string message)
        {
            return new ArzException("InvalidChecksum", message);
        }

        public static ArzException NotFiniteNumber()
        {
            return new ArzException("NotFiniteNumber", "Number encountered was not a finite quantity.");
        }

        public static ArzException ArithmeticOverflow()
        {
            return new ArzException("ArithmeticOverflow", "Arithmetic operation resulted in an overflow.");
        }
    }
}
