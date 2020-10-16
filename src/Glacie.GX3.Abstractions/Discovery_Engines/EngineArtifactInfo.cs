namespace Glacie.Discovery_Engines
{
    public sealed class EngineArtifactInfo
    {
        public string PhysicalPath { get; }

        public string RelativePath { get; }

        internal EngineArtifactInfo(string physicalPath, string relativePath)
        {
            PhysicalPath = physicalPath;
            RelativePath = relativePath;
        }
    }
}
