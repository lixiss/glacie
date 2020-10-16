using System;

using DryIoc;

namespace Glacie.IoC
{
    public sealed class ServiceCollection : IDisposable
    {
        private Container _container;
        private ServiceProvider _serviceProvider;

        public ServiceCollection()
        {
            _container = new Container();
            _serviceProvider = new ServiceProvider(_container);
        }

        public void Dispose()
        {
            if (!_container.IsDisposed)
            {
                _container.Dispose();
            }
        }

        public ServiceProvider AsServiceProvider()
        {
            return _serviceProvider;
        }

        public void AddSingleton<TService>(TService implementation)
        {
            _container.RegisterInstance<TService>(implementation);
        }

        public void AddSingleton<TService, TImplementation>()
            where TImplementation : TService
        {
            _container.Register<TService, TImplementation>(Reuse.Singleton);
        }

        public void AddTransient<TService, TImplementation>()
            where TImplementation : TService
        {
            _container.Register<TService, TImplementation>(Reuse.Transient);
        }

        public void Remove<TService>()
        {
            _container.Unregister<TService>();
        }
    }
}
