using Glacie.Discovery_Engines;
using Glacie.Discovery_Engines.Builder;

using IO = System.IO;

namespace Glacie.Services.Discovery
{
    public sealed class EngineDiscoverer : IEngineDiscoverer
    {
        public EngineInfo DiscoverEngine(string path)
        {
            Check.Argument.NotNullNorEmpty(path, nameof(path));

            var physicalPath = IO.Path.GetFullPath(path);

            var result = new EngineInfoBuilder();
            result.SetPhysicalPath(physicalPath);

            if (IO.Directory.Exists(physicalPath))
            {
                FindFile(result, physicalPath, "Engine.dll");
                FindFile(result, physicalPath, "Game.dll");
                FindFile(result, physicalPath, "Titan Quest.exe", EngineClass.TQ);
                FindFile(result, physicalPath, "Tqit.exe", EngineClass.TQIT);
                FindFile(result, physicalPath, "TQ.exe", EngineClass.TQAE);
                FindFile(result, physicalPath, "Grim Dawn.exe", EngineClass.GD);
            }

            return result.Build();
        }

        private static void FindFile(EngineInfoBuilder result, string physicalPath, string filename, EngineClass? engineClass = null)
        {
            var path = IO.Path.Combine(physicalPath, filename);

            if (IO.File.Exists(path))
            {
                result.AddArtifact(path);

                if (engineClass != null)
                {
                    result.AddEngineClass(engineClass.Value);
                }
            }
        }
    }
}
