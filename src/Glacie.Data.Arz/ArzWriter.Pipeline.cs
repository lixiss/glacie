using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Glacie.Buffers;
using Glacie.Data.Arz.FileFormat;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Data.Arz
{
    partial class ArzWriter
    {
        private void WriteRecordsSinglethreaded(List<ArzRecord> records, IIncrementalProgress<int>? progress)
        {
            foreach (var record in records)
            {
                if (TryProcessRecord(record, out var encoderJob))
                {
                    EncodeRecord(ref encoderJob, ref _encoder, out var writerJob);
                    WriteRecord(ref writerJob);
                }

                progress?.AddValue(1);
            }
        }

        private void WriteRecordsMultithreaded(List<ArzRecord> records, int effectiveDegreeOfParallelism, IIncrementalProgress<int>? progress)
        {
            Check.True(effectiveDegreeOfParallelism > 0);

            var numberOfConsumers = effectiveDegreeOfParallelism;
            var encoderQueueLength = effectiveDegreeOfParallelism * 4;
            var writerQueueLength = effectiveDegreeOfParallelism * 4;

            using var encoderQueue = new BlockingCollection<EncodeRecordJob>(encoderQueueLength);
            using var writerQueue = new BlockingCollection<WriteRecordJob>(writerQueueLength);
            var encoderTasks = new Task[numberOfConsumers];
            Task writerTask;
            var cts = new CancellationTokenSource();

            // TODO: (Medium) ArzWriter.Pipeline is wrong:
            // EffectiveDegreeOfParallelism is 1*CPU, but:
            // - 1 (main) thread waiting in producer, it is not async call, so it block
            // - DOPx encoders which practically blocking
            // - 1 (writer) task to write job, which practically blocking
            // Se we need minimum of (DOP + 2 threads), and still need thread pool free for other tasks.
            // All of them are blocking operations, and there is should be more
            // fair to use long-running tasks instead.

            // Writer
            {
                var ct = cts.Token;
                writerTask = Task.Run(() =>
                {
                    while (!writerQueue.IsCompleted)
                    {
                        while (writerQueue.TryTake(out var x, Timeout.Infinite, ct))
                        {
                            WriteRecord(ref x);
                            progress?.AddValue(1);
                        }
                    }
                }, ct);
                writerTask.ContinueWith((x) => cts.Cancel(), TaskContinuationOptions.OnlyOnFaulted);
            }

            // Consumers
            for (var i = 0; i < numberOfConsumers; i++)
            {
                var ct = cts.Token;
                var consumerTask = Task.Run(() =>
                {
                    Encoder? encoder = null;
                    try
                    {
                        while (!encoderQueue.IsCompleted)
                        {
                            while (encoderQueue.TryTake(out var x, Timeout.Infinite, ct))
                            {
                                EncodeRecord(ref x, ref encoder, out var job2);
                                writerQueue.Add(job2);
                            }
                        }
                    }
                    finally
                    {
                        encoder?.Dispose();
                    }
                }, ct);
                consumerTask.ContinueWith((x) => cts.Cancel(), TaskContinuationOptions.OnlyOnFaulted);
                encoderTasks[i] = consumerTask;
            }

            // Producer
            bool success;
            try
            {
                foreach (var record in records)
                {
                    if (TryProcessRecord(record, out var job))
                    {
                        encoderQueue.Add(job, cts.Token);
                    }
                }
                encoderQueue.CompleteAdding();
                success = true;
            }
            catch (OperationCanceledException)
            {
                // If consumer throw any excetion, it will cancel token
                // which may result in this exception. 
                success = false;
            }

            Task.WaitAll(encoderTasks);

            writerQueue.CompleteAdding();
            writerTask.Wait();

            Check.True(success && encoderQueue.IsCompleted && writerQueue.IsCompleted);
        }

        private bool TryProcessRecord(ArzRecord record, out EncodeRecordJob job)
        {
            Check.True(record.ClassId > 0);
            Check.True(record.Any());

            // TODO: (VeryLow) (ArzWriter) This checks actually no more needed.
            var isModified = record.IsNew || record.IsModified || record.IsDataModified;
            if (_changesOnly && !isModified)
            {
                job = default;
                return false;
            }

            if (_afStringTableIsCompatibleWithRawFieldData)
            {
                // 1. Record Has No Data (should be readed)
                // 2. Record Has Raw Data (get buffer and write)
                // 3. Record Has Data
                //   IsNew
                // +4.  encode -> encode
                //   Is From File
                // +5.  is modified -> encode
                // 6.  is not modified -> read raw data from file and write

                if (_forceCompression || record.IsDataModified || record.IsNew)
                {
                    var fieldData = record.GetFieldDataBuffer(loadFieldData: _forceCompression);
                    Check.True(fieldData.Length > 0);
                    job = new EncodeRecordJob
                    {
                        Record = record,
                        FieldData = fieldData,
                        DecompressedSize = 0,
                        NameId = record.NameId,
                        Compress = true,
                    };
                    return true;
                }
                else
                {
                    DebugCheck.True(!_forceCompression);

                    if (record.HasNoFieldData || record.HasFieldData)
                    {
                        Check.That(!record.IsNew);

                        if (_contextCanReadFieldData)
                        {
                            var buffer = _context.ReadRawFieldDataBuffer(
                                record.DataOffset,
                                record.DataSize,
                                record.DataSizeDecompressed);

                            job = new EncodeRecordJob
                            {
                                Record = record,
                                FieldData = buffer,
                                DecompressedSize = record.DataSizeDecompressed,
                                NameId = record.NameId,
                                Compress = false,
                            };
                            return true;
                        }
                        else
                        {
                            var fieldData = record.GetFieldDataBuffer(loadFieldData: false);
                            Check.True(fieldData.Length > 0);
                            job = new EncodeRecordJob
                            {
                                Record = record,
                                FieldData = fieldData,
                                DecompressedSize = 0,
                                NameId = record.NameId,
                                Compress = true,
                            };
                            return true;
                        }
                    }
                    else if (record.HasRawFieldData)
                    {
                        Check.That(!record.IsNew);

                        var buffer = record.GetRawFieldDataBuffer();

                        job = new EncodeRecordJob
                        {
                            Record = record,
                            FieldData = buffer,
                            DecompressedSize = record.DataSizeDecompressed,
                            NameId = record.NameId,
                            Compress = false,
                        };
                        return true;
                    }
                    else throw Error.Unreachable();
                }
            }
            else
            {
                DebugCheck.True(_afStringEncoder != null);

                // TODO: (VeryLow) (ArzWriter) We can make string encoder and ArzRecord::EncodeFieldDataBuffer
                // thread safe and do this as EncoderJob.
                var recordNameId = _afStringEncoder.Encode(record.NameId);
                var buffer = record.EncodeFieldDataBuffer(_afStringEncoder, pool: true);

                job = new EncodeRecordJob
                {
                    Record = record,
                    FieldData = buffer,
                    DecompressedSize = buffer.Length,
                    NameId = recordNameId,
                    Compress = true,
                };
                return true;
            }
        }

        private void EncodeRecord(ref EncodeRecordJob j, ref Encoder? encoder, out WriteRecordJob writerJob)
        {
            long timestamp;
            if (!j.Record.HasExplicitTimestamp &&
                (j.Record.IsDataModified || j.Record.IsModified))
            {
                timestamp = _modifiedTimestamp;
            }
            else
            {
                timestamp = j.Record.Timestamp;
            }

            if (j.Compress)
            {
                if (encoder == null) encoder = CreateEncoder();
                var encodedFieldData = encoder.EncodeToBuffer(j.FieldData.Span);
                var decompressedSize = j.FieldData.Length;
                j.FieldData.Return();

                writerJob = new WriteRecordJob
                {
                    Record = j.Record,
                    FieldData = encodedFieldData,
                    DecompressedSize = decompressedSize,
                    NameId = j.NameId,
                    ClassId = j.Record.ClassId,
                    Timestamp = timestamp,
                };
            }
            else
            {
                writerJob = new WriteRecordJob
                {
                    Record = j.Record,
                    FieldData = j.FieldData,
                    DecompressedSize = j.DecompressedSize,
                    NameId = j.NameId,
                    ClassId = j.Record.ClassId,
                    Timestamp = timestamp,
                };
            }
        }

        private void WriteRecord(ref WriteRecordJob j)
        {
            ref var afRecord = ref _afRecords[_afRecordIndex];
            afRecord = new ArzFileRecord
            {
                NameId = j.NameId,
                ClassId = j.ClassId,
                DataOffset = _afRecordDataOffset,
                DataSize = j.FieldData.Length,
                DataSizeDecompressed = j.DecompressedSize,
                Timestamp = j.Timestamp,
            };

            _afRecordIndex++;
            _afRecordDataOffset += j.FieldData.Length;

            _afStream.Write(j.FieldData.Span);
            j.FieldData.Return();
        }

        private struct EncodeRecordJob
        {
            // TODO: (VeryLow) (ArzWriter) Use ctor for EncodeRecordJob.

            public ArzRecord Record;
            public DataBuffer FieldData;
            public int DecompressedSize;
            public arz_string_id NameId;
            public bool Compress;
        }

        private struct WriteRecordJob
        {
            // TODO: (VeryLow) (ArzWriter) Use ctor for WriteRecordJob.

            public ArzRecord Record;
            public DataBuffer FieldData;
            public int DecompressedSize;
            public arz_string_id NameId;
            public int ClassId;
            public long Timestamp;
        }
    }
}
