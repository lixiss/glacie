using Glacie.Abstractions;
using Glacie.IoC;

namespace Glacie.ProjectSystem
{
    // TODO: Project should be disposable

    /// <summary>
    /// Project may use various services: this is composition root.
    /// </summary>
    public sealed class ProjectContext : IProjectContext
    {
        private readonly ServiceCollection _serviceCollection;
        private readonly ServiceProvider _serviceProvider;

        internal ProjectContext()
        {
            _serviceCollection = new ServiceCollection();
            _serviceProvider = _serviceCollection.AsServiceProvider();
        }

        internal ServiceCollection ServiceCollection => _serviceCollection;

        public IServiceProvider Services => _serviceProvider;

    }
}
