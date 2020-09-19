using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Data.Tpl;

namespace Glacie.Lab.Metadata
{
    // TODO: should it return just stream of data? (as resolver)

    [Obsolete("Obsolete", true)]
    public interface ITemplateResolver
    {
        Template ResolveTemplate(string name);
    }
}
