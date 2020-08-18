using System;
using System.Runtime.CompilerServices;
using Glacie.Buffers;
using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    /// <summary>
    /// Acts as factory of ArzDatabase and holds some implementation details.
    /// </summary>
    internal abstract class ArzContext : IDisposable
    {
        private ArzStringTable? _stringTable;
        private ArzRecordClassTable? _recordClassTable;
        private ArzDatabase? _database;
        private string? _path;

        protected ArzContext(string? path)
        {
            _path = path;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // _database should not be disposed - context doesn't own it.
        }

        protected abstract InitializeResult InitializeCore();

        protected void Initialize()
        {
            if (_database != null) throw Error.InvalidOperation();

            var result = InitializeCore();

            _stringTable = result.StringTable ?? throw Error.InvalidOperation("String table is not initialized.");
            _recordClassTable = result.RecordClassTable ?? throw Error.InvalidOperation("Record class table is not initialized.");
            _database = result.Database ?? throw Error.InvalidOperation("Database is not initialized.");
        }

        #region API

        public string? Path => _path;

        public ArzDatabase Database
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _database!;
        }

        public ArzStringTable StringTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stringTable!;
        }

        public ArzRecordClassTable RecordClassTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _recordClassTable!;
        }

        public abstract ArzFileLayout Layout { get; }

        /// <summary>
        /// When true, then file layout might be determined dynamically.
        /// </summary>
        public abstract bool HasLayoutInference { get; }

        internal abstract DataBuffer ReadRawFieldDataBuffer(int offset, int compressedSize, int decompressedSize);

        internal abstract byte[] ReadFieldData(int offset, int compressedSize, int decompressedSize);

        internal abstract byte[] DecodeRawFieldData(Span<byte> source, int decompressedSize);

        #endregion

        protected readonly struct InitializeResult
        {
            public readonly ArzStringTable StringTable { get; }
            public readonly ArzRecordClassTable RecordClassTable { get; }
            public readonly ArzDatabase Database { get; }

            public InitializeResult(ArzStringTable stringTable,
                ArzRecordClassTable recordClassTable,
                ArzDatabase database)
            {
                Check.Argument.NotNull(stringTable, nameof(stringTable));
                Check.Argument.NotNull(recordClassTable, nameof(recordClassTable));
                Check.Argument.NotNull(database, nameof(database));

                StringTable = stringTable;
                RecordClassTable = recordClassTable;
                Database = database;
            }
        }
    }
}
