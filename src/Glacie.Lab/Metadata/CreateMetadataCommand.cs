using System;

using Glacie.CommandLine.IO;
using Glacie.Data.Metadata.V1;
using Glacie.Data.Metadata.V1.Emit;
using Glacie.Metadata.V1;
using Glacie.Data.Resources.V1;
using Glacie.Data.Resources.V1.Providers;
using Glacie.Targeting;

namespace Glacie.Lab.Metadata
{
    // controllermonsterhidden.tpl -> class should be ControllerMonsterHidden, because it should not be overriden by include
    // npcwanderpoint.tpl -> class should be NpcWanderPoint -> similar to controllermonsterhidden.
    // ormenosdropzone.tpl -> class should be OrmenosDropZone -> similar to NpcWanderPoint.

    // dynamicbarrier.tpl -> variable 'invincible' overrides defaultValue after included template.
    //   So, overriding variable after included template it introduced looks like intended.
    // endlessmodecontroller.tpl -> include go after variables (eqnVariables)

    // gamepadbuttonsdescriptionbox.tpl -> violate include order

    public sealed class CreateMetadataCommand : Command
    {
        protected override void RunCore()
        {
            // TODO: Configuration to get game data, glacie.lab.config.

            // TODO: resource filtering

            // var templatesDir = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates";
            var templatesDir = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates";

            //var templateResourceProvider = new FileSystemResourceProvider(templatesDir, "database/templates/");
            //foreach (var x in templateResourceProvider.SelectAll())
            //{
            //    Console.Out.WriteLine("Type: {0}  Name: {1}", x.Type, x.Name);
            //}

            /*
            var targetType = new UnifiedTargetType();

            var metadataProviderFactoryOptions = new MetadataProviderFactoryOptions
            {
                ArzReaderOptions = new Data.Arz.ArzReaderOptions { Mode = Data.Arz.ArzReadingMode.Raw },
                TemplateNameMapper = targetType.GetTemplateNameMapper(),
                TemplateProcessor = targetType.GetTemplateProcessor(),
                // Logger = _log,
            };

            // TODO: must dispose this provider, but must not disposed explicit provider.
            using var metadataProvider = MetadataProviderFactory.Create(templatesDir,
                    metadataProviderFactoryOptions);
            throw Error.NotImplemented();*/


            using var templateResourceProvider = TemplateResourceProviderFactory.Create(templatesDir); // new FileSystemTemplateResolver(templatesDir);
            using var templateProvider = TemplateProviderFactory.Create(templateResourceProvider);

            var databaseDefinitionBuilder = new DatabaseTypeBuilder();

            var templateParser = new TemplateParser(templateProvider, databaseDefinitionBuilder);

            // var definition = templateParser.Parse("monster.tpl");

            // TODO: Parsing multiple things should add errors into diagnostic bag
            if (true)
            foreach (var f in templateResourceProvider.SelectAll())
            {
                // var tName = Path.Combine("database", Path.GetRelativePath(Path.Combine(templatesDir, ".."), f));

                //if (f.Name.EndsWith("copy of lootitemtable_dynweightdynaffix.tpl", StringComparison.OrdinalIgnoreCase)) continue;
                //if (f.Name.EndsWith("copy of lootitemtable_dynweighted_dynaffix.tpl", StringComparison.OrdinalIgnoreCase)) continue;

                var x = templateParser.Parse(f.VirtualPath.Value);
            }

            // WriteFieldGroup(databaseDefinitionBuilder.RootFieldGroupDefinition, 0);

            var databaseDefinition = databaseDefinitionBuilder.CreateDatabaseDefinition();
            ;
            // WriteFieldGroup(databaseDefinition.RootFieldGroupDefinition, 0);

            var document = new XmlDatabaseDefinitionWriter().Write(databaseDefinition);
            document.Save("database.gxmd");
        }

        private void WriteFieldGroup(FieldGroupBuilder g, int indent)
        {
            //Console.Out.WriteLine("{0}{1}{2} ({3})",
            //    new string(' ', indent), g.Name ?? "Root", g.System ? " (system)" : "", g.Id);
            Console.Out.WriteLine("{0}{1}{2}",
                new string(' ', indent), g.Name ?? "Root", g.System ? " (system)" : "", g.Id);

            foreach (var cg in g.Children)
            {
                WriteFieldGroup(cg, indent + 2);
            }
        }

        private void WriteFieldGroup(FieldGroupDefinition fg, int indent)
        {
            //Console.Out.WriteLine("{0}{1}{2} ({3})",
            //    new string(' ', indent), g.Name ?? "Root", g.System ? " (system)" : "", g.Id);
            Console.Out.WriteLine("{0}{1}{2}",
                new string(' ', indent), fg.Name ?? "Root", fg.System ? " (system)" : "", fg.Id);

            foreach (var cg in fg.Children)
            {
                WriteFieldGroup(cg, indent + 2);
            }
        }
    }
}
