using System.Collections.Generic;

namespace Glacie.ProjectSystem.Serialization.Model
{
    public sealed class ProjectModel
    {
        public List<ProjectSourceModel>? Sources { get; set; }

        public ProjectMetadataModel? Metadata { get; set; }
    }
}
