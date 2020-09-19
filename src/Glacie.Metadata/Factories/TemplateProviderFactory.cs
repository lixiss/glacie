using Glacie.Abstractions;

namespace Glacie.Metadata
{
    public static class TemplateProviderFactory
    {
        public static TemplateProvider Create(ResourceProvider templateResourceProvider)
        {
            return new TemplateProviderImpl(templateResourceProvider);
        }
    }
}
