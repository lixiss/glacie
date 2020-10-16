using System;

using Glacie.DependencyInjection;

namespace Glacie.Hosting
{
    public interface IHostBuilder
    {
        IHost Build();

        IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureServices);
    }
}
