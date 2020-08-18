using System;
using System.Collections.Generic;
using Glacie.Data.Arz.Infrastructure;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    // TODO: (Low) (MutatingRecordApi Tests) - rename, and review.
    // TODO: (Low) (MutatingRecordApi Tests) - check what version keep increasing and/or modified flag are applied.
    // TODO: (Low) (MutatingRecordApi Tests) - create fuzzy/random mutations and check results with simulated expected results. Also good for benchmarking.

    public sealed class MutatingRecordApi : IDisposable
    {
        private readonly ArzDatabase _database;
        private ArzRecord Record0 { get; }
        private ArzRecord Record1 { get; }
        private ArzRecord Record2 { get; }

        public MutatingRecordApi()
        {
            _database = ArzDatabase.Open(TestData.GtdTqae2);
            Record0 = _database[TestData.GtdTqae2RawRecordNames[0]];
            Record1 = _database[TestData.GtdTqae2RawRecordNames[1]];
            Record2 = _database[TestData.GtdTqae2RawRecordNames[2]];
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        // TODO: set values by type

        [Fact]
        public void SetNewField()
        {
            var fieldName = "--gx-field";
            var record = Record1;

            VerifyFieldIsNotExist(record, fieldName);

            var countBefore = record.Count;

            record.Set(fieldName, 123);

            Assert.Equal(countBefore + 1, record.Count);

            VerifyFieldIsExist(record, fieldName);
            VerifyInvariants(record);

            var r = record.Get(fieldName);
            Assert.Equal(ArzValueType.Integer, r.ValueType);
            Assert.Equal(1, r.Count);
            Assert.Equal(123, r.Get<int>());
        }

        [Fact]
        public void SetNewFieldAndOverwrite()
        {
            var fieldName = "--gx-field";
            var record = Record1;

            VerifyFieldIsNotExist(record, fieldName);

            var countBefore = record.Count;

            record.Set(fieldName, 123);
            record.Set(fieldName, 456);

            Assert.Equal(countBefore + 1, record.Count);

            VerifyFieldIsExist(record, fieldName);
            VerifyInvariants(record);

            var f = record.Get(fieldName);
            Assert.Equal(ArzValueType.Integer, f.ValueType);
            Assert.Equal(1, f.Count);
            Assert.Equal(456, f.Get<int>());
        }

        [Fact]
        public void SetNewFieldAndOverwriteInNewDatabase()
        {
            using var database = ArzDatabase.Create();

            var fieldName = "--gx-field";
            var record = database.Add("my/record");

            VerifyFieldIsNotExist(record, fieldName);

            var countBefore = record.Count;

            record.Set(fieldName, 123);
            record.Set(fieldName, 456);

            Assert.Equal(countBefore + 1, record.Count);

            VerifyFieldIsExist(record, fieldName);
            VerifyInvariants(record);

            var f = record.Get(fieldName);
            Assert.Equal(ArzValueType.Integer, f.ValueType);
            Assert.Equal(1, f.Count);
            Assert.Equal(456, f.Get<int>());
        }

        [Fact]
        public void RemoveNonExisting()
        {
            var record = Record1;
            var recordMetrics = (IArzRecordMetrics)record;

            var beforeFieldCount = record.Count;
            var beforeRemovedFieldCount = recordMetrics.NumberOfRemovedFields;

            Assert.False(record.Remove(Guid.NewGuid().ToString("N")));

            Assert.Equal(beforeFieldCount, record.Count);
            Assert.Equal(beforeRemovedFieldCount, recordMetrics.NumberOfRemovedFields);

            VerifyInvariants(record);
        }

        [Fact]
        public void RemoveExisting()
        {
            var record = Record1;
            var recordMetrics = (IArzRecordMetrics)record;

            var beforeFieldCount = record.Count;
            var beforeRemovedFieldCount = recordMetrics.NumberOfRemovedFields;

            Assert.True(record.Remove("templateName"));
            Assert.False(record.Remove("templateName"));

            Assert.Equal(beforeFieldCount - 1, record.Count);
            Assert.Equal(beforeRemovedFieldCount + 1, recordMetrics.NumberOfRemovedFields);

            VerifyFieldIsNotExist(record, "templateName");
            VerifyInvariants(record);
        }

        [Fact]
        public void EnumerationBreaksWhenFieldAdded()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var _ in Record1.GetAll())
                {
                    Record1.Set("--gx-field", 123);
                }
            });
        }

        [Fact]
        public void EnumerationBreaksWhenRecordRemoved()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var x in Record1.GetAll())
                {
                    Record1.Remove(x.Name);
                }
            });
        }

        private void VerifyFieldIsNotExist(ArzRecord record, string fieldName)
        {
            foreach (var f in record.GetAll())
            {
                Assert.NotEqual(fieldName, f.Name);
            }

            var result = record.TryGet(fieldName, out var field);
            Assert.False(result);
        }

        private void VerifyFieldIsExist(ArzRecord record, string fieldName)
        {
            bool found = false;
            foreach (var f in record.GetAll())
            {
                if (fieldName == f.Name)
                {
                    Assert.False(found, "Found field with same name twice.");
                    found = true;
                }
            }
            Assert.True(found);

            var result = record.TryGet(fieldName, out var _);
            Assert.True(result);
        }

        private void VerifyInvariants(ArzRecord record)
        {
            // Count.
            var count = 0;
            foreach (var _ in record.GetAll()) count++;
            Assert.True(record.Count == count, "Record iterates more fields than reports by Count property.");

            // All fields has unique names.
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var x in record.GetAll())
            {
                var wasAdded = fieldNames.Add(x.Name);
                if (!wasAdded)
                {
                    Assert.True(wasAdded, $"Record has duplicate field \"{x.Name}\".");
                }
            }
        }
    }
}
