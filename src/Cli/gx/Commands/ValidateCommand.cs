using System;
using System.Collections.Generic;
using System.Linq;

using Glacie.Abstractions;
using Glacie.CommandLine.IO;
using Glacie.Configuration;
using Glacie.Data.Arc;
using Glacie.Data.Arz;
using Glacie.Diagnostics;
using Glacie.Logging;
using Glacie.Metadata;
using Glacie.Resources;
using Glacie.Data.Resources;
using Glacie.Targeting;

using IO = System.IO;
using Glacie.Metadata.Factories;
using Glacie.Data;
using Glacie.Validation;
using Glacie.ProjectSystem;
using Glacie.ProjectSystem.Builder;
using Glacie.ProjectSystem.Serialization;
using Glacie.Cli.Reporting;
using System.Text;

namespace Glacie.Cli.Commands
{
    // TODO: there is Context-bound command (ContextCommand base class?(
    internal sealed class ValidateCommand : ProjectCommand
    {
        private bool _resolveReferences;
        private string? _outputHtmlReport;

        public ValidateCommand(string project, bool resolveReferences, string? outputHtmlReport = null)
            : base(project)
        {
            _resolveReferences = resolveReferences;
            _outputHtmlReport = outputHtmlReport;
        }

        public void Run()
        {
            // var projectBuilder1 = ProjectSystem.Serialization.ProjectReader.Read(@"G:\Glacie\glacie-test-data\gxprojects\01-tqae.gxproject");
            // var projectBuilder2 = ProjectSystem.Serialization.ProjectReader.Read(@"G:\Glacie\glacie-test-data\gxprojects\03-gd.gxproject");

            var projectPhysicalPath = GetProjectPhysicalPath();
            var projectBuilder = ProjectReader.Read(projectPhysicalPath);
            projectBuilder.WithLogger(Log); // logger is a host option
            projectBuilder.WithDiagnosticListener(DiagnosticListener.Create((diagnostic) =>
            {
                Console.Out.WriteLine(diagnostic.ToString());
            }));
            // TODO: DiagnosticListener <<< !

            //// TODO: this should be done automatically... or via option. Need MetadataDiscoverer.
            //var metadataProvider = MetadataProviderFactory.Create(metadataPath, engineType: null, Log);
            //projectBuilder.WithMetadata(metadataProvider);


            // using var project = CreateProject(grimDawn: false);
            using var project = projectBuilder.Build();
            var context = project.CreateContext();

            // TODO: Show diagnostics

            for (var i = 0; i < 1; i++)
            {
                using var progress = StartProgress("Validating...");
                progress.SetValueUnit("records", true);
                progress.ShowElapsedTime = true;
                progress.ShowRemainingTime = true;
                progress.ShowRate = true;
                progress.ShowValue = true;
                progress.ShowMaximumValue = true;

                var sw = System.Diagnostics.Stopwatch.StartNew();

                Log.Debug("Validating...");

                var validationResult = context.Validate(resolveReferences: _resolveReferences, all: true, progress); // TODO: raise diagnostic messages, or throws...

                sw.Stop();
                Log.Debug("Validate done in: {0:N0}ms", sw.ElapsedMilliseconds);

                var validationSummary = validationResult.GetSummary();
                Log.Information("---- Validation: {0} error(s), {1} warning(s) ----",
                    validationSummary.GetCountBySeverity(DiagnosticSeverity.Error),
                    validationSummary.GetCountBySeverity(DiagnosticSeverity.Warning)
                    );

                // TODO: write report
                // TODO: collect ALL diagnostics, project itself may generate them
                // however Validation is action over context and uses separate reporting.
                var x = validationResult.Bag;
                if (!string.IsNullOrEmpty(_outputHtmlReport))
                {
                    using var stream = IO.File.Create(_outputHtmlReport);
                    using var writer = new IO.StreamWriter(stream, Encoding.UTF8);
                    new DiagnosticReportHtmlWriter(writer, validationResult.Bag).Write();
                }
            }
        }

        //private Project CreateProject(bool grimDawn)
        //{
        //    var projectBuilder = new ProjectBuilder();
        //    projectBuilder.WithLogger(Log);

        //    string metadataPath;
        //    if (grimDawn)
        //    {
        //        var gamePath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)\";
        //        projectBuilder.AddSource(gamePath);
        //        projectBuilder.AddSource(gamePath + "gdx1");
        //        projectBuilder.AddSource(gamePath + "gdx2");

        //        metadataPath = gamePath;
        //    }
        //    else
        //    {
        //        var gamePath = @"G:\Games\TQAE\";
        //        projectBuilder.AddSource(gamePath);

        //        metadataPath = gamePath;
        //    }

        //    var metadataProvider = MetadataProviderFactory.Create(metadataPath,
        //        engineType: null, Log);
        //    projectBuilder.WithMetadata(metadataProvider);

        //    //configuration.Logger = Log;
        //    //configuration.DiagnosticReporter = DiagnosticReporter.Create((diagnostic) =>
        //    //{
        //    //    Console.Out.WriteLine(diagnostic.ToString());
        //    //});

        //    return projectBuilder.Build();
        //}

