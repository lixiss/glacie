using System;

using Glacie.Diagnostics;

using Xunit;

using IO = System.IO;

namespace Glacie.Data.Templates.Tests
{
    [Trait("Category", "TPL")]
    public sealed class TemplateReaderTests
    {
        private readonly TemplateReader _templateReader = new TemplateReader();

        [Fact]
        public void Empty()
        {
            var text = "";
            AssertDiagnosticException(() => Parse(text));
        }

        [Fact]
        public void NoRoot()
        {
            var text = "fileNameHistoryEntry { \"Templates/New Template.tpl\" }";
            AssertDiagnosticException(() => Parse(text));
        }

        [Fact]
        public void MultipleRoots()
        {
            var text = "group { name = \"abc\" type = \"def\" } group { name = \"abc\" type = \"def\" } ";
            AssertDiagnosticException(() => Parse(text));
        }

        private void AssertDiagnosticException(Action action)
        {
            var ex = Assert.Throws<DiagnosticException>(action);
        }

        private Template Parse(string text)
        {
            using var textReader = new IO.StringReader(text);
            return _templateReader.Read(textReader);
        }
    }
}
