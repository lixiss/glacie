using System;

using Glacie.CommandLine.IO;
using Glacie.Data.Arz;
using Glacie.Data.Arz.Utilities;

namespace Glacie.Cli.Arz.Commands
{
    internal sealed class VerifyCommand : DatabaseCommand
    {
        private string Database { get; }

        public VerifyCommand(string database)
        {
            Database = database;
        }

        public int Run()
        {
            using var progress = StartProgress("Verifying...");

            try
            {
                ArzVerifier.Verify(Database);
            }
            catch(ArzException arzEx)
            {
                Console.Error.WriteLine("[fail] {0}", Database);
                throw new CliErrorException(arzEx.Message);
            }

            // TODO: (ArzVerifier) current ArzVerifier is very limited,
            // so we just try read database in full mode normally,
            // this dirty way to ensure what file is not corrupted.
            try
            {
                using ArzDatabase database = ReadDatabase(Database,
                    options: CreateReaderOptions(ArzReadingMode.Full),
                    progress: progress);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[fail] {0}", Database);
                throw new CliErrorException("Failed to open database.", ex);
            }

            Console.Out.WriteLine("[ ok ] {0}", Database);

            return 0;
        }
    }
}
