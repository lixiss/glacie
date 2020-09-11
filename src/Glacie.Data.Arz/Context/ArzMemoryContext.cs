using System;
using System.Collections.Generic;
using Glacie.Buffers;
using Glacie.Data.Arz.Infrastructure;

namespace Glacie.Data.Arz
{
    internal sealed class ArzMemoryContext : ArzContext
    {
        #region Factory

        public static ArzDatabase Create()
        {
            ArzMemoryContext? context = null;
            try
            {
                context = new ArzMemoryContext();

                context.Initialize();
                var database = context.Database;

                // Database itself takes ownership for ArzContext, so we 
                // should not dispose it. However if ReadCore throw exception,
                // we will dispose context.
                context = null;

                return database;
            }
            finally
            {
                context?.Dispose();
            }
        }

        #endregion

        private ArzMemoryContext() : base(path: null) { }

        protected override InitializeResult InitializeCore()
        {
            return new InitializeResult(
                stringTable: new ArzStringTable(),
                recordClassTable: new ArzRecordClassTable(),
                database: new ArzDatabase(this, new List<ArzRecord>()));
        }

        public override bool CanInferFormat => false;

        public override bool CanReadFieldData => false;

        public override ArzFileFormat Format => default;

        internal override DataBuffer ReadRawFieldDataBuffer(int offset, int compressedSize, int decompressedSize)
        {
            throw Error.NotSupported();
        }

        internal override byte[] DecodeRawFieldData(Span<byte> source, int decompressedSize)
        {
            throw Error.NotSupported();
        }

        internal override byte[] ReadFieldData(int offset, int compressedSize, int decompressedSize)
        {
            throw Error.NotSupported();
        }
    }
}
