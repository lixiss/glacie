using System;
using System.Linq;
using System.Text;

using Glacie.Discovery_Engines;
using Glacie.Localization;
using Glacie.Discovery_Modules;
using Glacie.Services.Discovery;
using Glacie.Modules;
using Glacie.Modules.Builder;

namespace Glacie
{
    internal class Usage1
    {
        public static void Run()
        {
            /*
            var moduleBuilder = new ModuleBuilder();
            moduleBuilder.SetPhysicalPath(@"G:\Games\TQAE");
            moduleBuilder.AddDatabase(@"database\database.arz");
            moduleBuilder.AddDatabase(IArzDatabase database);

            var module = moduleBuilder.Build();
            */


            // Context should take TargetModule and SourceModule(s), and may be some rules.
            //var targetModule = new TargetModule();
            // var sourceModule = new SourceModule();
            // new Context(targetModule, new[] { sourceModule });

            var engineDiscoverer = new EngineDiscoverer();
            var ei0 = engineDiscoverer.DiscoverEngine(@"G:\Games\TQAE");
            DumpEngineInfo(ei0);

            var ei1 = engineDiscoverer.DiscoverEngine(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");
            DumpEngineInfo(ei1);

            var ei2 = engineDiscoverer.DiscoverEngine(@"Z:\Games\TQIT");
            DumpEngineInfo(ei2);

            var ei3 = engineDiscoverer.DiscoverEngine(@"Z:\Games\TQIT\Immortal Throne");
            DumpEngineInfo(ei3);


            // Module Discoverer should be bound to IProjectHost
            // my need -> engine
            var moduleDiscoverer = new ModuleDiscoverer();

            //var mi0 = moduleDiscoverer.DiscoverModule(@"G:\Games\TQAE-SV-AERA");
            //DumpModuleInfo(mi0);
            //return;

            var mi1 = moduleDiscoverer.DiscoverModule(@"G:\Games\TQAE");
            DumpModuleInfo(mi1);

            var mi2 = moduleDiscoverer.DiscoverModule(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");
            DumpModuleInfo(mi2);

            var mi3 = moduleDiscoverer.DiscoverModule(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)\gdx1");
            DumpModuleInfo(mi3);

            var mi4 = moduleDiscoverer.DiscoverModule(@"Z:\Games\TQIT");
            DumpModuleInfo(mi4);

            var mi5 = moduleDiscoverer.DiscoverModule(@"Z:\Games\TQIT\Immortal Throne");
            DumpModuleInfo(mi5);

            return;
        }

        private static void DumpEngineInfo(EngineInfo engineInfo)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Engine Info:\n");
            sb.AppendFormat("  Physical Path: {0}\n", engineInfo.PhysicalPath);
            sb.AppendFormat("         Exists: {0}\n", engineInfo.Exists);
            sb.AppendFormat("     Amibigious: {0}\n", engineInfo.Ambigious);
            sb.AppendFormat("   Engine Class: {0}\n", engineInfo.EngineClass);
            sb.AppendFormat("  Artifacts: ({0})\n", engineInfo.Artifacts.Count);
            foreach (var artifactInfo in engineInfo.Artifacts)
            {
                sb.AppendFormat("    Artifact: {0}\n", artifactInfo.RelativePath);
            }

            Console.WriteLine(sb.ToString());
        }

        private static void DumpModuleInfo(ModuleInfo moduleInfo)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Module Info:\n");
            sb.AppendFormat("  Physical Path: {0}\n", moduleInfo.PhysicalPath);
            sb.AppendFormat("         Exists: {0}\n", moduleInfo.Exists);
            sb.AppendFormat("           Name: {0}\n", moduleInfo.Name);
            sb.AppendFormat("     Amibigious: {0}\n", moduleInfo.Ambigious);
            sb.AppendFormat("  Engine Family: {0}\n", moduleInfo.EngineFamily);
            sb.AppendFormat("   Has Database: {0}\n", moduleInfo.HasDatabase);
            sb.AppendFormat("  Has Resources: {0}\n", moduleInfo.HasResourceBundles);

            sb.AppendFormat("  Database Path: {0}\n", moduleInfo.Database?.RelativePath);

            foreach (var languageSymbol in moduleInfo.Languages)
            {
                var languageName = languageSymbol.Language.Name;

                var resourceBundles = moduleInfo.GetResourceBundles(languageSymbol);
                sb.AppendFormat("  Resource Bundles: {0} ({1})\n", languageName, resourceBundles.Count);
                foreach (var rbi in resourceBundles
                    .OrderBy(x => x.Prefix)
                    .ThenBy(x => x.Priority)
                    .ThenBy(x => x.RelativePath))
                {
                    sb.AppendFormat("    Bundle: [{0}] (P{1}) {2} => {3}", rbi.Kind, rbi.Priority, rbi.RelativePath, rbi.Prefix);
                    if (rbi.LanguageSymbol != LanguageSymbol.Invariant)
                    {
                        sb.AppendFormat(" ({0})", rbi.LanguageSymbol.Language.Code);
                    }
                    sb.Append('\n');
                }
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
