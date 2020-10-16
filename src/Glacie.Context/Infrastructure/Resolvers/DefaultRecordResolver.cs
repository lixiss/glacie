using System.Collections.Generic;

using Glacie.Abstractions;

namespace Glacie.Infrastructure.Resolvers
{
    public class DefaultRecordResolver : RecordResolver
    {
        private RecordProvider _recordProvider;
        private bool _disposeRecordProvider;

        public DefaultRecordResolver(RecordProvider recordProvider, bool disposeRecordProvider = false)
        {
            _recordProvider = recordProvider;
            _disposeRecordProvider = disposeRecordProvider;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposeRecordProvider)
                {
                    _recordProvider?.Dispose();
                    _recordProvider = null!;
                }
            }
            base.Dispose(disposing);
        }

        public override IEnumerable<Record> SelectAll()
        {
            return _recordProvider.SelectAll();
        }

        public override Resolution<Record> ResolveRecord(Path path)
        {
            if (_recordProvider.TryGetRecord(path, out var result))
            {
                return new Resolution<Record>(result, resolved: true);
            }
            else return default;
        }
    }
}
