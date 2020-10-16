using System;

using Glacie.Abstractions;
using Glacie.Configuration;
using Glacie.Data.Arz;
using Glacie.Targeting;

using Xunit;

namespace Glacie.Tests
{
    // TODO: This tests requires to be rebuild to go over project context.

    /*
    [Trait("Category", "GX")]
    public sealed class SourceBuilderTests : GlacieTests
    {
        [Fact]
        public void ThrowsIfSourceNotConfigured()
        {
            AssertGxError("SourceConfigurationInvalid", () =>
            {
                var cc = new ContextConfiguration();
                cc.Sources.Add(new ContextSourceConfiguration());
                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }

        [Fact]
        public void ThrowsIfSourcePathNullOrEmpty()
        {
            AssertGxError("SourceConfigurationInvalid", () =>
            {
                var cc = new ContextConfiguration();
                cc.Sources.Add(new ContextSourceConfiguration() { Path = "" });

                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }

        [Fact]
        public void ThrowsIfSourcePathAndDatabaseSpecified()
        {
            AssertGxError("SourceConfigurationInvalid", () =>
            {
                using var sourceDatabase = ArzDatabase.Create();

                var cc = new ContextConfiguration();
                cc.Sources.Add(new ContextSourceConfiguration()
                {
                    Path = "some-path",
                    Database = sourceDatabase
                });

                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }
    }
    */
}
