using System;
using System.Collections.Generic;

namespace Glacie.Discovery_Engines
{
    public sealed class EngineInfo
    {
        private readonly string? _physicalPath;
        private readonly EngineArtifactInfo[]? _artifacts;
        private readonly EngineClass? _engineClass;
        private readonly bool _ambigious;

        internal EngineInfo(
            string? physicalPath,
            EngineArtifactInfo[]? artifacts,
            EngineClass? engineClass,
            bool ambigious)
        {
            _physicalPath = physicalPath;
            _artifacts = artifacts;
            _engineClass = engineClass;
            _ambigious = ambigious;
        }

        public string? PhysicalPath => _physicalPath;

        public IReadOnlyCollection<EngineArtifactInfo> Artifacts => _artifacts ?? Array.Empty<EngineArtifactInfo>();

        public EngineClass? EngineClass => _engineClass;

        public bool Ambigious => _ambigious;

        public bool Exists => _artifacts != null && _artifacts.Length > 0;
    }
}
