using System;
using Xunit;

namespace Glacie.Tests
{
    // TODO: (Medium) Complete VariantTests together with API.

    public sealed class VariantTests
    {
        [Fact]
        public void ImplicitFromFloat32()
        {
            AssertVariant(123.0f, 123.0f);
        }

        [Fact]
        public void ImplicitFromFloat64()
        {
            AssertVariant(123.0f, 123.0);
        }

        [Fact]
        public void ImplicitFromBoolean()
        {
            AssertVariant(true, true);
        }

        [Fact]
        public void ImplicitFromInteger()
        {
            AssertVariant(123, 123);
        }

        [Fact]
        public void ImplicitFromString()
        {
            AssertVariant("abc", "abc");
        }

        [Fact]
        public void ImplicitFromFloat32Array()
        {
            AssertVariant(new[] { 123.0f, 456.0f }, new[] { 123.0f, 456.0f });
        }

        [Fact]
        public void ImplicitFromFloat64Array()
        {
            AssertVariant(new[] { 123.0, 456.0 }, new[] { 123.0, 456.0 });
        }

        [Fact]
        public void ImplicitFromBooleanArray()
        {
            AssertVariant(new[] { true, false }, new[] { true, false });
        }

        [Fact]
        public void ImplicitFromIntegerArray()
        {
            AssertVariant(new[] { 123, 456 }, new[] { 123, 456 });
        }

        [Fact]
        public void ImplicitFromStringArray()
        {
            AssertVariant(new[] { "abc", "def" }, new[] { "abc", "def" });
        }

        private void AssertVariant(float expected, Variant actual)
        {
            Assert.Equal(VariantType.Real, actual.Type);
            Assert.Equal(1, actual.Count);
            Assert.True(actual.Is<float>());
            Assert.Equal(expected, actual.Get<float>());
            Assert.Equal(expected, actual.Get<float>(0));

            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());

            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<float>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<float>(1));
        }

        private void AssertVariant(bool expected, Variant actual)
        {
            Assert.Equal(VariantType.Boolean, actual.Type);
            Assert.Equal(1, actual.Count);
            Assert.True(actual.Is<bool>());
            Assert.Equal(expected, actual.Get<bool>());
            Assert.Equal(expected, actual.Get<bool>(0));

            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());

            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<bool>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<bool>(1));
        }

        private void AssertVariant(int expected, Variant actual)
        {
            Assert.Equal(VariantType.Integer, actual.Type);
            Assert.Equal(1, actual.Count);
            Assert.True(actual.Is<int>());
            Assert.Equal(expected, actual.Get<int>());
            Assert.Equal(expected, actual.Get<int>(0));

            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());

            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<int>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<int>(1));
        }

        private void AssertVariant(string expected, Variant actual)
        {
            Assert.Equal(VariantType.String, actual.Type);
            Assert.Equal(1, actual.Count);
            Assert.True(actual.Is<string>());
            Assert.Equal(expected, actual.Get<string>());
            Assert.Equal(expected, actual.Get<string>(0));

            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());

            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<string>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<string>(1));
        }

        private void AssertVariant(float[] expected, Variant actual)
        {
            Assert.Equal(VariantType.RealArray, actual.Type);
            Assert.Equal(expected.Length, actual.Count);
            Assert.True(actual.Is<float[]>());
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.Get<float>(i));
            }
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<float>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<float>(actual.Count));

            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());
        }

        private void AssertVariant(double[] expected, Variant actual)
        {
            Assert.Equal(VariantType.Float64Array, actual.Type);
            Assert.Equal(expected.Length, actual.Count);
            Assert.True(actual.Is<double[]>());
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.Get<double>(i));
            }
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<double>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<double>(actual.Count));

            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());
        }

        private void AssertVariant(int[] expected, Variant actual)
        {
            Assert.Equal(VariantType.IntegerArray, actual.Type);
            Assert.Equal(expected.Length, actual.Count);
            Assert.True(actual.Is<int[]>());
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.Get<int>(i));
            }
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<int>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<int>(actual.Count));

            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());
        }

        private void AssertVariant(bool[] expected, Variant actual)
        {
            Assert.Equal(VariantType.BooleanArray, actual.Type);
            Assert.Equal(expected.Length, actual.Count);
            Assert.True(actual.Is<bool[]>());
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.Get<bool>(i));
            }
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<bool>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<bool>(actual.Count));

            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());
        }

        private void AssertVariant(string[] expected, Variant actual)
        {
            Assert.Equal(VariantType.StringArray, actual.Type);
            Assert.Equal(expected.Length, actual.Count);
            Assert.True(actual.Is<string[]>());
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.Get<string>(i));
            }
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<string>(-1));
            Assert.Throws<IndexOutOfRangeException>(() => actual.Get<string>(actual.Count));

            Assert.Throws<InvalidOperationException>(() => actual.Get<int>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<float>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<bool>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<string>());
            Assert.Throws<InvalidOperationException>(() => actual.Get<double>());
        }
    }
}
