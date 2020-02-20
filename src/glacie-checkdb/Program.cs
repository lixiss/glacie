using System;
using System.IO;

namespace Glacie
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Help:");
                Console.WriteLine("  glacie-checkdb <path-to-database>");
                return 1;
            }


            var settings = new Settings
            {
                DatabasePath = args[0],
            };

            var context = new Context(settings.DatabasePath);

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
