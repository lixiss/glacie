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

            foreach (var record in database.GetAll())
            {
                Console.Out.WriteLine(record.Name);
            }

            return 0;
        }
    }
}
