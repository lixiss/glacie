using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Glacie.Buffers;
using Glacie.Data.Compression;

namespace Glacie.Data.Arz
{
    partial class ArzFileContext
    {
        /// <summary>
        /// Reads record data in their order (so typically it is sequential
        /// access), and decompress data in threads.
        /// </summary>
        /// <remarks>
        /// This done as single producer and multiple consumers: producer only
        /// read raw data into buffer (which is rented from pool), while
        /// consumers only decompress portion of data and return buffer back.
        /// Producer is limited (bounded) to how many items may wait to be
        /// consumed (or queued). Otherwise it may read too many data, as result
        /// allocate too many buffers, while there is attempt to keep balance
        /// between number of buffers, their reusing and CPU utilization.
        /// Note, that best results achieved with GC running in server mode.
        /// </remarks>
        private void ReadFieldDataMultithreaded(IEnumerable<ArzRecord> records, int effectiveDegreeOfParallelism)
        {
            Check.True(effectiveDegreeOfParallelism > 0);

            var numberOfConsumers = effectiveDegreeOfParallelism;
            var queueLength = effectiveDegreeOfParallelism * 4;

            using var blockingCollection = new BlockingCollection<(ArzRecord Record, DataBuffer Data)>(queueLength);
            var consumers = new Task[numberOfConsumers];
            var cts = new CancellationTokenSource();

            // Consumers
            for (var i = 0; i < numberOfConsumers; i++)
            {
                var ct = cts.Token;
                var consumerTask = Task.Run(() =>
                {
                    Decoder? decoder = null;
                    try
                    {
                        while (!blockingCollection.IsCompleted)
                        {
                            while (blockingCollection.TryTake(out var x, Timeout.Infinite, ct))
                            {
                                if (decoder == null)
                                {
                                    InferCompressionAlgorithm(x.Data.Span, x.Record.DataSizeDecompressed);
                                    decoder = CreateDecoder();
                                }
                                var data = decoder.Decode(x.Data.Span, x.Record.DataSizeDecompressed);
                                x.Data.Return();
                                x.Record.SetFieldDataCore(data);
                            }
                        }
                    }
                    finally
                    {
                        decoder?.Dispose();
                    }
                }, ct);
                consumerTask.ContinueWith((x) => cts.Cancel(), TaskContinuationOptions.OnlyOnFaulted);
                consumers[i] = consumerTask;
            }

            // Producer
            bool success;
            try
            {
                foreach (var record in records)
                {
                    var data = ReadRawFieldDataAsBuffer(record.DataOffset, record.DataSize, record.DataSizeDecompressed);
                    var fieldDataDecompressJob = (Record: record, Data: data);
                    blockingCollection.Add(fieldDataDecompressJob, cts.Token);
                }
                blockingCollection.CompleteAdding();
                success = true;
            }
            catch (OperationCanceledException)
            {
                // If consumer throw any excetion, it will cancel token
                // which may result in this exception. 
                success = false;
            }

            Task.WaitAll(consumers);
            Check.True(success && blockingCollection.IsCompleted);
        }
    }
}
