namespace Glacie.Discovery_Modules
{
    public sealed class DatabaseInfo
    {
        public string PhysicalPath { get; }

        public string RelativePath { get; }

        internal DatabaseInfo(string physicalPath, string relativePath)
        {
            Check.Argument.NotNullNorEmpty(physicalPath, nameof(physicalPath));
            Check.Argument.NotNullNorEmpty(relativePath, nameof(relativePath));

            PhysicalPath = physicalPath;
            RelativePath = relativePath;
        }
    }
}
