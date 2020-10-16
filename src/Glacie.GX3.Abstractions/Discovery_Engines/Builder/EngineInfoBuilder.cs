using System.Collections.Generic;

using IO = System.IO;

namespace Glacie.Discovery_Engines.Builder
{
    public sealed class EngineInfoBuilder
    {
        private bool _built;

        private string? _physicalPath;
        private EngineClass? _engineClass;
        private bool _ambigious;
        private List<EngineArtifactInfoBuilder> _artifacts;

        public EngineInfoBuilder()
        {
            _artifacts = new List<EngineArtifactInfoBuilder>();
        }

        public void SetPhysicalPath(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IO.Path.GetFullPath(value);
            }

            _physicalPath = value;
        }

        public void AddEngineClass(EngineClass value)
        {
            if (_engineClass == null)
            {
                _engineClass = value;
            }
            else if (_engineClass.Value != value)
            {
                _ambigious = true;
            }
        }

        public void AddArtifact(string artifactPath)
        {
            _artifacts.Add(new EngineArtifactInfoBuilder(artifactPath));
        }

        public EngineInfo Build()
        {
            Check.That(!_built);
            _built = true;

            var physicalPath = EmptyToNull(_physicalPath);
            var artifacts = Map(_artifacts, _physicalPath);

            return new EngineInfo(
                physicalPath: physicalPath,
                artifacts: artifacts,
                engineClass: _engineClass,
                ambigious: _ambigious
                );
        }

        private static EngineArtifactInfo[]? Map(List<EngineArtifactInfoBuilder> value, string? physicalPath)
        {
            if (value == null || value.Count == 0) return null;

            var result = new EngineArtifactInfo[value.Count];
            int i = 0;
            foreach (var eaib in value)
            {
                result[i] = eaib.Build(physicalPath);
                i++;
            }
            return result;
        }

        private static string? EmptyToNull(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value;
        }
    }
}
