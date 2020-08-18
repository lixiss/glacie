using System;
using System.Collections.Generic;
using Glacie.Abstractions;
using Glacie.Infrastructure;

namespace Glacie
{
    public sealed class Context : IDisposable
    {
        #region Factory

        public static Context Create(Action<IContextBuilder> configure)
        {
            Check.Argument.NotNull(configure, nameof(configure));

            var builder = new ContextBuilder();
            configure(builder);
            return builder.Build();
        }

        #endregion

        private readonly Source[] _sources;
        private readonly Target _target;
        private Database? _database;

        internal Context(Source[] sources, Target target)
        {
            Check.Argument.NotNull(sources, nameof(sources));
            Check.Argument.NotNull(target, nameof(target));

            _sources = sources;
            _target = target;
        }

        internal void Open()
        {
            // Should read records from sources (from target and sources in backward)
            var database = new Database(this, _target.GetDatabase());

            // TODO: (Gx) alternatively, instead of making record mapping, we might
            // just merge down into single in-memory ArzDatabase. However this
            // may block some features (this would require almost full reencoding).

            foreach (var databaseProvider in GetDatabaseProviders())
            {
                //TODO: (Gx) Need Logging
                Console.WriteLine("#{0}: {1}", databaseProvider.Identifier, "<path> or <in-memory>");

                if (databaseProvider.CanProvideDatabase)
                {
                    // TODO: This might need some optimization or configuration, about database reading mode.
                    // 1. For hybrid mode, we might use alternative approaches: target database exist partially.
                    // 2. We should understand if we can derive string table and which.
                    //    Database which hold most of records is most likely to derive from.
                    // 3. If string table can be derived -> we can use Raw mode for record data importing.
                    // 4. Generally, when multithreading enabled, database better to be in full mode. (it depends)
                    // 5. Generally, best way is to open database in Lazy mode, and then perform bulk-reads if need. (Need support in ArzDatabase)
                    // Generally priority should be to avoid less re-encodings as possible.
                    var db = databaseProvider.GetDatabase();
                    database.LinkRecordReferences(db);
                    // TODO: instead of Import -> use Adopt: adopt should transfer record(s) from
                    // another database, and remove it from owner (e.g. it may modify source), however
                    // this might be done semi-transparent (file-based databased can lazily recover
                    // non-modified content)
                }
                else
                {
                    // When reading DBR's we doesn't want to materialize them as separate
                    // database, so and this will need special support.
                    // Also DBR's can be built into ArzDatabase in form of cache (so they might be actually imported/used easier)
                    // Combining read/save + timestamps can achieve this relatively easy (rebuilding as cache).
                    throw Error.NotImplemented("IDatabaseProvider.CanProvideDatabase == false");
                }
            }

            _database = database;

            Check.True(_database != null);

            IEnumerable<SourceOrTarget> GetDatabaseProviders()
            {
                yield return _target;
                for (var i = _sources.Length; i-- != 0;)
                {
                    yield return _sources[i];
                }
            }
        }

        public void Dispose()
        {
            foreach (var source in _sources)
            {
                source.Dispose();
            }
            _target.Dispose();
        }

        public IReadOnlyCollection<ISource> Sources => _sources;

        public ITarget Target => _target;

        public Database Database => _database!;

        // TODO: (Gx) Select Records
        // TODO: (Gx) Access to Resources
    }
}
