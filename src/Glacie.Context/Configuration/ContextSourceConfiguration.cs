using Glacie.Data.Arz;

namespace Glacie.Configuration
{
    public sealed class ContextSourceConfiguration
    {
        public string? Path { get; set; }

        public ArzDatabase? Database { get; set; }
    }
}
