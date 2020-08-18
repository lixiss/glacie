using System;
using Glacie.Abstractions;
using Glacie.Data.Arz;
using Xunit;

namespace Glacie.Tests
{
    public sealed class TargetBuilderTests : GlacieTests
    {
        [Fact]
        public void ThrowsIfConfigureActionIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Target((Action<ITargetBuilder>)null!);
                });
            });
        }

        [Fact]
        public void ThrowsIfTargetNotConfigured()
        {
            AssertGxError("TargetConfigurationInvalid", () =>
            {
                using var context = Context.Create(c =>
                {
                    c.Target(t => { });
                });
            });
        }

        [Fact]
        public void ThrowsIfTargetPathNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Target(t => t.Path(null!));
                });
            });

            Assert.Throws<ArgumentException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Target(t => t.Path(""));
                });
            });
        }

        [Fact]
        public void ThrowsIfTargetDatabaseNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                using var context = Context.Create(c =>
                {
                    c.Target(t => t.Database(null!));
                });
            });
        }

        [Fact]
        public void ThrowsIfTargetPathAndDatabaseSpecified()
        {
            AssertGxError("TargetConfigurationInvalid", () =>
            {
                using var targetDatabase = ArzDatabase.Create();
                using var context = Context.Create(c =>
                {
                    c.Target(t => {
                        t.Path("some-path");
                        t.Database(targetDatabase);
                    });
                });
            });
        }

    }
}
