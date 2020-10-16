namespace Glacie.Discovery_Modules.Builder
{
    internal struct DatabaseInfoBuilder
    {
        private readonly string _path;

        public DatabaseInfoBuilder(string path)
        {
            _path = path;
        }

        public DatabaseInfo Build(string? physicalPath)
        {
            return new DatabaseInfo(
                physicalPath: PathUtilities.GetPhysicalPath(_path),
                relativePath: PathUtilities.GetRelativePath(physicalPath, _path));
        }
    }
}
