using System;
using System.Linq;
using System.Text;

using Glacie.Abstractions;
using Glacie.Localization;
using Glacie.Modules;
using Glacie.Modules.Builder;
using Glacie.ProjectSystem.Builder;
using Glacie.Services;
using Glacie.Targeting.Default;
using Glacie.Targeting.GD;
using Glacie.Targeting.TQAE;

namespace Glacie
{
    internal class Usage1
    {
        public static void Run()
        {
            // Localization:
            // LanguageSymbol
            // LanguageInfo
            //   Code
            //   Name
            // LanguageInfoBuilder.Register()


            // Design:
            // Module Discoverer -> ModuleBuilder -> Module (Source or Target, limit currently only to Source)
            // Raw Code -> ModuleBuiler -> Module
            // ModuleBuilder -> supports IModuleInfo
            // Module -> supports IModuleInfo

            //-- Database in module:
            // Can be configured by physical path
            // Can get existing instance (IArzDatabase) -> optionally with ownership
            // IDatabaseInfo

            //-- ResourceBundle in module:
            // ...
            // IResourceBundleInfo
            //

            // Making module in code:
            if (false)
            {
                var mb = new ModuleBuilder();
                mb.WithPhysicalPath(@"G:\Games\TQAE")
                    .WithDatabase("database/database.arz");
                // .WithDatabase(obj); // "database/database.arz"
                // .WithPhysicalPath();

                DumpModuleInfo(mb);
            }

            // Making module from discovery:
            if (false)
            {
                var mds = new ModuleDiscoverer(new TqaeLanguageProvider());
                var mb = mds.DiscoverModule(@"G:\Games\TQAE-SV-AERA");
                // var mb = mds.DiscoverModule(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");
                DumpModuleInfo(mb);
                mb.CreateSourceModule();
            }

            // TODO: try make ProjectContext simple as possible currently
            // var context = new Context(ProjectContext/IProject, new TargetModule(), new[] { mb.CreateSourceModule() });

            // TODO: Model real top-level API here.
            // Project: Looks nice... 
            var projectBuilder = new ProjectBuilder();
            projectBuilder.AddSource(@"G:\Games\TQAE");
            var project = projectBuilder.Build();
            var context = project.CreateContext();

        }

        private static void DumpModuleInfo(IModuleInfo moduleInfo)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Module Info:\n");
            sb.AppendFormat("  Physical Path: {0}\n", moduleInfo.PhysicalPath ?? "(null)");
            // sb.AppendFormat("         Exists: {0}\n", moduleInfo.Exists);
            // sb.AppendFormat("           Name: {0}\n", moduleInfo.Name);
            // sb.AppendFormat("     Amibigious: {0}\n", moduleInfo.Ambigious);
            // sb.AppendFormat("  Engine Family: {0}\n", moduleInfo.EngineFamily);
            sb.AppendFormat("   Has Database: {0}\n", moduleInfo.HasDatabase);
            // sb.AppendFormat("  Has Resources: {0}\n", moduleInfo.HasResourceBundles);

            sb.AppendFormat("       Database: {0}\n", moduleInfo.DatabaseInfo?.PhysicalPath ?? "(in-memory)");

            var resourceSet = moduleInfo.ResourceSet;
            foreach (var languageSymbol in resourceSet.Languages)
            {
                var languageName = languageSymbol.LanguageInfo.Name;

                var resourceBundles = resourceSet.Select(languageSymbol);
                sb.AppendFormat("  Resource Bundles: {0} ({1})\n", languageName, resourceBundles.Count);
                foreach (var rbi in resourceBundles
                    .OrderBy(x => x.Prefix)
                    .ThenBy(x => x.Priority)
                    .ThenBy(x => x.PhysicalPath))
                {
                    sb.AppendFormat("    Bundle: [{0}] (P{1}) {2} => {3}", rbi.Kind, rbi.Priority, rbi.PhysicalPath, rbi.Prefix);
                    if (rbi.Language != LanguageSymbol.Invariant)
                    {
                        sb.AppendFormat(" ({0})", rbi.Language.LanguageInfo.Code);
                    }
                    sb.Append('\n');
                }
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
