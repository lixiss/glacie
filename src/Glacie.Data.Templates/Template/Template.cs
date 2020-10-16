using System.Collections.Generic;
using System.Linq;

namespace Glacie.Data.Templates
{
    public sealed class Template
    {
        private Path1 _path;

        public string Name => _path.ToString();

        public Path1 Path
        {
            get => _path;
            set => _path = value;
        }

        public TemplateGroup Root { get; set; } = default!;

        public List<string>? FileNameHistory { get; set; }

        public IEnumerable<TemplateNode> Descendants()
        {
            if (Root != null) return Root.DescendantsAndSelf();
            else return Enumerable.Empty<TemplateNode>();
        }
    }
}
