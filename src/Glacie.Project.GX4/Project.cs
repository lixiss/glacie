using System;
using System.Collections.Generic;
using System.Linq;

using Glacie.Infrastructure;
using Glacie.Logging;
using Glacie.Modules;

namespace Glacie.ProjectSystem
{
    public sealed class Project : IDisposable
    {
        private readonly ProjectContext _context;

        // Modules are listed as they declared, e.g. root module first. Context work with modules in reverse order.
        // Probably has sense reverse them initially.
        private readonly SourceModule[] _sourceModules;
        // TODO: who will own target module?
        // _targetModule

        internal Project(ProjectContext context, IReadOnlyCollection<SourceModule> sourceModules)
        {
            _context = context;
            _sourceModules = sourceModules.ToArray();
        }

        public void Dispose()
        {
            // TODO: implement disposing
        }

        public IReadOnlyCollection<Module> SourceModules => _sourceModules;

        /// <summary>
        /// Creates new <see cref="Context"/> object.
        /// </summary>
        public Context CreateContext()
        {
            //// TODO: Let context open modules instead, as it want.
            //foreach (var sourceModule in _sourceModules)
            //{
            //    sourceModule.Open();
            //}

            var targetModule = new TargetModule();
            var sourceModules = _sourceModules.Reverse().ToArray();

            // TODO: instead use DI to get resolver over context's modules.
            // actual factory, may use cached providers/resolvers
            var resourceManager = new ResourceProviderFactory().CreateResourceManager(
                new Module[] { targetModule }.Concat(sourceModules),
                _context.Services.Resolve<Logger>());

            var context = new Context(
                _context,
                targetModule,
                sourceModules,
                resourceManager
                );
            return context;
        }
    }
}
