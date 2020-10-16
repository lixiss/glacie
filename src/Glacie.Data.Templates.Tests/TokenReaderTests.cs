using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using IO = System.IO;

namespace Glacie.Data.Templates.Tests
{
    [Trait("Category", "TPL")]
    public sealed class TokenReaderTests
    {
        [Fact]
        public void Empty()
        {
            var text = "";
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("                \t\r\n\t\r\n                \r\r\r ")] // Long string which is bigger than buffer
        public void Whitespace(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("=")]
        [InlineData("= ")]
        [InlineData(" = ")]
        public void Assignment(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.Assignment),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("==")]
        [InlineData("== ")]
        [InlineData(" == ")]
        public void Assignment2(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.Assignment),
                new ExpectedToken(TokenType.Assignment),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("{ ")]
        [InlineData(" { ")]
        public void BeginBlock(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.BeginBlock),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("{{")]
        [InlineData("{{ ")]
        [InlineData(" {{ ")]
        public void BeginBlock2(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.BeginBlock),
                new ExpectedToken(TokenType.BeginBlock),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("}")]
        [InlineData("} ")]
        [InlineData(" } ")]
        public void EndBlock(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.EndBlock),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("}}")]
        [InlineData("}} ")]
        [InlineData(" }} ")]
        public void EndBlock2(string text)
        {
            AssertTokenSequence(new[] {
                new ExpectedToken(TokenType.EndBlock),
                new ExpectedToken(TokenType.EndBlock),
                new ExpectedToken(TokenType.EndOfStream)
                }, ReadTokens(text));
        }

        [Theory]
        [InlineData("group", new[] { TokenType.Group, TokenType.EndOfStream })]
        [InlineData(" group ", new[] { TokenType.Group, TokenType.EndOfStream })]
        [InlineData("group=", new[] { TokenType.Group, TokenType.Assignment, TokenType.EndOfStream })]
        [InlineData("group{", new[] { TokenType.Group, TokenType.BeginBlock, TokenType.EndOfStream })]
        [InlineData("group}", new[] { TokenType.Group, TokenType.EndBlock, TokenType.EndOfStream })]
        internal void GroupKeyword(string text, TokenType[] expectedTokenTypes)
        {
            AssertTokenSequence(
                expectedTokenTypes.Select(x => new ExpectedToken(x)),
                ReadTokens(text));
        }

        [Fact]
        public void UnknownToken()
        {
            var text = "some_unknown_token_string_big_enough_to_resize_buffer";
            Assert.True(text.Length > 32);

            using var textReader = new IO.StringReader(text);
            using var tokenReader = new TokenReader(textReader, 1);

            var token = tokenReader.Read();
            Assert.Equal(TokenType.Unknown, token.Type);
            Assert.Equal(text, tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.EndOfStream, token.Type);
        }

        [Fact]
        public void UnknownTokenWithLimit()
        {
            var text = "some_unknown_token_string_big_enough_to_resize_buffer";
            Assert.True(text.Length > 32);

            using var textReader = new IO.StringReader(text);
            using var tokenReader = new TokenReader(textReader, 16, 16);

            var token = tokenReader.Read();
            Assert.Equal(TokenType.Unknown, token.Type);
            Assert.Equal(text.Substring(0, 16), tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.Unknown, token.Type);
            Assert.Equal(text.Substring(16, 16), tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.Unknown, token.Type);
            Assert.Equal(text.Substring(32, 16), tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.Unknown, token.Type);
            Assert.Equal(text.Substring(48), tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.EndOfStream, token.Type);
        }

        [Theory]
        [InlineData("\"value\"", "value")]
        public void StringToken(string text, string expectedValue)
        {
            using var textReader = new IO.StringReader(text);
            using var tokenReader = new TokenReader(textReader, 1);

            var token = tokenReader.Read();
            Assert.Equal(TokenType.StringLiteral, token.Type);
            Assert.Equal(expectedValue, tokenReader.GetTokenValue(token));

            token = tokenReader.Read();
            Assert.Equal(TokenType.EndOfStream, token.Type);
        }

        [Theory]
        [InlineData("\"value")]
        [InlineData("\"1234567890123456789012345678901234567890123456789012345678901234567890")]
        public void UnexpectedEosStringToken(string text)
        {
            using var textReader = new IO.StringReader(text);
            using var tokenReader = new TokenReader(textReader, 1);

            var token = tokenReader.Read();
            Assert.Equal(TokenType.UnexpectedEndOfStreamInStringLiteral, token.Type);

            token = tokenReader.Read();
            Assert.Equal(TokenType.EndOfStream, token.Type);
        }

        [Fact]
        public void LineInformationEmpty()
        {
            var text = "";
            AssertTokenSequence(new[]
            {
                new ExpectedToken(TokenType.EndOfStream, 1, 1),
            }, ReadTokens(text), lineInfo: true);
        }

        [Fact]
        public void LineInformationWhitespace()
        {
            var text = "  ";
            AssertTokenSequence(new[]
            {
                new ExpectedToken(TokenType.EndOfStream, 1, 3),
            }, ReadTokens(text), lineInfo: true);
        }

        [Fact]
        public void LineInformation2()
        {
            var text = " \"some\nabc\nstring\"=  ";
            AssertTokenSequence(new[]
            {
                new ExpectedToken(TokenType.StringLiteral, 1, 2),
                new ExpectedToken(TokenType.Assignment, 3, 8),
                new ExpectedToken(TokenType.EndOfStream, 3, 11),
            }, ReadTokens(text), lineInfo: true);
        }

        [Fact]
        public void LineInformation3()
        {
            var text = "  group{}\ngroup{}\n\t\"some\nstring\"  \n";
            AssertTokenSequence(new[]
            {
                new ExpectedToken(TokenType.Group, 1, 1 + 2),
                new ExpectedToken(TokenType.BeginBlock, 1, 6 + 2),
                new ExpectedToken(TokenType.EndBlock, 1, 7 + 2),
                new ExpectedToken(TokenType.Group, 2, 1),
                new ExpectedToken(TokenType.BeginBlock, 2, 6),
                new ExpectedToken(TokenType.EndBlock, 2, 7),
                new ExpectedToken(TokenType.StringLiteral, 3, 2),
                new ExpectedToken(TokenType.EndOfStream, 5, 1),
            }, ReadTokens(text), lineInfo: true);
        }

        private readonly struct ExpectedToken
        {
            public readonly TokenType Type;
            public readonly int LineNumber;
            public readonly int LinePosition;

            public ExpectedToken(TokenType type)
            {
                Type = type;
                LineNumber = 0;
                LinePosition = 0;
            }

            public ExpectedToken(TokenType type, int lineNumber, int linePosition)
            {
                Type = type;
                LineNumber = lineNumber;
                LinePosition = linePosition;
            }
        }

        private void AssertTokenSequence(IEnumerable<ExpectedToken> expected, IEnumerable<Token> actual, bool lineInfo = false)
        {
            var actualEnumerator = actual.GetEnumerator();
            var expectedEnumerator = expected.GetEnumerator();

            while (true)
            {
                var an = actualEnumerator.MoveNext();
                var en = expectedEnumerator.MoveNext();
                Assert.Equal(an, en);
                if (an == false) break;

                var actualCurrent = actualEnumerator.Current;
                var expectedCurrent = expectedEnumerator.Current;

                Assert.Equal(expectedCurrent.Type, actualCurrent.Type);
                if (lineInfo)
                {
                    Assert.Equal(expectedCurrent.LineNumber, actualCurrent.LineNumber);
                    Assert.Equal(expectedCurrent.LinePosition, actualCurrent.LinePosition);
                }

                // Line
                // Column
                // Value
            }
        }

        private IEnumerable<Token> ReadTokens(string text)
        {
            using var reader = new IO.StringReader(text);
            using var tokenReader = new TokenReader(reader, bufferSize: 1);

            while (true)
            {
                var token = tokenReader.Read();
                yield return token;
                if (token.Type == TokenType.EndOfStream) break;
            }

            var endToken = tokenReader.Read();
            Assert.Equal(TokenType.EndOfStream, endToken.Type);
        }

    }
}
