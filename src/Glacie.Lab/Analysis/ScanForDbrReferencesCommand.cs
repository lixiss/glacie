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
    public sealed class ScanForDbrReferencesCommand : Command
    {
        protected override void RunCore()
        {
            // Create ResourceManager for game data folder.
            using var resourceManager = new ResourceManager(
                language: null,
                Log);

            var isTQAE = true;

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

            using var progress = StartProgress("Scanning...");
            progress.SetValueUnit("resources", false);
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.ShowTotalTime = true;
            progress.ShowMaximumValue = true;
            progress.ShowValue = true;
            progress.ShowRate = true;
            progress.SetMaximumValue(resourceManager.Count);

            foreach (var resource in allResources)
            {
                ScanDbrReferences(resource.Open(), resource.Name, outputReport);

                progress.AddValue(1);
            }
        }

        private void ScanDbrReferences(IO.Stream streamToScan, string resourceName, IO.TextWriter outputReport)
        {
            // TODO: It is good idea to scan depending on resource type.
            // wav, mp3, ogg is audio, or Texture => so no dbr references...
            // .map/.level -> int32 little endian length encoded strings, might be optimized to get string tables

            // .msh -> meshes has some metadata in format of (see CreateEntity section, multiple occurences allowed):
            // (This format similar to used by templates, how it is named?)
            //
            // RigidBodyDescription
            // {
            //     parentName = "Bone_R_Foot"
            //     childName = "Bone_R_Toe"
            //     parentOrigin = (-0.152358, 0.000000, 0.000000)
            //     parentXAxis  = (1.000000, 0.000000, 0.000000)
            //     parentYAxis  = (0.000000, -1.000000, 0.000000)
            //     parentZAxis  = (0.000000, 0.000000, 1.000000)
            //     childOrigin = (0.152358, 0.000000, 0.000000)
            //     childXAxis  = (0.997962, 0.063817, 0.000000)
            //     childYAxis  = (0.063817, -0.997962, 0.000000)
            //     childZAxis  = (0.000000, 0.000000, 1.000000)
            //     extents = (0.152358, 0.100000, 0.100000)
            //     collides = "true"
            //     density = "100.000000"
            //     playFallSound = "false"
            // }
            // JointDescription
            // {
            //     rigidBody0 = "17"
            //     rigidBody1 = "21"
            //     jointType = "hinge"
            //     jointConnection = "1"
            //     jointAxis0 = "y"
            //     loStop0 = "0.000000"
            //     hiStop0 = "0.742049"
            //     jointAxis1 = "x"
            //     loStop1 = "0.000000"
            //     hiStop1 = "0.000000"
            //     breakable = "false"
            //     breakChance = "0"
            // }
            // CreateEntity
            // {
            //     attach = "BossAura"
            //     entity = "Records\Effects\Boss Effects\Boss Aura.dbr"
            // }




            using var progress = StartProgress("Scanning: " + resourceName);
            progress.SetValueUnit(CommandLine.UI.ProgressValueUnit.Bytes);
            progress.ShowElapsedTime = true;
            progress.ShowRemainingTime = true;
            progress.ShowTotalTime = true;
            progress.ShowMaximumValue = true;
            progress.ShowValue = true;
            progress.ShowRate = true;
            // progress.Message = resourceName;

            progress.SetMaximumValue(streamToScan.Length);

            int foundDbrReferencesCount = 0;

            using var scanner = new StringStreamScanner(streamToScan, disposeStream: true);
            while (scanner.ReadNext(out var token))
            {
                progress.SetValue(token.Position);

                if (token.Length < 4) continue;

                Check.That(token.Type == StringTokenType.RawAsciiString
                    || token.Type == StringTokenType.Int32LeleAsciiString);

                if (true)
                {
                    var value = token.GetValue();

                    if (value.EndsWith(".dbr", StringComparison.OrdinalIgnoreCase)
                        || value.Contains(".dbr", StringComparison.OrdinalIgnoreCase))
                    {
                        foundDbrReferencesCount++;

                        // Check.That(token.Type == StringTokenType.Int32LeleAsciiString);

                        //outputReport.WriteLine("0x{0:X8} ({1}) ({2}) => \"{3}\"",
                        //    token.Position,
                        //    token.Type,
                        //    token.Length,
                        //    value);
                    }
                }
            }

            if (foundDbrReferencesCount > 0)
            {
                outputReport.WriteLine("{0} => {1}", resourceName, foundDbrReferencesCount);
                Console.Out.WriteLine("{0} => {1}", resourceName, foundDbrReferencesCount);
            }
        }
    }
}
