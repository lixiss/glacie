using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Glacie.Abstractions;
using Glacie.Analysis.Binary;
using Glacie.CommandLine.IO;
using Glacie.Data;
using Glacie.Data.Arc;
using Glacie.Data.Resources;
using Glacie.Data.Resources.Providers;
using Glacie.Metadata;
using Glacie.Metadata.Builder;
using Glacie.Metadata.Factories;
using Glacie.Metadata.Providers;
using Glacie.Metadata.Serialization;
using Glacie.Resources;
using Glacie.Targeting;

using IO = System.IO;

namespace Glacie.Cli.Metadata.Commands
{
    internal sealed class CreateCommand : Command
    {
        private List<string> Metadata { get; }

        private string Output { get; }

        private bool Multipart { get; }

        private EngineClass EngineClass { get; }

        private string? MultipartMainFile { get; }

        private string? MultipartIncludeSubDirectory { get; }

        private string OutputFormat { get; }

        private bool EmitVarOnly { get; }

        public CreateCommand(
            string metadata,
            string output,
            EngineClass engineType,
            bool multipart = false,
            string? mpMain = null,
            string? mpInclude = null,
            string outputFormat = "GXMD",
            bool emitVarOnly = false)
        {
            Metadata = new List<string>();
            Metadata.AddRange(
                metadata
                    .Split(IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                );
            Output = output;
            Multipart = multipart;
            EngineClass = engineType;
            MultipartMainFile = mpMain;
            MultipartIncludeSubDirectory = mpInclude;
            OutputFormat = outputFormat;
            EmitVarOnly = emitVarOnly;
        }

        public void Run()
        {
            EngineType? engineType;
            if (EngineClass == EngineClass.Unknown)
            {
                engineType = null;
            }
            else
            {
                engineType = EngineType.GetFrom(EngineClass);
            }

            // TODO: discover specified path. understand composite paths: path_to_archive::sub_path
            // to be compatible with win32 file streams, sub_path must not start with $ sign.
            // e.g. filename::$DATA - is win32 file stream, while filename::subpath is composite path.
            // need function in Path which support correct splitting of this.

            var metadataProvider = MetadataProviderFactory.Create(Metadata, engineType: engineType, logger: Log);

            var metadata = metadataProvider.GetDatabaseType();

            var metadataWriterOptions = new MetadataWriterOptions();
            if (OutputFormat.Equals("GXMD", StringComparison.OrdinalIgnoreCase))
            {
                metadataWriterOptions.EmitPatchBoilerplate = false;
            }
            else if (OutputFormat.Equals("GXMP-BOILERPLATE", StringComparison.OrdinalIgnoreCase))
            {
                metadataWriterOptions.EmitPatchBoilerplate = true;
                metadataWriterOptions.EmitRootVarGroup = false;
            }
            // TODO: emit templates (.tpl), everything is done for what
            else throw Error.InvalidOperation("Unknown or unsupported output format.");

            if (EmitVarOnly)
            {
                metadataWriterOptions.EmitVarPropertyAsAttribute = true;
                metadataWriterOptions.IncludeOnlyVarProperties = true;
            }

            var filesWritten = 0;
            var metadataWriter = new MetadataWriter(metadata, metadataWriterOptions);
            if (Multipart)
            {
                metadataWriter.Write(
                    (path) =>
                    {
                        var outputPath = IO.Path.Combine(Output, path);
                        var outputDir = IO.Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(outputDir))
                        {
                            IO.Directory.CreateDirectory(outputDir);
                        }
                        filesWritten++;
                        Log.Information("Writing: {0}", outputPath);
                        return IO.File.Create(outputPath);
                    },
                    mainFileName: MultipartMainFile,
                    includeSubDirectory: MultipartIncludeSubDirectory);
            }
            else
            {
                var outputDirectory = IO.Path.GetDirectoryName(Output);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    IO.Directory.CreateDirectory(outputDirectory);
                }
                filesWritten++;
                Log.Information("Writing: {0}", Output);
                metadataWriter.Write(IO.File.Create(Output));
            }

            Console.Out.WriteLine("Written {0} file(s)", filesWritten);
        }

