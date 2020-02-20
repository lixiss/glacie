namespace Glacie
{
    /// <summary>
    /// Represent database record (.DBR) file.
    /// </summary>
    public class DbRecord
    {
        private readonly Context _context;
        private readonly string _path;
        private readonly string _content;

        internal DbRecord(Context context, string path, string content)
        {
            _context = context;
            _path = path;
            _content = content;
        }

        public Context Context => _context;

        public string RelativePath => _path;

        public string RawContent => _content;

        public void CheckResourceReference(string propertyName)
        {
            if (TryGetPropertyValue(_content, propertyName, out var value))
            {
                if (!Context.IsResourceExist(value))
                {
                    Context.Report(string.Format("ER0001:{0}: Property \"{1}\" has invalid resource reference: \"{2}\".",
                        RelativePath, propertyName, value));
                }
            }
        }

        private static bool TryGetPropertyValue(string content, string key, out string value)
        {
            var startIndex = content.IndexOf(key);
            if (startIndex < 0)
            {
                // Content has no this property.
                value = null;
                return false;
            }

            // Ensure what we stop at line start.
            if (!(startIndex == 0 || content[startIndex - 1] == '\r' || content[startIndex - 1] == '\n'))
            {
                value = null;
                return false;
            }

            // Ensure what it is full property name
            if (!(content.Length >= (startIndex + key.Length)
                && content[startIndex + key.Length] == ','))
            {
                value = null;
                return false;
            }

            var valueStartIndex = startIndex + key.Length + 1;
            var valueEndIndex = valueStartIndex;
            for (; valueEndIndex < content.Length; valueEndIndex++)
            {
                if (content[valueEndIndex] == ','
                    || content[valueEndIndex] == '\r'
                    || content[valueEndIndex] == '\n') break;
            }

            value = content.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
            return true;
        }
    }
}
