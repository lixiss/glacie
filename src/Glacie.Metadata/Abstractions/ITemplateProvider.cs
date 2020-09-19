using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Tpl;

// TODO: Glacie.Abstractions

namespace Glacie.Metadata
{
    public interface ITemplateProvider
    {
        bool TryGetTemplate(in VirtualPath templateName,
            [NotNullWhen(returnValue: true)] out Template? result);

        Template? GetTemplateOrDefault(in VirtualPath templateName);

        Template GetTemplate(in VirtualPath templateName);
    }
}
