using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie.Data.Tpl
{
    public sealed class Template
    {
        public string Name { get; set; } = default!;

        public TemplateGroup Root { get; set; } = default!;

        public List<string>? FileNameHistory { get; set; }
    }
}
