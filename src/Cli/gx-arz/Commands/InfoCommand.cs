using Glacie.CommandLine.IO;
using Glacie.Data.Arz;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class InfoCommand : DatabaseCommand
    {
        private string Database { get; }

        public InfoCommand(string database)
        {
            Database = database;
        }

        public int Run()
        {
            // Use Lazy mode, because we doesn't want read records content.
            using ArzDatabase database = ReadDatabase(Database,
                options: CreateReaderOptions(ArzReadingMode.Lazy));

            database.GetContext().TryInferFormat(out var format);

            Console.Out.WriteLine("        Format: {0}", format);
            Console.Out.WriteLine("  # of records: {0}", database.Count);

            return 0;
        }
    }
}
