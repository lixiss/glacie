using Glacie.Data;
using Glacie.Resources;

namespace Glacie.Discovery
{
    internal static class EngineTypeDiscoverer
    {
        public static bool TryGetEngineTypeId(IResourceResolver resourceResolver, out EngineClass engineTypeId)
        {
            if (resourceResolver.TryResolveResource(Path.Implicit("database/templates/devotionskilltree.tpl"), out var _))
            {
                engineTypeId = EngineClass.GD;
                return true;
            }
            else if (resourceResolver.TryResolveResource(Path.Implicit("database/templates/ingameui/charstatstab1 ae.tpl"), out var _))
            {
                engineTypeId = EngineClass.TQAE;
                return true;
            }
            else if(resourceResolver.TryResolveResource(Path.Implicit("database/templates/itemartifact.tpl"), out var _))
            {
                engineTypeId = EngineClass.TQIT;
                return true;
            }
            else
            {
                engineTypeId = EngineClass.Unknown;
                return false;
            }
        }
    }
}