        // TODO: rework code below -> move to gx-lab useful parts.
        private void CreateFromPath(string path)
        {




            var engineType = new TqaeEngineType();

            // TODO: (!!!) MetadataProvider MUST return final types,
            // however internal providers implementation should use builders
            // (MetadataReaders?), (e.g. everything what get loaded, e.g.
            // ephemeral, templates or gxmd) should return builders.

            /*
            if (false)
            {
                // API
                var metadataProvider = __MetadataProviderFactory.Create(path, engineType, Log);

                var databaseTypeBuilder = metadataProvider.GetDatabaseTypeBuilder();
                foreach (var x in databaseTypeBuilder.RecordTypes)
                {
                    Console.Out.WriteLine("Name: {0}", x.Name);
                }

                var databaseType = metadataProvider.GetDatabaseTypeBuilder().Build();

                var writer = new __XmlMetadataWriter(
                    includeOnlyVarProperties: true,
                    varPropertiesAsAttributes: true,
                    varGroups: true,
                    rootVarGroup: false);

                var document = writer.Write(
                        metadataProvider.GetDatabaseTypeBuilder().Build()
                    );
                document.Save("output.gxmd");


                var outputDirectory = IO.Path.GetFullPath("out");

                var multipart = writer.WriteMultipart(
                    metadataProvider.GetDatabaseTypeBuilder().Build(),
                    "!metadata", includeSubDirectory: null
                    );
                foreach (var x in multipart)
                {
                    var outputPath = IO.Path.Combine(outputDirectory, x.Path);
                    var outputDir = IO.Path.GetDirectoryName(outputPath);
                    Console.Out.WriteLine("Writing: {0}...", outputPath);

                    IO.Directory.CreateDirectory(outputDir);
                    x.Document.Save(outputPath);
                }

                metadataProvider.Dispose(); // free-up all used resources.
            }

            // Simple Metadata Reader & Writer
            if (false)
            {
                var metadataBuilder = new MetadataBuilder();

                var metadataReaderOptions = new MetadataReaderOptions
                {
                    ResourceResolver = (path) =>
                    {
                        if (IO.File.Exists(path))
                        {
                            Console.Out.WriteLine("Reading... {0}", path);
                            return IO.File.OpenRead(path);
                        }
                        return null;
                    }
                };
                var metadataReader = new MetadataReader(metadataBuilder, metadataReaderOptions);

                // var path1 = "output.gxmd";
                // metadataReader.Read(path1);

                var path2 = "input1/!metadata.gxmd";
                metadataReader.Read(path2);


                var databaseType = metadataBuilder.Build();

                var metadataWriterOptions = new MetadataWriterOptions
                {
                };

                var metadataWriter = new MetadataWriter(databaseType, metadataWriterOptions);
                metadataWriter.Write(IO.File.OpenWrite("new.gxmd"));
                metadataWriter.Write((path) =>
                {
                    var targetPath = IO.Path.Combine("out1", path);
                    var dir = IO.Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) { IO.Directory.CreateDirectory(dir); }
                    return IO.File.OpenWrite(targetPath);
                });
            }
            */

            // Template Metadata Reader
            {
                // var metadataPath = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates\";
                // var metadataPath = "templates/";
                //var metadataPath = @"G:\Games\TQAE";
                //var metadataPath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)";

                //// Specifying database directory, templates.arc exist, and choosen.
                //var metadataPath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)\database";

                //// Specifying templates directory directly
                //var metadataPath = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates";

                //// Specifying directory where templates directory and templates.arc exist -> prefer to directory.
                //var metadataPath = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1";

                //// Creating from archive, autodetection by archive layout.
                // var metadataPath = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\Templates.arc";
                //var metadataPath = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates.arc";
                //var metadataPath = @"G:\Glacie\glacie-test-data\tpl\Templates_SVAERA_1.7.zip";
                var metadataPath = @"G:\Glacie\glacie-test-data\tpl\tqit";

                var metadataProvider = MetadataProviderFactory.Create(metadataPath, engineType: null, logger: Log);

                /*

                var metadataBuilder = new MetadataBuilder();
                metadataBuilder.BasePath = Path.From("database/templates");

                var resourceManager = new ResourceManager(language: null, Log);
                var resourceBundle = new FileSystemBundle("templates", metadataPath, new[] { ResourceType.Template });
                resourceManager.AddBundle("database/templates/", language: null, bundleName: "templates", 0, resourceBundle);

                var mappedRecordTypePath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // TODO: Create new TemplateMetadataReader => read templates into builder.
                var templateMetadataReader = new TemplateMetadataReader(metadataBuilder,
                    engineType.GetTemplateProcessor(),
                    (templateName) =>
                    {
                        var resource = resourceManager.AsResolver().Resolve(templateName);
                        Check.That(resource.Type == ResourceType.Template);
                        return resource.Open();
                    },
                    recordTypePathMapper: (path) =>
                    {
                        if (!engineType.TryMapRecordTypePath(path, out var result))
                        {
                            if (!mappedRecordTypePath.Contains(path.ToString()))
                            {
                                Log.Warning("Record type path is not mapped: \"{0}\".", path);
                                mappedRecordTypePath.Add(path.ToString());
                            }
                        }
                        return result;
                    });

                // read all templates
                foreach (var x in resourceManager.SelectAll())
                {
                    if (engineType.IsExcludedTemplateName(x.Path)) continue;

                    // Exlude options
                    templateMetadataReader.Read(x.Name);
                }
                */

                var metadataWriter = new MetadataWriter(metadataProvider.GetDatabaseType());
                metadataWriter.Write(IO.File.Create("out-from-templates.gxmd"));

                //foreach (var x in metadataProvider.GetDatabaseType().RecordTypes
                //    .Select(x => x.Path.ToString()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                //    .Where(x => !x.Any(y => char.IsUpper(y))))
                //{
                //    Console.Out.WriteLine("Probably\"{0}\",", x);
                //}
            }


            // singlepart
            // var gxmdMetadataBuilder1 = MBFactory.Create("output.gxmd");

            // multipart
            // var gxmdMetadataBuilder2 = MBFactory.Create("out/!metadata.gxmd");

        }

