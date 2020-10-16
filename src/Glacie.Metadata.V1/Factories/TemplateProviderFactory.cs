using Glacie.Data.Resources.V1;

namespace Glacie.Metadata.V1
{
    public static class TemplateProviderFactory
    {
        public static TemplateProvider Create(ResourceProvider templateResourceProvider)
        {
            return new TemplateProviderImpl(templateResourceProvider);
        }
    }
}
