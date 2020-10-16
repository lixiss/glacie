using System;
using System.Linq;
using System.Text;

using Glacie.GX1.Discovery;

namespace Glacie.GX1.Lab
{
    public class Usage1
    {
        private static void DumpModuleDiscoverResult(ModuleInfo mdr)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Module Discovery Result:\n");
            sb.AppendFormat("  Physical Path: {0}\n", mdr.PhysicalPath);
            sb.AppendFormat("           Name: {0}\n", mdr.Name);
            sb.AppendFormat("     Amibigious: {0}\n", mdr.Ambigious);
            sb.AppendFormat("  Engine Family: {0}\n", mdr.EngineFamily);
            sb.AppendFormat("   Has Database: {0}\n", mdr.HasDatabase);
            sb.AppendFormat("  Has Resources: {0}\n", mdr.HasResources);

            sb.AppendFormat("  Database Path: {0}\n", mdr.DatabasePath);

            foreach (var languageKey in mdr.Languages)
            {
                string languageCode;
                if (string.IsNullOrEmpty(languageKey)) languageCode = "Invariant";
                else languageCode = languageKey;

                var resourceBundles = mdr.GetResourceBundlesByLanguage(languageKey);
                sb.AppendFormat("  Resource Bundles: {0} ({1})\n", languageCode, resourceBundles.Count);
                foreach (var rbi in resourceBundles.OrderBy(x => x.Path))
                {
                    sb.AppendFormat("    Bundle: [{0}] (P{1}) {2} => {3}", rbi.Kind, rbi.Priority, rbi.Path, rbi.Prefix);
                    if (!string.IsNullOrEmpty(rbi.Language))
                    {
                        sb.AppendFormat(" ({0})", rbi.Language);
                    }
                    sb.Append('\n');
                }
            }

            Console.WriteLine(sb.ToString());
        }

        public static void Run()
        {
            var md = new ModuleDiscoverer();

            var dr0 = md.Discover(@"G:\Games\TQAE-SV-AERA");
            DumpModuleDiscoverResult(dr0);
            return;

            var dr1 = md.Discover(@"G:\Games\TQAE");
            DumpModuleDiscoverResult(dr1);

            var dr2 = md.Discover(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");
            DumpModuleDiscoverResult(dr2);

            var dr3 = md.Discover(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)\gdx1");
            DumpModuleDiscoverResult(dr3);


            return;


            // SourceModules should be available outside ProjectContext, so each
            // Module they should be connected to life-time/caching service.
            // Only readonly and successfully opened modules should be cacheable?
            var sm1 = new SourceModule("GDX1");


            // Hosting: IHost
            // Logging (for host, but might be overriden in IProjectHost)
            // IProjectHost
            // Project(s)
            // Module(s)
            // ProjectContext->Context->work(Database,Record,etc.)


            // 0. Need configure/model project/output listener:
            // Something similar to: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging
            // which may log output from runtime loggers,
            // collect diagnostic messages.
            // When project opens - it may emit messages, and we want collect and show them.
            // So need:
            // RuntimeLogs
            // DiagnosticLogs
            // Log messages must be structured.
            // 1. Listener: may log and discard messages immediately (as it doesn't need them).
            // 2. Listener: may collect messages to process (or to show them later UI.)
            // Listener should be associated with project. Reloading project might clear output...
            // However, Listener from user perspective is just structured logger or logfactory.

            // Hosting:
            // IHost
            // IProjectHost
            // provides host-dependent services (like logging, etc...), caching

            // Modules:
            // Database+Resource, or database-only module or resource-only modules
            // + define what is ResourceBundle (it is similar but different than in Glacie.Data.Rersource)



            // 1. How to load project file.
            // 2. How to create ProjectContext from project. (GetContext() / CreateContext()?)

            // ProjectConfiguration -> object as it expressed in file, typed, but no values expanded.
            // it is more like ProjectModel?
            // Project.Builder => ProjectBuilder? ProjectBuilder construct Project, e.g. ProjectBuilder is
            //    editable view of Project, which can be used in UI to modify opened project? However,
            //    builder might return new Project after it changed?
            // Project -> should be able re-create context multiple times, and
            //            reuse objects (which it can reuse).
            //            If we want prevent object reusing, then we should close it and/or refresh.
            //            Project should rely on even higher service level to manage shareable objects.
            // ProjectContext -> expose everything in resolved form, e.g. enigne type / etc...

            // Module can be created from:
            // path
            // with directly specified entities
            // constructed with builder?

            // Naming of Context: may be rename it to something other? No.


            // Create source module
            // Create target module





        }
    }
}
