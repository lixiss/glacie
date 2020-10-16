using System;
using System.Linq;

using Glacie.CommandLine.IO;
using Glacie.Data.Arz;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class ListCommand : DatabaseCommand
    {
        private string Database { get; }

        public ListCommand(string database)
        {
            Database = database;
        }

        public int Run()
        {
            // Use Lazy mode, because we doesn't want read records content.
            using ArzDatabase database = ReadDatabase(Database,
                CreateReaderOptions(ArzReadingMode.Lazy));

            var qRecordNames = database.SelectAll()
                .Select(x => x.Name)
                .OrderBy(x => x, NaturalOrderStringComparer.Ordinal);

            foreach (var recordName in qRecordNames)
            {
                Console.Out.WriteLine(recordName);
            }

            return 0;
        }
    }
}
