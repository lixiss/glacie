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
    public sealed class TargetBuilderTests : GlacieTests
    {
        [Fact]
        public void ThrowsIfTargetNotConfigured()
        {
            AssertGxError("TargetConfigurationInvalid", () =>
            {
                var cc = new ContextConfiguration();
                cc.Target = new ContextTargetConfiguration()
                {
                };

                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }

        [Fact]
        public void ThrowsIfTargetPathNullOrEmpty()
        {
            AssertGxError("TargetConfigurationInvalid", () =>
            {
                var cc = new ContextConfiguration();
                cc.Target = new ContextTargetConfiguration()
                {
                    Path = "",
                };

                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }

        [Fact]
        public void ThrowsIfTargetPathAndDatabaseSpecified()
        {
            AssertGxError("TargetConfigurationInvalid", () =>
            {
                using var targetDatabase = ArzDatabase.Create();

                var cc = new ContextConfiguration();
                cc.Target = new ContextTargetConfiguration()
                {
                    Path = "some-path",
                    Database = targetDatabase,
                };

                using var context = Context.Create(new UnifiedEngineType(), cc);
            });
        }
    }
    */
}
