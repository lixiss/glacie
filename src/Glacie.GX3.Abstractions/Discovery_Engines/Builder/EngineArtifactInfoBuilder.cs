namespace Glacie.Discovery_Engines.Builder
{
    internal readonly struct EngineArtifactInfoBuilder
    {
        private readonly string _path;

        public EngineArtifactInfoBuilder(string path)
        {
            _path = path;
        }

        public EngineArtifactInfo Build(string? physicalPath)
        {
            return new EngineArtifactInfo(
                physicalPath: PathUtilities.GetPhysicalPath(_path),
                relativePath: PathUtilities.GetRelativePath(physicalPath, _path));
        }
    }
}
