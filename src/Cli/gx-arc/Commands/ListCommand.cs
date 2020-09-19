using System;
using System.IO;
using System.Linq;

using Glacie.CommandLine.IO;
using Glacie.Data.Arc;

namespace Glacie.Cli.Arc.Commands
{
    internal sealed class ListCommand : Command
    {
        public string Path { get; }

        public ListCommand(string archive)
        {
            Path = archive;
        }

        public int Run()
        {
            if (!File.Exists(Path))
            {
                throw new CliErrorException("File does not exist: " + Path);
            }

            ListArchive(Path);
            return 0;
        }

        private void ListArchive(string path)
        {
            using var archive = ArcArchive.Open(path);
            ListArchive(archive);
        }

        private void ListArchive(ArcArchive archive)
        {
            var qEntryNames = archive.GetEntries()
                .Select(x => x.Name)
                .OrderBy(x => x, NaturalOrderStringComparer.Ordinal);

            foreach (var entryName in qEntryNames)
            {
                Console.Out.WriteLine(entryName);
            }
        }
    }
}
