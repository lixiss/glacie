using Glacie.Abstractions;

using IO = System.IO;

namespace Glacie.Resources.Providers
{
    internal abstract class ResourceBase : Resource
    {
        private readonly ResourceProvider _provider;

        private readonly VirtualPath _name;
        private readonly ResourceType _type;
        private readonly bool _development;

        protected ResourceBase(ResourceProvider provider, in VirtualPath name)
        {
            Check.Argument.NotNull(provider, nameof(provider));
            Check.Argument.NotNullNorEmpty(name.Value, nameof(name));

            _provider = provider;
            _name = name;
            _type = ResourceTypeUtilities.FromName(name);
            _development = _type == ResourceType.Template;
        }

        public override VirtualPath Name => _name;

        public override ResourceType Type => _type;

        public override bool Development => _development;

        public override IO.Stream Open() => _provider.Open(this);

        public ResourceProvider Provider => _provider;
    }
}
