using Glacie.Discovery_Modules;

namespace Glacie.Services.Discovery
{
    public interface IModuleDiscoverer
    {
        /// <summary>
        /// Discover given path as game module, and return information about it.
        /// Information includes database path, resource bundle(s) paths,
        /// their types and priorities.
        /// Later module info can be used to construct module.
        /// </summary>
        ModuleInfo DiscoverModule(string path);
    }
}