        private void RunOld()
        {
            /*
            // TODO: Move this into lab-tests / lab-data.

            // Creating from file system path (mod or game data directory)
            // Also matches to ArtManager's default Working directory.
            MetadataBuilderLoader.Create(@"G:\Games\TQAE");
            MetadataBuilderLoader.Create(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");

            // Specifying database directory, templates.arc exist, and choosen.
            MetadataBuilderLoader.Create(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)\database");

            // Specifying templates directory directly
            MetadataBuilderLoader.Create(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates");

            // Specifying directory where templates directory and templates.arc exist -> prefer to directory.
            MetadataBuilderLoader.Create(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1");

            // Creating from archive, autodetection by archive layout.
            MetadataBuilderLoader.Create(@"G:\Glacie\glacie-test-data\tpl\tqae-2.9\Templates.arc");
            MetadataBuilderLoader.Create(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates.arc");
            MetadataBuilderLoader.Create(@"G:\Glacie\glacie-test-data\tpl\Templates_SVAERA_1.7.zip");
            */

            // API

            if (true)
            {
                CreateFromPath(@"G:\Games\TQAE");
            }
            else
            {


                // Creating from file system path (mod or game data directory)
                // Also matches to ArtManager's default Working directory.
                CreateFromPath(@"G:\Games\TQAE");
                CreateFromPath(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)");

                // Specifying database directory, templates.arc exist, and choosen.
                CreateFromPath(@"Z:\Games\Grim Dawn 1.1.7.1 (39085)\database");

                // Specifying templates directory directly
                CreateFromPath(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates");

                // Specifying directory where templates directory and templates.arc exist -> prefer to directory.
                CreateFromPath(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1");

                // Creating from archive, autodetection by archive layout.
                CreateFromPath(@"G:\Glacie\glacie-test-data\tpl\tqae-2.9\Templates.arc");
                CreateFromPath(@"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates.arc");
                CreateFromPath(@"G:\Glacie\glacie-test-data\tpl\Templates_SVAERA_1.7.zip");
            }

            return;

            // Create ResourceManager for .tpl template source.
            using var resourceManager = new ResourceManager(
                language: null,
                Log);

            // var archivePath = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\Templates.arc";
            var archivePath = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates.arc";
            var archive = ArcArchive.Open(
                archivePath,
                ArcArchiveMode.Read);
            // TODO: look for "actor.tpl" inside archive to determine right injection point.

            // var namesToMatch = new[]{ "Some/Path/Templates\\\\Actor.TPL" };
            // var namesToMatch = archive.SelectAll().Select(x => x.Name);

            var fullFiles = IO.Directory.EnumerateFiles(@"G:\Glacie\glacie-test-data\tpl\", "*.tpl", IO.SearchOption.AllDirectories);
            var namesToMatch = fullFiles.Select(x => IO.Path.GetRelativePath(@"G:\Glacie\glacie-test-data\tpl\", x));

            var ext = IO.Path.GetExtension(".zip");
            Console.Out.Write(ext);
            return;

            foreach (var x in namesToMatch)
            {
                if (Path1.From(x).EndsWith("actor.tpl", Path1Comparison.OrdinalIgnoreCase))
                {
                    Console.Out.WriteLine("Found: {0}", x);

                    var prefix = IO.Path.GetDirectoryName(x);
                    Console.Out.WriteLine("Prefix inside archive: {0}", prefix);

                    // if trim ending templates => and build prefix (templates)
                    // if trim ending database => and build prefix (database)
                    // ending prefix is something inside archive
                }
            }

            return;




            var bundle = new ArcArchiveBundle("templates", archivePath, new[] { ResourceType.Template }, archive, true);
            resourceManager.AddBundle(
                // "database/", // for TQ
                "database/templates/", // for GD
                language: null,
                "templates-bundle", 0,
                bundle);

            var names = new List<string>();
            foreach (var x in resourceManager.SelectAll())
            {
                names.Add(x.Name);
                Console.Out.WriteLine(x.Name);
            }
            IO.File.WriteAllLines("gd-template-list.txt", names.OrderBy(x => x));
            return;




            var gamePath = @"G:\Games\TQAE";
            // var gamePath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)";

            // TQ
            resourceManager.AddForSourceDirectory(gamePath, sourceId: 1);

            // GD
            //resourceManager.AddForSourceDirectory(gamePath, sourceId: 3);
            //resourceManager.AddForSourceDirectory(IO.Path.Join(gamePath, "gdx1"), sourceId: 2);
            //resourceManager.AddForSourceDirectory(IO.Path.Join(gamePath, "gdx2"), sourceId: 1);

            // TODO: reverse source ids - target is zero. zero is maximum priority.
            // AddResourceBundles(resourceManager, gamePath, sourceId: 3, bundleNamePrefix: null);
            // AddResourceBundles(resourceManager, IO.Path.Join(gamePath, "gdx1"), sourceId: 2, "gdx1");
            // AddResourceBundles(resourceManager, IO.Path.Join(gamePath, "gdx2"), sourceId: 1, "gdx2");

            var allResources = resourceManager.SelectAll();
            Log.Information("Total: {0} resources", resourceManager.Count);
        }


        /*public void RunCreateMetadataTemp()
        {
            Log.Information("Creating metadata...");

            var engineType = new TqaeEngineType();

            // 1. Configure file-system provider to read templates.
            var templatesDir = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates";
            // var templatesDir = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates";
            using ResourceBundle templateResourceProvider = new FileSystemBundle(
                name: null,
                physicalPath: templatesDir,
                supportedResourceTypes: null);
            //virtualBasePath: default, // Path.From("database/templates/"),
            //virtualPathForm: default, // PathForm.Relative | PathForm.Strict | PathForm.Normalized | PathForm.DirectorySeparator | PathForm.LowerInvariant,
            //internalBasePath: default,
            //internalPathForm: PathForm.Any,
            //supportedTypes: new[] { ResourceType.Template },
            // basePath: templatesDir);

            // 2. Configure resource resolver.
            // TODO: want to have ResourceResolver factory? also think about combining.
            using ResourceResolver resourceResolver = new VirtualPathMappingResourceResolver(
                templateResourceProvider, Path.From("database/templates/"),
                PathForm.Relative | PathForm.Strict | PathForm.Normalized | PathForm.DirectorySeparator | PathForm.LowerInvariant
                );

            // TODO: What if TemplateProvider will wrap ResourceProvider?

            // 3. Configure template provider to parse templates.
            using TemplateProvider templateProvider = new TemplateProvider(resourceResolver);

            // 4. Configure template resolver to find correct templates.
            // E.g. it is exist only for adjust requests or implement caching... (omitting %TEMPLATE_DIR%, etc...)
            using TemplateResolver templateResolver = new TemplateResolver(templateProvider);

            // TODO: !!! Complete metadata, and then back to resource provider.
            // Glacie.Data.Resources -> should provide ResourceBundle(s), or ResourceStorage,
            // e.g. abstract FS, ZIP or ARC.
            // Glacie.Resources -> should provide ResourceProvider which will manage
            // resources (including virtual path management). It still should be 
            // consumed over IResourceResolver...

            // 5. Create TemplateMetadataProvider -> provide builders.
            using TemplateMetadataProvider metadataProvider = new TemplateMetadataProvider(
                templateResolver,
                templateProcessor: engineType.GetTemplateProcessor(),
                Log);
            // TODO: metadata provider should provide DatabaseTypeBuilder

            foreach (var x in resourceResolver.SelectAll())
            {
                if (x.Type != Abstractions.ResourceType.Template) continue;

                var recordType = metadataProvider.GetRecordType(x.Path);
                Console.Out.WriteLine("Name: {0}", x.Path);
            }

            // 6. Create MetadataResolver -> provides DatabaseType/RecordType, with optional remapping.

            // 7. GXMD Writer (Writer should operate over TemplateMetadataProvider?),
            // e.g. write builders. Generally final RecordType should be consumed only for consuming
            // metadata. However GXMP writer might want write final RecordType (because ...
            var document = new DatabaseTypeWriter(
                includeOnlyVarProperties: true,
                varPropertiesAsAttributes: true,
                varGroups: true,
                rootVarGroup: false)
                .Write(
                    metadataProvider.GetDatabaseTypeBuilder().Build()
                );
            document.Save("output.gxmd");

            // TODO: write .gxmp with only simple properties (arz-value)/(etc),
            // however they might be included in base too.

            // TODO: write .gxmp for manual patching, generate custom attributes,
            // to mark fields are not had been validated or processed.

            // TODO: Analysis: collect all references by field.
        }*/
    }
}
