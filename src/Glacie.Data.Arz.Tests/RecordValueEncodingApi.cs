using System;
using System.Linq;
using Glacie.Data.Arz.Infrastructure;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    // TODO: (Low) (RecordValueEncodingApi Tests) - rename, and review.

    /// <summary>
    /// This tests verifies what values of different types are properly encoded.
    /// </summary>
    public class RecordValueEncodingApi : IDisposable
    {
        private const string FieldName = "--gx-field";
        private const string FieldName2 = "--gx-field2";
        private const string FieldName3 = "--gx-field3";

        private readonly ArzDatabase Database;
        private readonly ArzRecord Record;
        private readonly IArzRecordMetrics RecordMetrics;

        public RecordValueEncodingApi()
        {
            Database = ArzDatabase.Create();
            Record = Database.Add("some/record");
            RecordMetrics = Record;
        }

        public void Dispose()
        {
            Database?.Dispose();
        }

        [Fact]
        public void SetInt32()
        {
            Record.Set(FieldName, 123);
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>());
        }

        [Fact]
        public void SetFloat32()
        {
            Record.Set(FieldName, 123.0f);
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat32NonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, float.NaN));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, float.NegativeInfinity));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, float.PositiveInfinity));
        }

        [Fact]
        public void SetFloat64()
        {
            Record.Set(FieldName, 123.0);
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat64NonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, double.MaxValue));
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, double.MinValue));
        }

        [Fact]
        public void SetFloat64NonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, double.NaN));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, double.NegativeInfinity));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, double.PositiveInfinity));
        }

        [Fact]
        public void SetBoolean()
        {
            Record.Set(FieldName, true);
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>());
        }

        [Fact]
        public void SetString()
        {
            Record.Set(FieldName, "abc");
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>());
        }

        [Fact]
        public void SetInt32ArraySingle()
        {
            Record.Set(FieldName, new int[] { 123 });
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>());
        }

        [Fact]
        public void SetFloat32ArraySingle()
        {
            Record.Set(FieldName, new float[] { 123.0f });
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat32ArraySingleNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { float.NaN }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { float.NegativeInfinity }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { float.PositiveInfinity }));
        }

        [Fact]
        public void SetFloat64ArraySingle()
        {
            Record.Set(FieldName, new double[] { 123.0 });
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat64ArraySingleNonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, new double[] { double.MaxValue }));
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, new double[] { double.MinValue }));
        }

        [Fact]
        public void SetFloat64ArraySingleNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { double.NaN }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { double.NegativeInfinity }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { double.PositiveInfinity }));
        }

        [Fact]
        public void SetBooleanArraySingle()
        {
            Record.Set(FieldName, new bool[] { true });
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>());
        }

        [Fact]
        public void SetStringArraySingle()
        {
            Record.Set(FieldName, new string[] { "abc" });
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>());
        }

        [Fact]
        public void SetInt32Array()
        {
            Record.Set(FieldName, new int[] { 123, 456, 789, 0x04030201 });
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>(0));
            Assert.Equal(456, field.Get<int>(1));
            Assert.Equal(789, field.Get<int>(2));
            Assert.Equal(0x04030201, field.Get<int>(3));
        }

        [Fact]
        public void SetFloat32Array()
        {
            Record.Set(FieldName, new float[] { 123.0f, 456.0f, 789.0f });
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>(0));
            Assert.Equal(456.0f, field.Get<float>(1));
            Assert.Equal(789.0f, field.Get<float>(2));
        }

        [Fact]
        public void SetFloat32ArrayNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { 1.0f, 2.0f, float.NaN }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { 1.0f, 2.0f, float.NegativeInfinity }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new float[] { 1.0f, 2.0f, float.PositiveInfinity }));
        }

        [Fact]
        public void SetFloat64Array()
        {
            Record.Set(FieldName, new double[] { 123.0, 456.0, 789.0 });
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>(0));
            Assert.Equal(456.0f, field.Get<float>(1));
            Assert.Equal(789.0f, field.Get<float>(2));
        }

        [Fact]
        public void SetFloat64ArrayNonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, new double[] { 1.0, 2.0, double.MaxValue }));
            AssertThrowsArithmeticOverflow(() => Record.Set(FieldName, new double[] { 1.0, 2.0, double.MinValue }));
        }

        [Fact]
        public void SetFloat64ArrayNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { 1.0, 2.0, double.NaN }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { 1.0, 2.0, double.NegativeInfinity }));
            AssertThrowsNotFiniteNumber(() => Record.Set(FieldName, new double[] { 1.0, 2.0, double.PositiveInfinity }));
        }

        [Fact]
        public void SetBooleanArray()
        {
            Record.Set(FieldName, new bool[] { true, false, true });
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>(0));
            Assert.False(field.Get<bool>(1));
            Assert.True(field.Get<bool>(2));
        }

        [Fact]
        public void SetStringArray()
        {
            Record.Set(FieldName, new string[] { "abc", "def", "ghi" });
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>(0));
            Assert.Equal("def", field.Get<string>(1));
            Assert.Equal("ghi", field.Get<string>(2));
        }

        [Fact]
        public void SetInt32Variant()
        {
            Record[FieldName] = 123;
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>());
        }

        [Fact]
        public void SetFloat32Variant()
        {
            Record[FieldName] = 123.0f;
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat32VariantNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = float.NaN; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = float.NegativeInfinity; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = float.PositiveInfinity; });
        }

        [Fact]
        public void SetFloat64Variant()
        {
            Record[FieldName] = 123.0;
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat64VariantNonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = double.MaxValue; });
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = double.MinValue; });
        }

        [Fact]
        public void SetFloat64VariantNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = double.NaN; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = double.NegativeInfinity; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = double.PositiveInfinity; });
        }

        [Fact]
        public void SetBooleanVariant()
        {
            Record[FieldName] = true;
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>());
        }

        [Fact]
        public void SetStringVariant()
        {
            Record[FieldName] = "abc";
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>());
        }

        [Fact]
        public void SetInt32ArrayVariantSingle()
        {
            Record[FieldName] = new int[] { 123 };
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>());
        }

        [Fact]
        public void SetFloat32ArrayVariantSingle()
        {
            Record[FieldName] = new float[] { 123.0f };
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat32ArrayVariantSingleNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { float.NaN }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { float.NegativeInfinity }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { float.PositiveInfinity }; });
        }

        [Fact]
        public void SetFloat64ArrayVariantSingle()
        {
            Record[FieldName] = new double[] { 123.0 };
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>());
        }

        [Fact]
        public void SetFloat64ArrayVariantSingleNonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = new double[] { double.MaxValue }; });
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = new double[] { double.MinValue }; });
        }

        [Fact]
        public void SetFloat64ArrayVariantSingleNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { double.NaN }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { double.NegativeInfinity }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { double.PositiveInfinity }; });
        }

        [Fact]
        public void SetBooleanArrayVariantSingle()
        {
            Record[FieldName] = new bool[] { true };
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>());
        }

        [Fact]
        public void SetStringArrayVariantSingle()
        {
            Record[FieldName] = new string[] { "abc" };
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>());
        }

        [Fact]
        public void SetInt32ArrayVariant()
        {
            Record[FieldName] = new int[] { 123, 456, 789 };
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>(0));
            Assert.Equal(456, field.Get<int>(1));
            Assert.Equal(789, field.Get<int>(2));
        }

        [Fact]
        public void SetFloat32ArrayVariant()
        {
            Record[FieldName] = new float[] { 123.0f, 456.0f, 789.0f };
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>(0));
            Assert.Equal(456.0f, field.Get<float>(1));
            Assert.Equal(789.0f, field.Get<float>(2));
        }

        [Fact]
        public void SetFloat32ArrayVariantNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { 1.0f, 2.0f, float.NaN }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { 1.0f, 2.0f, float.NegativeInfinity }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new float[] { 1.0f, 2.0f, float.PositiveInfinity }; });
        }

        [Fact]
        public void SetFloat64ArrayVariant()
        {
            Record[FieldName] = new double[] { 123.0, 456.0, 789.0 };
            var field = Record.Get(FieldName);
            Assert.Equal(123.0f, field.Get<float>(0));
            Assert.Equal(456.0f, field.Get<float>(1));
            Assert.Equal(789.0f, field.Get<float>(2));
        }

        [Fact]
        public void SetFloat64ArrayVariantNonRepresentativeShouldThrow()
        {
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = new double[] { 1.0, 2.0, double.MaxValue }; });
            AssertThrowsArithmeticOverflow(() => { Record[FieldName] = new double[] { 1.0, 2.0, double.MinValue }; });
        }

        [Fact]
        public void SetFloat64ArrayVariantNonFiniteShouldThrow()
        {
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { 1.0, 2.0, double.NaN }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { 1.0, 2.0, double.NegativeInfinity }; });
            AssertThrowsNotFiniteNumber(() => { Record[FieldName] = new double[] { 1.0, 2.0, double.PositiveInfinity }; });
        }

        [Fact]
        public void SetBooleanArrayVariant()
        {
            Record[FieldName] = new bool[] { true, false, true };
            var field = Record.Get(FieldName);
            Assert.True(field.Get<bool>(0));
            Assert.False(field.Get<bool>(1));
            Assert.True(field.Get<bool>(2));
        }

        [Fact]
        public void SetStringArrayVariant()
        {
            Record[FieldName] = new string[] { "abc", "def", "ghi" };
            var field = Record.Get(FieldName);
            Assert.Equal("abc", field.Get<string>(0));
            Assert.Equal("def", field.Get<string>(1));
            Assert.Equal("ghi", field.Get<string>(2));
        }

        #region Encoding Tests

        [Fact]
        public void OverwritingSmallerWithLargerFieldWhenItLastField()
        {
            // Overwriting smaller field with larger field
            Assert.Equal(0, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, 777);
            Assert.Equal(1, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, new int[] { 123, 456, 789, 0x04030201 });
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>(0));
            Assert.Equal(456, field.Get<int>(1));
            Assert.Equal(789, field.Get<int>(2));
            Assert.Equal(0x04030201, field.Get<int>(3));

            Assert.Equal(1, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            // Ensures what tail properly maintained.
            Assert.Equal(
                new[] { FieldName },
                Record.GetAll().Select(x => x.Name)
                );

            // TODO: should not break rest invariants
        }

        [Fact]
        public void OverwritingSmallerWithLargerFieldWhenItNonLastField()
        {
            Assert.Equal(0, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, 777);
            Record.Set(FieldName2, 999);
            Assert.Equal(2, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, new int[] { 123, 456, 789, 0x04030201 });
            var field = Record.Get(FieldName);
            Assert.Equal(123, field.Get<int>(0));
            Assert.Equal(456, field.Get<int>(1));
            Assert.Equal(789, field.Get<int>(2));
            Assert.Equal(0x04030201, field.Get<int>(3));

            Assert.Equal(2, Record.Count);
            Assert.Equal(1, RecordMetrics.NumberOfRemovedFields);

            Assert.Equal(
                new[] { FieldName2, FieldName },
                Record.GetAll().Select(x => x.Name)
                );

            // TODO: should not break invariants
        }

        [Fact]
        public void OverwritingLargerWithSmallerFieldWhenItLastField()
        {
            // TODO: Tail field
            Assert.Equal(0, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, new int[] { 123, 456, 789, 0x04030201 });
            Assert.Equal(1, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, 0x08060504);
            var field = Record.Get(FieldName);
            Assert.Equal(0x08060504, field.Get<int>());

            Assert.Equal(1, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            // Ensures what tail properly maintained.
            Assert.Equal(
                new[] { FieldName },
                Record.GetAll().Select(x => x.Name)
                );

            // TODO: should not break rest invariants
        }

        [Fact]
        public void OverwritingLargerWithSmallerFieldWhenItNonLastFieldNoSplitting()
        {
            Assert.Equal(0, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, new int[] { 123, 456, 789 });
            Record.Set(FieldName2, 999);
            Assert.Equal(2, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, 0x08060504);
            var field = Record.Get(FieldName);
            Assert.Equal(0x08060504, field.Get<int>());

            Assert.Equal(2, Record.Count);
            Assert.Equal(1, RecordMetrics.NumberOfRemovedFields);

            // Without splitting field will be appended to the end.
            Assert.Equal(
                new[] { FieldName2, FieldName },
                Record.GetAll().Select(x => x.Name)
                );

            // TODO: should not break rest invariants
        }

        [Fact]
        public void OverwritingLargerWithSmallerFieldWhenItNonLastFieldSplitting()
        {
            Assert.Equal(0, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, new int[] { 123, 456, 789, 0x04030201 });
            Record.Set(FieldName2, 999);
            Assert.Equal(2, Record.Count);
            Assert.Equal(0, RecordMetrics.NumberOfRemovedFields);

            Record.Set(FieldName, 0x08060504);
            var field = Record.Get(FieldName);
            Assert.Equal(0x08060504, field.Get<int>());

            Assert.Equal(2, Record.Count);
            Assert.Equal(1, RecordMetrics.NumberOfRemovedFields);

            // Splitting doesn't changes field order.
            Assert.Equal(
                new[] { FieldName, FieldName2 },
                Record.GetAll().Select(x => x.Name)
                );

            // TODO: should not break rest invariants
        }

        #endregion


        private static void AssertThrowsNotFiniteNumber(Action action)
        {
            var ex = Assert.Throws<ArzException>(action);
            Assert.Equal("NotFiniteNumber", ex.ErrorCode);
        }

        private static void AssertThrowsArithmeticOverflow(Action action)
        {
            var ex = Assert.Throws<ArzException>(action);
            Assert.Equal("ArithmeticOverflow", ex.ErrorCode);
        }
    }
}
