using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Glacie.CommandLine;
using Glacie.CommandLine.IO;
using Glacie.CommandLine.UI;
using Glacie.Data.Tpl;

namespace Glacie.Data.Templates.Lab
{
    internal static class Program
    {
        internal static IConsole Console { get; private set; } = default!;

        private static void Main(string[] args)
        {
            using var terminal = new Terminal();
            Console = terminal;

            var sw = Stopwatch.StartNew();

            if (false)
            {
                // var path = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates\charanimationtable.tpl";
                // var path = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates\weapon_bow.tpl";
                var path = @"G:\Glacie\glacie-test-data\tpl\tqae-2.9\templates\skill_turretfirecontrol.tpl";
                // var path = @"G:\Glacie\glacie-test-data\tpl\gd-1.1.7.1\templates\ambientcharacter.tpl";
                // using var inputTemplate = File.OpenRead();
                using var inputTemplate = File.OpenRead(path);

                var templateReader = new TemplateReader();
                var template = templateReader.Read(inputTemplate, path);

                TemplateWriter.Write(System.Console.Out, template);
            }

            // TODO: create validation tests, we must know what we may have
            var allTemplates = new List<Template>();
            if (true)
            {
                // sv-aera + underlord = buggy templates, don't use them.
                // use TQAE and GD templates. TQIT might also be useful.
                var path = @"G:\Glacie\glacie-test-data\tpl\tqit";
                var files = Directory.EnumerateFiles(path, "*.tpl", SearchOption.AllDirectories);
                var templateReader = new TemplateReader();
                foreach (var file in files)
                {
                    // Console.Out.WriteLine(file);
                    var template = templateReader.Read(file);
                    allTemplates.Add(template);
                }

                var types = new HashSet<string>();
                var classes = new HashSet<string>();
                foreach (var template in allTemplates)
                {
                    foreach (var v in GetAllVariables(template.Root))
                    {
                        types.Add(v.Type);
                        classes.Add(v.Class);
                    }
                }

                Console.Out.WriteLine("Types:");
                foreach (var x in types)
                {
                    Console.Out.WriteLine(x);
                }
                Console.Out.WriteLine();

                Console.Out.WriteLine("Classes:");
                foreach (var x in classes)
                {
                    Console.Out.WriteLine(x);
                }
                Console.Out.WriteLine();
            }


            sw.Stop();
            Console.Out.WriteLine("Done In: {0}ms", sw.ElapsedMilliseconds);

            WriteMemoryMetrics();


            return;
        }

        private static IEnumerable<TemplateVariable> GetAllVariables(TemplateGroup group)
        {
            foreach (var v in group.Variables)
            {
                yield return v;
            }

            foreach (var g in group.Groups)
            {
                foreach (var v in GetAllVariables(g)) yield return v;
            }
        }

        private static void WriteMemoryMetrics()
        {
            var totalAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);

            var maxGeneration = GC.MaxGeneration;
            Console.Out.Write("    GC Collection Count: ");
            for (var i = 0; i <= maxGeneration; i++)
            {
                if (i > 0) Console.Out.Write(" / ");
                Console.Out.Write("{0}", GC.CollectionCount(i));
            }
            Console.Out.WriteLine();

            Console.Out.WriteLine("        Total Allocated: {0} bytes", totalAllocatedBytes);
            Console.Out.WriteLine("           Total Memory: {0} bytes", GC.GetTotalMemory(false));
            Console.Out.WriteLine("      Total Memory (FC): {0} bytes", GC.GetTotalMemory(true));
            Console.Out.WriteLine();
        }
    }
}
