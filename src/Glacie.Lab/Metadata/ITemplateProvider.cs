using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Glacie.Data.Templates;

namespace Glacie.Lab.Metadata
{
    public interface ITemplateProvider
    {
        Template GetTemplate(string name);
        bool TryGetTemplate(string name, [NotNullWhen(returnValue: true)] out Template result);
    }
}
