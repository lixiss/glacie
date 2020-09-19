using System.Collections.Generic;
using System.Linq;

namespace Glacie.Data.Tpl
{
    public sealed class TemplateGroup : TemplateNode
    {
        private List<TemplateNode>? _children;

        public TemplateGroup() { }

        public string Type { get; set; }

        public IEnumerable<TemplateNode> Children => _children ?? Enumerable.Empty<TemplateNode>();

        public IEnumerable<TemplateGroup> Groups => Children.OfType<TemplateGroup>();

        public IEnumerable<TemplateVariable> Variables => Children.OfType<TemplateVariable>();

        public TemplateGroup? Group(string name)
        {
            return Groups.Where(x => x.Name == name).FirstOrDefault();
        }

        public TemplateVariable? Variable(string name)
        {
            return Variables.Where(x => x.Name == name).FirstOrDefault();
        }

        public void Add(TemplateNode value)
        {
            if (_children == null) _children = new List<TemplateNode>();

            _children.Add(value);
        }
    }
}
