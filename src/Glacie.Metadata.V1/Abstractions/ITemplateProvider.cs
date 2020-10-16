using System.Diagnostics.CodeAnalysis;

using Glacie.Data.Templates;

// TODO: Glacie.Abstractions

namespace Glacie.Metadata.V1
{
    // TODO: Need ITemplateResolver who will remap template names

    public interface ITemplateProvider
    {
        bool TryGetTemplate(in Path1 templateName,
            [NotNullWhen(returnValue: true)] out Template? result);

        Template? GetTemplateOrDefault(in Path1 templateName);

        Template GetTemplate(in Path1 templateName);
    }
}
