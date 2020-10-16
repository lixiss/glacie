using Glacie.Data.Arz;

namespace Glacie.Modules.Builder
{
    internal sealed class ModuleDatabaseBuilder : IDatabaseInfo
    {
        private readonly string? _physicalPath;
        private readonly ArzDatabase? _database;
        private readonly bool _disposeDatabase;

        public ModuleDatabaseBuilder(
            string? physicalPath = null,
            ArzDatabase? database = null,
            bool disposeDatabase = false)
        {
            Check.That(physicalPath != null || database != null);

            _physicalPath = physicalPath;
            _database = database;
            _disposeDatabase = database != null ? disposeDatabase : true;
        }

        public string? PhysicalPath => _physicalPath;

        public ArzDatabase? Database => _database;

        public ModuleDatabase Build()
        {
            return new ModuleDatabase(_physicalPath, _database, _disposeDatabase);
        }
    }
}
