using System;
using System.IO;

namespace Glacie
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Help:");
                Console.WriteLine("  glacie-checkdb <path-to-mod-database> [<path-to-game-database>]");
                return 1;
            }


            var settings = new Settings { };
            settings.DatabasePath = args[0];
            if (args.Length >= 2) settings.SecondaryDatabasePath = args[1];

            if (!Directory.Exists(settings.DatabasePath))
            {
                Console.WriteLine("ES0002: Directory \"{0}\" is not exist.", settings.DatabasePath);
                return 1;
            }

            if (!string.IsNullOrEmpty(settings.SecondaryDatabasePath) && !Directory.Exists(settings.SecondaryDatabasePath))
            {
                Console.WriteLine("ES0002: Directory \"{0}\" is not exist.", settings.SecondaryDatabasePath);
                return 1;
            }

            var context = new Context(settings.DatabasePath, settings.SecondaryDatabasePath);

            Console.WriteLine("Processing database...");

            ProcessAllFiles(context);

            return 0;
        }

        private static void ProcessAllFiles(Context context)
        {
            var srcFiles = Directory.EnumerateFiles(context.DatabasePath, "*.dbr", SearchOption.AllDirectories);
            foreach (var srcFile in srcFiles)
            {
                var relativePath = context.GetRelativeDatabasePath(srcFile);
                var content = context.GetContent(srcFile);
                var dbRecord = new DbRecord(context, relativePath, content);

                Rules.ProcessDbRecord(dbRecord);
            }
        }
    }
}
