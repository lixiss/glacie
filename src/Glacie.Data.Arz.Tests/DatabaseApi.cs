using System;
using System.IO;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    // TODO: (Low) (DatabaseApi Tests) - rename, and review.

    [Trait("Category", "ARZ")]
    public sealed class DatabaseApi
    {
        #region Create

        [Fact]
        public void Create()
        {
            using var database = ArzDatabase.Create();
        }

        #endregion

        #region Open

        // TODO: Review this tests over different file opening modes.
        // Generally we want test thru over ArzReader/ArzWriter.

        [Fact]
        public void OpenOnNonExistentFileThrows()
        {
            var nonExistentFileName = TestDataUtilities.GetPath(Guid.NewGuid().ToString("N"));

            Assert.Throws<FileNotFoundException>(
                () => ArzDatabase.Open(nonExistentFileName)
                );
        }

        [Fact]
        public void Open()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae0);
        }

        [Fact]
        public void DatabaseSharesFileForRead()
        {
            using var database1 = ArzDatabase.Open(TestData.GtdTqae0);
            using var database2 = ArzDatabase.Open(TestData.GtdTqae0);
        }

        [Fact]
        public void DatabaseLocksFileForWriting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae0);
            Assert.Throws<IOException>(() =>
            {
                using var fileStream = File.Open(TestData.GtdTqae0,
                    FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            });
        }

        #endregion

        #region Count

        [Fact]
        public void Count0()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae0);
            Assert.Equal(0, database.Count);
        }

        [Fact]
        public void Count1()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            Assert.Equal(1, database.Count);
        }

        // TODO: Count should be verified also for adding or removing records.

        #endregion

        #region Enumeration

        [Fact]
        public void Enumeration0()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae0);
            var count = 0;
            foreach (var _ in database.GetAll()) count++;
            Assert.Equal(0, count);
        }

        [Fact]
        public void Enumeration1()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var count = 0;
            foreach (var _ in database.GetAll()) count++;
            Assert.Equal(1, count);
        }

        [Fact]
        public void EnumerationBreaksWhenRecordAdded()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (var _ in database.GetAll())
                    {
                        database.Add("some/record");
                    }
                });
        }

        [Fact]
        public void EnumerationBreaksWhenRecordRemoved()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae2);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    foreach (var _ in database.GetAll())
                    {
                        Assert.True(database.Remove(TestData.GtdTqae2RawRecordNames[0]));
                    }
                });
        }

        #endregion

        #region Get

        [Fact]
        public void GetExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var record = database.Get(TestData.GtdTqae1RawRecordName);
            Assert.NotNull(record);
        }

        [Fact]
        public void GetNotExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var nonExistentRecordName = Guid.NewGuid().ToString("N");

            var ex = Assert.Throws<ArzException>(() =>
                database.Get(nonExistentRecordName)
                );
            Assert.Equal("RecordNotFound", ex.ErrorCode);
        }

        // TODO: after record added it should be accessible
        // TODO: after record removed it should not be accessible

        #endregion

        #region TryGet

        [Fact]
        public void TryGetExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var result = database.TryGet(TestData.GtdTqae1RawRecordName, out var record);
            Assert.True(result);
            Assert.NotNull(record);
        }

        [Fact]
        public void TryGetNotExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var nonExistentRecordName = Guid.NewGuid().ToString("N");

            var result = database.TryGet(nonExistentRecordName, out var record);
            Assert.False(result);
            Assert.Null(record);
        }

        // TODO: after record added it should be accessible
        // TODO: after record removed it should not be accessible

        #endregion

        #region GetOrNull

        [Fact]
        public void GetOrNullExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var record = database.GetOrNull(TestData.GtdTqae1RawRecordName);
            Assert.NotNull(record);
        }

        [Fact]
        public void GetOrNullNotExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var nonExistentRecordName = Guid.NewGuid().ToString("N");

            var record = database.GetOrNull(nonExistentRecordName);
            Assert.Null(record);
        }

        // TODO: after record added it should be accessible
        // TODO: after record removed it should not be accessible

        #endregion

        #region Indexer

        [Fact]
        public void IndexerExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var record = database[TestData.GtdTqae1RawRecordName];
            Assert.NotNull(record);
        }

        [Fact]
        public void IndexerNotExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var nonExistentRecordName = Guid.NewGuid().ToString("N");

            var ex = Assert.Throws<ArzException>(() =>
                database[nonExistentRecordName]
                );
            Assert.Equal("RecordNotFound", ex.ErrorCode);
        }

        // TODO: after record added it should be accessible
        // TODO: after record removed it should not be accessible

        #endregion

        #region Invariants

        // TODO: This tests should be done for every database...

        [Fact]
        public void InvaraintCountAndEnumerable()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            var count = 0;
            foreach (var _ in database.GetAll()) count++;
            Assert.True(count == database.Count);
            // TODO: This invariant should satisfied after any record add or remove.
        }

        #endregion

        #region Add

        [Fact]
        public void Add()
        {
            using var database = ArzDatabase.Create();

            var myRecord1 = database.Add("my/record.dbr");
            Assert.Equal("my/record.dbr", myRecord1.Name);
            Assert.True(myRecord1.Class is null);
        }

        [Fact]
        public void AddWithClass()
        {
            using var database = ArzDatabase.Create();

            var myRecord1 = database.Add("my/record.dbr", "my-record-type");
            Assert.Equal("my/record.dbr", myRecord1.Name);
            Assert.Equal("my-record-type", myRecord1.Class);
        }

        [Fact]
        public void AddButRecordAlreadyExist()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var ex = Assert.Throws<ArzException>(
                () => database.Add(TestData.GtdTqae1RawRecordName)
                );
        }

        [Fact]
        public void AddWithRecordMap()
        {
            using var database = ArzDatabase.Create();

            var myRecord1 = database.Add("my/record.dbr");
            Assert.Equal("my/record.dbr", myRecord1.Name);

            // Triggers record map to be created.
            Assert.Same(myRecord1, database.GetOrNull("my/record.dbr"));

            var myRecord2 = database.Add("my/record2.dbr");
            Assert.Equal("my/record2.dbr", myRecord2.Name);

            Assert.Same(myRecord1, database.GetOrNull("my/record.dbr"));
            Assert.Same(myRecord2, database.GetOrNull("my/record2.dbr"));
        }

        #endregion

        #region GetOrAdd

        [Fact]
        public void GetOrAdd()
        {
            using var database = ArzDatabase.Create();

            var record = database.GetOrAdd(TestData.GtdTqae1RawRecordName);
            Assert.Equal(TestData.GtdTqae1RawRecordName, record.Name);
            Assert.True(record.Class is null);
        }

        [Fact]
        public void GetOrAddAlreadyExisting()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var record = database.GetOrAdd(TestData.GtdTqae1RawRecordName);
            Assert.Equal(TestData.GtdTqae1RawRecordName, record.Name);
            Assert.False(record.Class is null);
        }

        #endregion

        #region RemoveByName

        [Fact]
        public void RemoveByName()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);
            Assert.True(database.Remove(TestData.GtdTqae1RawRecordName));
            Assert.False(database.Remove(TestData.GtdTqae1RawRecordName));
        }

        [Fact]
        public void RemoveByNameWithRecordMap()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            // Trigger record map to be created.
            var record1 = database.Get(TestData.GtdTqae1RawRecordName);

            Assert.True(database.Remove(TestData.GtdTqae1RawRecordName));
            Assert.False(database.Remove(TestData.GtdTqae1RawRecordName));

            Assert.Null(database.GetOrNull(TestData.GtdTqae1RawRecordName));
        }

        #endregion

        #region RemoveByReference

        [Fact]
        public void RemoveByReference()
        {
            using var database = ArzDatabase.Open(TestData.GtdTqae1);

            var recordToDelete = database[TestData.GtdTqae1RawRecordName];
            Assert.True(database.Remove(recordToDelete));
            Assert.False(database.Remove(recordToDelete));
        }

        [Fact]
        public void RemoveByReferenceExternalRecord()
        {
            using var database1 = ArzDatabase.Open(TestData.GtdTqae1);
            using var database2 = ArzDatabase.Open(TestData.GtdTqae2);

            var recordName = TestData.GtdTqae1RawRecordName;
            var recordToDelete = database1[recordName];
            Assert.True(database2.GetOrNull(recordName) != null);

            var ex = Assert.Throws<ArzException>(
                () => database2.Remove(recordToDelete)
                );
            Assert.Equal("ExternalRecord", ex.ErrorCode);
        }

        #endregion

        #region Import & Adopt

        [Fact]
        public void ImportRecord()
        {
            using var database1 = ArzDatabase.Create();
            var s1 = database1.Add("record1");
            s1["--gx-imp-1"] = "abc";
            s1["--gx-imp-2"] = "def";

            using var database2 = ArzDatabase.Create();

            // Making sure what string table effectively different in database2
            var sr = database2.Add("some_record");
            sr["--gx-t-1"] = "zzz";

            var t1 = database2.Import(s1);

            // Imported record is not same record
            Assert.NotSame(t1, s1);

            // Imported record are in database
            Assert.Same(t1, database2["record1"]);

            // Modify source record, to ensure what this changes not affects imported record
            s1.Set("--gx-test-1", 1);
            s1.Set("--gx-test-2", 2);
            s1.Set("--gx-test-3", 3);

            // Modify target record, to ensure what this changes not affects source record
            t1.Set("--gx-test-1", 10);
            t1.Set("--gx-test-2", 20);
            t1.Set("--gx-test-3", 30);

            Assert.Equal(1, (int)s1["--gx-test-1"]);
            Assert.Equal(2, (int)s1["--gx-test-2"]);
            Assert.Equal(3, (int)s1["--gx-test-3"]);

            Assert.Equal(10, (int)t1["--gx-test-1"]);
            Assert.Equal(20, (int)t1["--gx-test-2"]);
            Assert.Equal(30, (int)t1["--gx-test-3"]);

            Assert.Equal("abc", (string)t1["--gx-imp-1"]);
            Assert.Equal("def", (string)t1["--gx-imp-2"]);

            // TODO: check what imported record marked as modified / e.g. eligible for writing
            // TODO: just write database and then compare (ArzWriterTests.AssertDatabaseEqual)
        }

        #endregion
    }
}
