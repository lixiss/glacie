using Glacie.Discovery_Engines;

namespace Glacie.Services.Discovery
{
    public interface IEngineDiscoverer
    {
        /// <summary>
        /// Discover given path as engine, and return information about it.
        /// </summary>
        EngineInfo DiscoverEngine(string path);
    }
}
