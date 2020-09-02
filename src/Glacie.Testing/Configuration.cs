using System.IO;
using System.Reflection;

namespace Glacie.Testing
{
    public sealed class Configuration
    {
        private static Configuration? _configuration;

        public static Configuration Current => GetCurrentConfiguration();

        private static Configuration GetCurrentConfiguration()
        {
            if (_configuration != null) return _configuration;

            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var configuration = new Configuration();

            while (basePath != null)
            {
                var configPath = Path.Combine(basePath, "glacie.testing.config");
                if (File.Exists(configPath))
                {
                    ConfigurationReader.Read(configPath, configuration);
                    if (configuration.Root) break;
                }

                basePath = Path.GetDirectoryName(basePath);
            }

            return _configuration = configuration;
        }

        private Configuration() { }

        public bool Root { get; internal set; } = false;

        public string TitanQuestPath { get; internal set; } = default!;

        public string TitanQuestAnniversaryEditionPath { get; internal set; } = default!;

        public string GrimDawnPath { get; internal set; } = default!;
    }
}
