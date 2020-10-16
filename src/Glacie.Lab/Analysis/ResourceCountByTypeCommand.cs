using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Glacie.Abstractions;
using Glacie.Analysis.Binary;
using Glacie.CommandLine.IO;
using Glacie.Data.Arc;
using Glacie.Resources;

using IO = System.IO;

namespace Glacie.Lab.Analysis
{
    public sealed class ResourceCountByTypeCommand : Command
    {
        protected override void RunCore()
        {
            // Create ResourceManager for game data folder.
            using var resourceManager = new ResourceManager(
                language: null,
                Log);

            var isTQAE = false;

            if (isTQAE)
            {
                var gamePath = @"G:\Games\TQAE";

                // TQ
                resourceManager.AddForSourceDirectory(gamePath, sourceId: 1);
            }
            else
            {
                var gamePath = @"Z:\Games\Grim Dawn 1.1.7.1 (39085)";

                // GD
                resourceManager.AddForSourceDirectory(gamePath, sourceId: 3);
                resourceManager.AddForSourceDirectory(IO.Path.Join(gamePath, "gdx1"), sourceId: 2, "gdx1");
                resourceManager.AddForSourceDirectory(IO.Path.Join(gamePath, "gdx2"), sourceId: 1, "gdx2");
            }

            var allResources = resourceManager.SelectAll();
            Log.Information("Total: {0} resources", resourceManager.Count);

            using var outputReport = IO.File.CreateText("scan-for-dbr-rerferences-output.txt");

            var progress = StartProgress("Scanning...");
            progress.SetValueUnit("resources", false);
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.ShowTotalTime = true;
            progress.ShowMaximumValue = true;
            progress.ShowValue = true;
            progress.ShowRate = true;
            progress.SetMaximumValue(resourceManager.Count);

            Dictionary<ResourceType, int> rCounts = new Dictionary<ResourceType, int>();

            foreach (var resource in allResources)
            {
                if (resource.Type == ResourceType.None)
                {
                    Console.Out.WriteLine("Unassigned resource type: {0}", resource.Name);
                }

                if (rCounts.TryGetValue(resource.Type, out var c))
                {
                    rCounts[resource.Type] = c + 1;
                }
                else
                {
                    rCounts[resource.Type] = 1;
                }

                // ScanDbrReferences(resource.Open(), resource.Name, outputReport);

                progress.AddValue(1);
            }

            foreach (var x in rCounts)
            {
                Console.Out.WriteLine("{0} => {1}", x.Key, x.Value);
            }
        }
    }
}
