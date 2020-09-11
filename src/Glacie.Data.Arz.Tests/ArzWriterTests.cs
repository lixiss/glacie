using System;
using System.Collections.Generic;
using System.IO;
using Glacie.Data.Arz.Infrastructure;
using Glacie.Data.Arz.Utilities;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    [Trait("Category", "ARZ")]
    public sealed class ArzWriterTests
    {
        private static readonly ArzFileFormat SomeValidFormat = ArzFileFormat.TitanQuest;

        [Fact]
        public void ThrowsIfFormatIsNotInferred()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var ex = Assert.Throws<ArzException>(() => ArzWriter.Write(outputStream, database));
            Assert.Equal("FileFormatRequired", ex.ErrorCode);
        }

        [Fact]
        public void ThrowsIfFormatIsNotInferredFromFile()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqgd0);
            using var outputStream = new MemoryStream();
            var ex = Assert.Throws<ArzException>(() => ArzWriter.Write(outputStream, database));
            Assert.Equal("FileFormatRequired", ex.ErrorCode);
        }

        [Fact]
        public void ThrowsIfLayoutIsIncomplete()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = ArzFileFormat.Automatic
            };
            var ex = Assert.Throws<ArzException>(
                () => ArzWriter.Write(outputStream, database, options)
                );
            Assert.Equal("FileFormatRequired", ex.ErrorCode);
        }

        // This tests disabled, because there is no more way provide invalid formats.
        //[Fact]
        //public void ThrowsIfLayoutIsAmbigious()
        //{
        //    using var database = ArzDatabase.Create();
        //    using var outputStream = new MemoryStream();
        //    var options = new ArzWriterOptions
        //    {
        //        Format = ArzFileLayout.UseLz4Compression | ArzFileLayout.UseZlibCompression,
        //    };
        //    var ex = Assert.Throws<ArzException>(
        //        () => ArzWriter.Write(outputStream, database, options)
        //        );
        //    Assert.Equal("InvalidLayout", ex.ErrorCode);
        //}

        //[Fact]
        //public void ThrowsIfLayoutIsLz4WithoutDecompressedSize()
        //{
        //    using var database = ArzDatabase.Create();
        //    using var outputStream = new MemoryStream();
        //    var options = new ArzWriterOptions
        //    {
        //        Format = ArzFileLayout.UseLz4Compression
        //    };
        //    var ex = Assert.Throws<ArzException>(
        //        () => ArzWriter.Write(outputStream, database, options)
        //        );
        //    Assert.Equal("InvalidLayout", ex.ErrorCode);
        //}

        [Fact]
        public void EmptyWithGdLayout()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = ArzFileFormat.GrimDawn,
                ComputeChecksum = true,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);
        }

        [Fact]
        public void EmptyWithTqaeLayout()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = ArzFileFormat.TitanQuestAnniversaryEdition,
                ComputeChecksum = true,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);
        }

        [Fact]
        public void EmptyWithTqLayout()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = ArzFileFormat.TitanQuest,
                ComputeChecksum = true,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);
        }

        [Fact]
        public void AutomaticallyInferLayoutFromSourceRecordsWhenPossible()
        {
            // This is not strictly needed, but this makes boilerplate code to work.

            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = ArzReadingMode.Lazy });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                ComputeChecksum = true,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);
        }

        [Fact]
        public void NewRecordNoFields()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
            };

            // Presense of record without fields, should be considered as logical error,
            // so this is failure. If code remove fields and ends with record without
            // fields, it may remove record as well.
            database.Add("some-record", "some-class");

            var ex = Assert.Throws<ArzException>(
                () => ArzWriter.Write(outputStream, database, options)
            );
            Assert.Equal("RecordHasNoAnyField", ex.ErrorCode);
            Assert.Equal(0, outputStream.Length);
        }

        [Fact]
        public void NewRecordAllFieldRemoved()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
            };

            // Presense of record without fields, should be considered as logical error,
            // so this is failure. If code remove fields and ends with record without
            // fields, it may remove record as well.
            var record = database.Add("some-record", "some-class");
            record["field-1"] = 1;
            record["field-1"] = 2;
            record.Remove("field-1");
            record.Remove("field-2");

            var ex = Assert.Throws<ArzException>(
                () => ArzWriter.Write(outputStream, database, options)
            );
            Assert.Equal("RecordHasNoAnyField", ex.ErrorCode);
            Assert.Equal(0, outputStream.Length);
        }

        [Fact]
        public void NewRecordNoClass()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                InferRecordClass = false,
            };

            // Presense of record without class,
            // also should be considered as logical error,
            // so this is failure.
            var record = database.Add("some-record", @class: null);
            record["some-field"] = 123;

            var ex = Assert.Throws<ArzException>(
                () => ArzWriter.Write(outputStream, database, options)
            );
            Assert.Equal("RecordHasNoClass", ex.ErrorCode);
            Assert.Equal(0, outputStream.Length);
        }

        [Fact]
        public void NewRecordNoClassWhenNoInfer()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                InferRecordClass = false,
            };

            // Record without class even with field "Class" defined
            // when no infer enabled - should generate error.
            var record = database.Add("some-record", @class: null);
            record["some-field"] = 123;
            record["Class"] = "some-class";

            var ex = Assert.Throws<ArzException>(
                () => ArzWriter.Write(outputStream, database, options)
            );
            Assert.Equal("RecordHasNoClass", ex.ErrorCode);
            Assert.Equal(0, outputStream.Length);
        }

        [Fact]
        public void NewRecord()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
            };

            var record = database.Add("some-record", "some-class");
            record["some-field"] = 123;

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            AssertRecordEqual(record, oRecord);
        }

        [Fact]
        public void NewRecordInferRecordClass()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
                InferRecordClass = true,
            };

            var record = database.Add("some-record", null);
            record["some-field"] = 123;
            record["some-field-2"] = "some-my-field";
            record["Class"] = "some-class";

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            Assert.Equal("some-class", oRecord.Class);
            AssertRecordEqual(record, oRecord, ignoreClass: true);
        }

        [Fact]
        public void NewRecordWithRemovedFields()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
            };

            // Remove field after other existing field
            // require to compact field data block, so ensure
            // what this is work.
            var record = database.Add("some-record", "some-class");
            record["some-field"] = 123;
            record["some-field-2"] = new[] { "some-my-field-value", "some-my-field-value-2" };
            record.Remove("some-field");

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            AssertRecordEqual(record, oRecord);
        }

        [Fact]
        public void NewRecordWithLargeRemovedAreas()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
            };

            var record = database.Add("some-record", "some-class");
            for (var i = 0; i < 11; i++)
            {
                record[MakeFieldName(i)] = MakeValueArray(1);
            }

            record.Remove(MakeFieldName(0));
            record.Remove(MakeFieldName(1));
            // 2 - alive
            record.Remove(MakeFieldName(3));
            record.Remove(MakeFieldName(4));
            // 5 - alive
            record.Remove(MakeFieldName(6));
            record.Remove(MakeFieldName(7));
            // 8 - alive
            record.Remove(MakeFieldName(9));
            record.Remove(MakeFieldName(10));

            var rm = (IArzRecordMetrics)record;
            Assert.Equal(8, rm.NumberOfRemovedFields);

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            AssertRecordEqual(record, oRecord);

            string MakeFieldName(int index) => "some-field-" + index;

            int[] MakeValueArray(int length)
            {
                var result = new int[length];
                for (var i = 0; i < length; i++)
                {
                    result[i] = 100 + i;
                }
                return result;
            }
        }

        [Fact]
        public void NewRecordWithSmallRemovedAreas()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
            };

            var record = database.Add("some-record", "some-class");
            for (var i = 0; i < 11; i++)
            {
                record[MakeFieldName(i)] = MakeValueArray(1);
            }

            record.Remove(MakeFieldName(2));
            record.Remove(MakeFieldName(5));
            record.Remove(MakeFieldName(8));

            var rm = (IArzRecordMetrics)record;
            Assert.Equal(3, rm.NumberOfRemovedFields);

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            AssertRecordEqual(record, oRecord);

            string MakeFieldName(int index) => "some-field-" + index;

            int[] MakeValueArray(int length)
            {
                var result = new int[length];
                for (var i = 0; i < length; i++)
                {
                    result[i] = 100 + i;
                }
                return result;
            }
        }

        [Fact]
        public void NewRecordWithManyRemovals()
        {
            using var database = ArzDatabase.Create();
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = SomeValidFormat,
                ComputeChecksum = true,
            };

            var record = database.Add("some-record", "some-class");
            for (var i = 0; i < 10000; i++)
            {
                record[MakeFieldName(i)] = MakeValueArray(1 + i % 10);
            }

            // remove by some strange pattern
            for (var i = 0; i < 10000; i++)
            {
                if (i < 100 && i % 2 == 0) record.Remove(MakeFieldName(i));
                else if (i < 400 && (i >> 2) % 2 == 0) record.Remove(MakeFieldName(i));
                else if (i % 3 == 0) record.Remove(MakeFieldName(i));
            }

            var rm = (IArzRecordMetrics)record;
            if (rm.NumberOfRemovedFields < 3000) throw Error.InvalidOperation();

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            var oRecord = oDatabase["some-record"];
            AssertRecordEqual(record, oRecord);

            string MakeFieldName(int index) => "some-field-" + index;

            int[] MakeValueArray(int length)
            {
                var result = new int[length];
                for (var i = 0; i < length; i++)
                {
                    result[i] = 100 + i;
                }
                return result;
            }
        }

        /// <summary>
        /// Ensures what writer able to write existing database opened in any mode.
        /// </summary>
        [Theory]
        [InlineData(ArzReadingMode.Lazy, false)]
        [InlineData(ArzReadingMode.Raw, false)]
        [InlineData(ArzReadingMode.Full, false)]
        [InlineData(ArzReadingMode.Lazy, true)]
        [InlineData(ArzReadingMode.Raw, true)]
        [InlineData(ArzReadingMode.Full, true)]
        public void WriteFromExisting(ArzReadingMode readingMode, bool forceCompression)
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = readingMode });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                Format = ArzFileFormat.TitanQuestAnniversaryEdition,
                ComputeChecksum = true,
                InferRecordClass = false,
                ForceCompression = forceCompression,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            AssertDatabaseEqual(database, oDatabase);
        }

        [Theory]
        [InlineData(ArzReadingMode.Lazy, false, "tqae")]
        [InlineData(ArzReadingMode.Lazy, false, "tq")]
        [InlineData(ArzReadingMode.Lazy, false, "gd")]
        [InlineData(ArzReadingMode.Raw, false, "tqae")]
        [InlineData(ArzReadingMode.Raw, false, "tq")]
        [InlineData(ArzReadingMode.Raw, false, "gd")]
        [InlineData(ArzReadingMode.Full, false, "tqae")]
        [InlineData(ArzReadingMode.Full, false, "tq")]
        [InlineData(ArzReadingMode.Full, false, "gd")]
        [InlineData(ArzReadingMode.Lazy, true, "tqae")]
        [InlineData(ArzReadingMode.Lazy, true, "tq")]
        [InlineData(ArzReadingMode.Lazy, true, "gd")]
        [InlineData(ArzReadingMode.Raw, true, "tqae")]
        [InlineData(ArzReadingMode.Raw, true, "tq")]
        [InlineData(ArzReadingMode.Raw, true, "gd")]
        [InlineData(ArzReadingMode.Full, true, "tqae")]
        [InlineData(ArzReadingMode.Full, true, "tq")]
        [InlineData(ArzReadingMode.Full, true, "gd")]
        public void WriteFromExistingToAnyLayout(ArzReadingMode readingMode, bool forceCompression, string targetFormat)
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = readingMode });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                ComputeChecksum = true,
                InferRecordClass = false,
                Format = ArzFileFormat.Parse(targetFormat),
                RebuildStringTable = false,
                ForceCompression = forceCompression,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            AssertDatabaseEqual(database, oDatabase);
        }

        [Theory]
        [InlineData(ArzReadingMode.Lazy, false, "tqae")]
        [InlineData(ArzReadingMode.Lazy, false, "tq")]
        [InlineData(ArzReadingMode.Lazy, false, "gd")]
        [InlineData(ArzReadingMode.Raw, false, "tqae")]
        [InlineData(ArzReadingMode.Raw, false, "tq")]
        [InlineData(ArzReadingMode.Raw, false, "gd")]
        [InlineData(ArzReadingMode.Full, false, "tqae")]
        [InlineData(ArzReadingMode.Full, false, "tq")]
        [InlineData(ArzReadingMode.Full, false, "gd")]
        [InlineData(ArzReadingMode.Lazy, true, "tqae")]
        [InlineData(ArzReadingMode.Lazy, true, "tq")]
        [InlineData(ArzReadingMode.Lazy, true, "gd")]
        [InlineData(ArzReadingMode.Raw, true, "tqae")]
        [InlineData(ArzReadingMode.Raw, true, "tq")]
        [InlineData(ArzReadingMode.Raw, true, "gd")]
        [InlineData(ArzReadingMode.Full, true, "tqae")]
        [InlineData(ArzReadingMode.Full, true, "tq")]
        [InlineData(ArzReadingMode.Full, true, "gd")]
        public void WriteFromExistingToAnyLayoutOptimized(ArzReadingMode readingMode, bool forceCompression, string targetFormat)
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = readingMode });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                ComputeChecksum = true,
                InferRecordClass = false,
                Format = ArzFileFormat.Parse(targetFormat),
                RebuildStringTable = true,
                ForceCompression = forceCompression,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            AssertDatabaseEqual(database, oDatabase);
        }

        [Fact]
        public void WriteChangesOnly0()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = ArzReadingMode.Lazy });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                ComputeChecksum = true,
                Format = ArzFileFormat.TitanQuestAnniversaryEdition,
                ChangesOnly = true,
                RebuildStringTable = true,
            };
            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            Assert.Equal(0, oDatabase.Count);
            // AssertDatabaseEqual(database, oDatabase);
        }

        [Fact]
        public void WriteChangesOnly1()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2,
                new ArzReaderOptions { Mode = ArzReadingMode.Lazy });
            using var outputStream = new MemoryStream();
            var options = new ArzWriterOptions
            {
                ComputeChecksum = true,
                Format = ArzFileFormat.TitanQuestAnniversaryEdition,
                ChangesOnly = true,
                RebuildStringTable = true,
            };
            database[TestData.GtdTqae2RawRecordNames[0]]["--gx-test"] = true;

            ArzWriter.Write(outputStream, database, options);
            ArzVerifier.Verify(outputStream);

            using var oDatabase = ArzDatabase.Open(outputStream);
            Assert.Equal(1, oDatabase.Count);
            var r = oDatabase.Get(TestData.GtdTqae2RawRecordNames[0]);
            Assert.Equal(TestData.GtdTqae2RawRecordNames[0], r.Name);
        }

        // TODO: (Low) (ArzWriterTests) Move AssertDatabaseEqual, AssertRecordEqual into shared helpers - they are useful. Also not bad good case for benchmarking.

        private void AssertDatabaseEqual(ArzDatabase expectedDatabase, ArzDatabase actualDatabase)
        {
            Assert.Equal(expectedDatabase.Count, actualDatabase.Count);

            foreach (var expectedRecord in expectedDatabase.GetAll())
            {
                var actualRecord = actualDatabase[expectedRecord.Name];
                AssertRecordEqual(expectedRecord, actualRecord);
            }
        }

        private void AssertRecordEqual(ArzRecord expectedRecord, ArzRecord actualRecord, bool ignoreClass = false)
        {
            Assert.Equal(expectedRecord.Name, actualRecord.Name);
            if (!ignoreClass) Assert.Equal(expectedRecord.Class, actualRecord.Class);
            Assert.Equal(expectedRecord.Count, actualRecord.Count);

            foreach (var expectedField in expectedRecord.GetAll())
            {
                var actualField = actualRecord.Get(expectedField.Name);

                Assert.Equal(expectedField.Count, actualField.Count);
                Assert.Equal(expectedField.ValueType, actualField.ValueType);

                switch (expectedField.ValueType)
                {
                    case ArzValueType.Integer:
                        for (var i = 0; i < expectedField.Count; i++)
                        {
                            Assert.Equal(expectedField.Get<int>(i), actualField.Get<int>(i));
                        }
                        break;

                    case ArzValueType.Real:
                        for (var i = 0; i < expectedField.Count; i++)
                        {
                            Assert.Equal(expectedField.Get<float>(i), actualField.Get<float>(i));
                        }
                        break;

                    case ArzValueType.Boolean:
                        for (var i = 0; i < expectedField.Count; i++)
                        {
                            Assert.Equal(expectedField.Get<bool>(i), actualField.Get<bool>(i));
                        }
                        break;

                    case ArzValueType.String:
                        for (var i = 0; i < expectedField.Count; i++)
                        {
                            Assert.Equal(expectedField.Get<string>(i), actualField.Get<string>(i));
                        }
                        break;

                    default:
                        throw Error.Unreachable();
                }
            }
        }
    }
}
