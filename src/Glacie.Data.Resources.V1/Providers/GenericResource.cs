using Glacie.Abstractions;

namespace Glacie.Data.Resources.V1.Providers
{
    internal abstract class GenericResource : Resource
    {
        private readonly ResourceProvider _provider;
        private readonly Path1 _virtualPath;
        private readonly string _physicalPath;
        private readonly ResourceType _type;

        protected GenericResource(ResourceProvider provider,
            in Path1 virtualPath,
            string physicalPath,
            ResourceType type)
        {
            Check.Argument.NotNull(provider, nameof(provider));
            Check.Argument.NotNullNorEmpty(virtualPath.ToString(), nameof(virtualPath));
            Check.Argument.NotNullNorEmpty(physicalPath, nameof(physicalPath));

            _provider = provider;
            _virtualPath = virtualPath;
            _physicalPath = physicalPath;
            _type = type;
        }

        public sealed override string Name => _virtualPath.ToString();

        public override ref readonly Path1 VirtualPath => ref _virtualPath;

        public override string PhysicalPath => _physicalPath;

        public override ResourceType Type => _type;

        public override bool Development => ResourceTypeUtilities.IsDevelopment(_type);

        public override ResourceProvider Provider => _provider;


        // public override abstract IO.Stream Open();
    }
}
