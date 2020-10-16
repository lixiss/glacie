using System;
using System.Collections.Generic;
using System.Linq;

namespace Glacie.Data.Templates
{
    public sealed class TemplateGroup : TemplateNode
    {
        private List<TemplateNode>? _children;

        public TemplateGroup() { }

        public string Type { get; set; } = default!;

        public IEnumerable<TemplateNode> Children => _children ?? Enumerable.Empty<TemplateNode>();

        public IEnumerable<TemplateGroup> Groups => Children.OfType<TemplateGroup>();

        public IEnumerable<TemplateVariable> Variables => Children.OfType<TemplateVariable>();

        public IEnumerable<TemplateNode> DescendantsAndSelf()
            => DescendantsInternal(true);

        public IEnumerable<TemplateNode> Descendants()
            => DescendantsInternal(false);

        public TemplateGroup? Group(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            foreach (var group in Groups)
            {
                if (name.Equals(group.Name, comparison)) return group;
            }
            return null;
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

        private IEnumerable<TemplateNode> DescendantsInternal(bool andSelf)
        {
            if (andSelf) yield return this;

            if (_children != null)
            {
                for (var i = 0; i < _children.Count; i++)
                {
                    var children = _children[i];
                    yield return children;

                    if (children is TemplateGroup g)
                    {
                        if (g._children != null && g._children.Count > 0)
                        {
                            foreach (var x in g.DescendantsInternal(false))
                            {
                                yield return x;
                            }
                        }
                    }
                }
            }
        }
    }
}
