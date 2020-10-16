using Glacie.Modules.Builder;

namespace Glacie.Services
{
    public interface IModuleDiscoverer
    {
        /// <summary>
        /// Discover given path as game (source) module, and return information
        /// about it in form of <see cref="ModuleBuilder"/>.
        /// Information includes database path, resource bundle(s) paths,
        /// their types and priorities within module.
        /// </summary>
        ModuleBuilder DiscoverModule(string path);
    }
}
