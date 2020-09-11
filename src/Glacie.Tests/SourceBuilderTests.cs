using System;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using Xunit;

namespace Glacie.Tests
{
    [Trait("Category", "GX")]
    public sealed class SourceBuilderTests : GlacieTests
    {
        [Fact]
        public void ThrowsIfConfigureActionIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Source((Action<ISourceBuilder>)null!);
                });
            });
        }

        [Fact]
        public void ThrowsIfSourceNotConfigured()
        {
            AssertGxError("SourceConfigurationInvalid", () =>
            {
                using var context = Context.Create(c =>
                {
                    c.Source(s => { });
                });
            });
        }

        [Fact]
        public void ThrowsIfSourcePathNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Source(s => s.Path(null!));
                });
            });

            Assert.Throws<ArgumentException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Source(s => s.Path(""));
                });
            });
        }

        [Fact]
        public void ThrowsIfSourceDatabaseNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Source(s => s.Database(null!));
                });
            });
        }

        [Fact]
        public void ThrowsIfSourcePathAndDatabaseSpecified()
        {
            AssertGxError("SourceConfigurationInvalid", () =>
            {
                using var sourceDatabase = ArzDatabase.Create();
                using var context = Context.Create(c =>
                {
                    c.Source(s => {
                        s.Path("some-path");
                        s.Database(sourceDatabase);
                    });
                });
            });
        }
    }
}