        /*
        private Context CreateContext(bool grimDawn)
        {
            // TODO: context should alter diagnostic reports
            // TODO: context should select target-type and automatically load metadata
            // TODO: context should have global resource provider to resolve any in-game resource/asset

            var engineType = grimDawn ? EngineType.GetFrom(EngineClass.GD) : EngineType.GetFrom(EngineClass.TQAE);

            string gamePath;
            string metadataPath;
            string databasePath;

            if (grimDawn) { gamePath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)/"; }
            else { gamePath = @"G:\Games\TQAE/"; }
            var resourceManager = new ResourceManager(language: null, Log);
            if (grimDawn)
            {
                resourceManager.AddForSourceDirectory(gamePath, sourceId: 3, bundleNamePrefix: "<gd>");
                resourceManager.AddForSourceDirectory(gamePath + "gdx1", sourceId: 2, bundleNamePrefix: "<gd>/GDX1");
                resourceManager.AddForSourceDirectory(gamePath + "gdx2", sourceId: 1, bundleNamePrefix: "<gd>/GDX2");
            }
            else
            {
                resourceManager.AddForSourceDirectory(gamePath, sourceId: 1, bundleNamePrefix: "<tqae>");
            }

            if (grimDawn) { metadataPath = gamePath; }
            else { metadataPath = gamePath; }
            // metadataPath = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\Templates.arc";

            databasePath = gamePath + "/database/database.arz";


            var resourceResolver = resourceManager.AsResolver();

            // resources comes from Audio, Resources, and Text (special), not handled here
            // Builds underlying resource map...
            var allResources = resourceResolver.SelectAll();


            //Console.Out.WriteLine();
            //Console.Out.WriteLine("Resources:");
            //Console.Out.WriteLine();
            //foreach (var resource in resourceProvider.SelectAll().OrderBy(x => x.Name))
            //{
            //    //Console.Out.WriteLine("{0} : {1}", resource.Provider.Name, resource.Name);
            //    Console.Out.WriteLine("{1}", resource.Provider.Name, resource.Name);
            //}
            //throw Error.InvalidOperation();

            var metadataProvider = MetadataProviderFactory.Create(metadataPath,
                engineType: null, Log);

            var configuration = new ContextConfiguration();
            configuration.Logger = Log;
            configuration.DiagnosticReporter = DiagnosticReporter.Create((diagnostic) =>
            {
                Console.Out.WriteLine(diagnostic.ToString());
            });
            configuration.Sources.Add(new ContextSourceConfiguration
            {
                Path = databasePath,
            });
            if (grimDawn)
            {
                configuration.Sources.Add(new ContextSourceConfiguration
                {
                    Path = gamePath + "/gdx1/database/GDX1.arz",
                });
                configuration.Sources.Add(new ContextSourceConfiguration
                {
                    Path = gamePath + "/gdx2/database/GDX2.arz",
                });
            }

            configuration.Metadata = new ContextMetadataConfiguration
            {
                Path = metadataPath,
                MetadataResolver = engineType.CreateMetadataResolver(metadataProvider, disposeMetadataProvider: true),
            };
            configuration.ResourceResolver = resourceResolver;
            // configuration.RecordNameMapper = Path1Mapper.CreateFor(engineType.RecordPathForm);
            return Context.Create(engineType, configuration);

            //var ctx = Context.Create((c) =>
            //{
            //    c.Source(s => s.Path(@"G:\Glacie\glacie-test-data\arz\tqae-2.9\database\database.arz"));
            //});
            //return ctx;
        }
        */

        /*
        private ResourceProvider CreateResourceProvider(string gamePath, EngineType targetType)
        {
            var resourcesDirectory = IO.Path.Combine(gamePath, "Resources");

            var r1 = CreateFromDirectory(targetType, IO.Path.Combine(gamePath, "Audio"), IO.Path.Combine(gamePath, "Audio"));
            var r2 = CreateFromDirectory(targetType, IO.Path.Combine(gamePath, "Resources"), resourcesDirectory);
            var r3 = CreateFromDirectory(targetType, IO.Path.Combine(gamePath, "Resources/xpack"), resourcesDirectory);
            var r4 = CreateFromDirectory(targetType, IO.Path.Combine(gamePath, "Resources/XPack2"), resourcesDirectory);
            var r5 = CreateFromDirectory(targetType, IO.Path.Combine(gamePath, "Resources/XPack3"), resourcesDirectory);
            return CreateCombinedProvider(targetType, new[] { r1, r2, r3, r4, r5 });
        }

        private ResourceProvider CreateFromDirectory(EngineType targetType, string path, string relativeTo)
        {
            var providers = new List<ResourceProvider>();
            foreach (var file in IO.Directory.EnumerateFiles(path, "*.arc", IO.SearchOption.TopDirectoryOnly))
            {
                if (Exclude(file)) continue;

                var relPath = IO.Path.GetRelativePath(relativeTo, file);
                Path vpRootName = Path.From(
                    IO.Path.Combine(IO.Path.GetDirectoryName(relPath)!,
                        IO.Path.GetFileNameWithoutExtension(file))
                    );

                var provider = new ArcArchiveResourceProvider(
                    name: "<arc>(" + file + ")",
                    virtualBasePath: vpRootName,
                    virtualPathForm: targetType.ResourcePathForm,
                    internalBasePath: default,
                    internalPathForm: PathForm.Any,
                    supportedTypes: null,
                    archive: ArcArchive.Open(file, ArcArchiveMode.Read),
                    disposeArchive: true);
                providers.Add(provider);
            }
            return CreateCombinedProvider(targetType, providers);

            static bool Exclude(string path)
            {
                var fileName = IO.Path.GetFileName(path);
                return fileName.Equals("Dialog_DE.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Dialog_FR.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Dialog_PL.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Dialog_RU.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("PlayerSounds_DE.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("PlayerSounds_FR.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("PlayerSounds_RU.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Sounds_DE.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Sounds_FR.arc", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Sounds_RU.arc", StringComparison.OrdinalIgnoreCase);
            }
        }

        private ResourceProvider CreateCombinedProvider(EngineType targetType, IReadOnlyCollection<ResourceProvider> resourceProviders)
        {
            return new UnionResourceProvider(targetType.ResourcePathForm, resourceProviders);
        }
        */
    }
}
