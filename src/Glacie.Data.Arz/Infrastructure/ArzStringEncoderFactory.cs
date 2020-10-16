using System;
using System.Collections.Generic;

namespace Glacie.Data.Arz.Infrastructure
{
    internal sealed class ArzStringEncoderFactory : IArzStringEncoderFactory
    {
        public static readonly ArzStringEncoderFactory Default = new ArzStringEncoderFactory();

        private readonly bool fillStringEncoder = false;

        private ArzStringEncoderFactory() { }

        public ArzStringEncoder Create(ArzDatabase database, List<ArzRecord> records)
        {
            var sourceStringTable = database.Context.StringTable;
            var targetStringTable = new ArzStringTable(sourceStringTable.Count);
            var stringEncoder = new ArzStringEncoder(sourceStringTable, targetStringTable);

            var groupKeyToIndex = new Dictionary<int, int>();

            foreach (var record in records)
            {
                var groupId = GetGroupId(record);
                if (groupKeyToIndex.TryGetValue(groupId, out var v))
                {
                    groupKeyToIndex[groupId] = v + 1;
                }
                else
                {
                    groupKeyToIndex[groupId] = 1;
                }
            }

            records.Sort((a, b) =>
            {
                var wa = GetGroupWeight(a, groupKeyToIndex);
                var wb = GetGroupWeight(b, groupKeyToIndex);
                var result = wb - wa;
                if (result != 0) return result;

                result = b.Count - a.Count;
                if (result != 0) return result;

                return StringComparer.Ordinal.Compare(a.Name, b.Name);
            });

            // Go over records and fill string encoder with strings, below
            // this would allow to cancel re-encoding if result is same as source
            // table.
            if (fillStringEncoder)
            {
                foreach (var record in records)
                {
                    stringEncoder.Encode(record.NameId);

                    // TODO: this should be ArzRecord's method which iterate over
                    // field values and encode them.
                    foreach (var field in record.SelectAll())
                    {
                        stringEncoder.Encode(field.NameId);

                        if (field.ValueType == ArzValueType.String)
                        {
                            // TODO: this is inefficient, better to have direct support.
                            if (field.Count == 1)
                            {
                                var v = field.Get<string>();
                                stringEncoder.Encode(sourceStringTable[v]);
                            }
                            else
                            {
                                var fieldCount = field.Count;
                                for (var i = 0; i < fieldCount; i++)
                                {
                                    var v = field.Get<string>(i);
                                    stringEncoder.Encode(sourceStringTable[v]);
                                }
                            }
                        }
                    }
                }
            }

            return stringEncoder;
        }

        private static int GetGroupWeight(ArzRecord record, Dictionary<int, int> groupKeyToIndex)
        {
            var templateName = GetGroupId(record);
            if (groupKeyToIndex.TryGetValue(templateName, out var v))
            {
                return v;
            }
            return 0;
        }

        private static int GetGroupId(ArzRecord record)
        {
            if (record.TryGet(WellKnownFieldNames.TemplateName, ArzRecordOptions.NoFieldMap, out var templateField))
            {
                return templateField.GetRawValue(0);
            }
            return 0;
        }
    }
}
