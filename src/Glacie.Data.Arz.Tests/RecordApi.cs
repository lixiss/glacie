using System;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    // TODO: (Low) (RecordApi Tests) - rename, and review.

    [Trait("Category", "ARZ")]
    public sealed class RecordApi : IDisposable
    {
        private readonly ArzDatabase _database;
        private ArzRecord Record0 { get; }
        private ArzRecord Record1 { get; }
        private ArzRecord Record2 { get; }

        public RecordApi()
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

        [Fact]
        public void Name()
        {
            Assert.Equal(TestData.GtdTqae2RawRecordNames[0], Record0.Name);
            Assert.Equal(TestData.GtdTqae2RawRecordNames[1], Record1.Name);
            Assert.Equal(TestData.GtdTqae2RawRecordNames[2], Record2.Name);
        }

        [Fact]
        public void Class()
        {
            Assert.Equal(string.Empty, Record0.Class);
            Assert.Equal("Player", Record1.Class);
            Assert.Equal(string.Empty, Record2.Class);
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(235, Record0.Count);
            Assert.Equal(1174, Record1.Count);
            Assert.Equal(13, Record2.Count);
        }

        [Fact]
        public void FieldEnumeration()
        {
            var count = 0;
            foreach (var _ in Record0.GetAll()) count++;
            Assert.Equal(235, count);

            count = 0;
            foreach (var _ in Record1.GetAll()) count++;
            Assert.Equal(1174, count);

            count = 0;
            foreach (var _ in Record2.GetAll()) count++;
            Assert.Equal(13, count);
        }

        [Fact]
        public void InitialInvariants()
        {
            VerifyInitialInvariants(Record0);
            VerifyInitialInvariants(Record1);
            VerifyInitialInvariants(Record2);
        }

        [Fact]
        public void GetByName()
        {
            var field = Record1.Get("Class");
            Assert.Equal("Class", field.Name);
        }

        [Fact]
        public void GetByNameNonExistent()
        {
            var nonExistentFieldName = Guid.NewGuid().ToString("N");
            var ex = Assert.Throws<ArzException>(
                () => Record1.Get(nonExistentFieldName)
                );
            Assert.Equal("FieldNotFound", ex.ErrorCode);
        }

        [Fact]
        public void TryGetByName()
        {
            var found = Record1.TryGet("Class", out var field);
            Assert.True(found);
            Assert.Equal("Class", field.Name);
        }

        [Fact]
        public void TryGetByNameNonExistent()
        {
            var nonExistentFieldName = Guid.NewGuid().ToString("N");
            var found = Record1.TryGet(nonExistentFieldName, out var field);
            Assert.False(found);
            Assert.Equal(default, field);
        }

        private void VerifyInitialInvariants(ArzRecord record)
        {
            var count = 0;
            foreach (var _ in record.GetAll()) count++;
            Assert.True(record.Count == count);
        }
    }
}
