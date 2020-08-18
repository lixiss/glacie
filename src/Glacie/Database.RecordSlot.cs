using Glacie.Data.Arz;

namespace Glacie
{
    partial class Database
    {
        private struct RecordSlot
        {
            public ArzRecord Record;
            public bool IsTarget;

            public RecordSlot(ArzRecord record, bool isTarget)
            {
                Record = record;
                IsTarget = isTarget;
            }
        }
    }
}
