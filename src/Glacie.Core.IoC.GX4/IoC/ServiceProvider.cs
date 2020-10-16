using DryIoc;

using Glacie.Abstractions;

namespace Glacie.IoC
{
    public sealed class ServiceProvider : IServiceProvider
    {
        private readonly Container _container;

        internal ServiceProvider(Container container)
        {
            _container = container;
        }

        public TService Resolve<TService>()
        {
            return _container.Resolve<TService>();
        }
    }
}