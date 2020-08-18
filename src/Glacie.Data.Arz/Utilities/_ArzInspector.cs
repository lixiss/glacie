using System;
using System.Collections.Generic;

namespace Glacie.Data.Arz.Utilities
{
    // TODO: (High) (ArzInspector) Implement. It should show various properties of arz file.

    internal sealed class _ArzInspector
    {
        private void SomeMethod()
        {
            var arzRecords = new List<ArzRecord>();

            // TODO: available for record count > 0

            var numberOfOutOfOrderRecords = GetNumberOfRequiredSeekOperationsForRecords(arzRecords);
            if (numberOfOutOfOrderRecords > 0)
            {
                Console.WriteLine("Records are not ordered by data offset.");
                Console.WriteLine("# of out required seeks = {0} (1 is best value)", numberOfOutOfOrderRecords);
                Console.WriteLine("           record count = {0}", arzRecords.Count);
                Console.WriteLine("             % of seeks = {0:N2}%", numberOfOutOfOrderRecords * 100.0 / arzRecords.Count);
            }
        }

        private int GetNumberOfRequiredSeekOperationsForRecords(List<ArzRecord> records)
        {
            var numberOfRequiredSeeks = 0;

            var currentPosition = 0;
            foreach (var record in records)
            {
                if (currentPosition != record.DataOffset)
                {
                    numberOfRequiredSeeks++;
                }

                currentPosition = record.DataOffset + record.DataSize;
            }

            return numberOfRequiredSeeks;
        }
    }
}
