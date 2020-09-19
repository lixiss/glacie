using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Glacie.Collections;

namespace Glacie.Data.Tpl
{
    // TODO: Simplify by create ParseContext and make parse methods as it's members.

    /// <remarks>
    /// This is stateless class. You can reuse this class to parse different streams.
    /// </remarks>
    public sealed class TemplateReader
    {
        private const int StreamReaderBufferSize = -1; // Default buffer (1024)
        private const int TokenReaderBufferSize = 2 * 1024; // 2K is enough to read any known templates

        private readonly Encoding _defaultEncoding;
        private readonly StringInterner _stringInterner;

        public TemplateReader()
            : this(defaultEncoding: Encoding.GetEncoding("iso-8859-1"))
        { }

        public TemplateReader(Encoding defaultEncoding)
        {
            _defaultEncoding = defaultEncoding;
            _stringInterner = new StringInterner();
        }
        
        public Template Read(string path)
            => Read(path, _defaultEncoding);

        public Template Read(string path, Encoding encoding)
        {
            using var stream = File.OpenRead(path);
            return Read(stream, encoding, path, leaveOpen: false);
        }

        public Template Read(Stream stream, string? path = null, bool leaveOpen = false)
            => Read(stream, _defaultEncoding, path, leaveOpen);

        public Template Read(Stream stream, Encoding encoding, string? path = null, bool leaveOpen = false)
        {
            Check.Argument.NotNull(stream, nameof(stream));
            Check.Argument.NotNull(encoding, nameof(encoding));

            using var textReader = new StreamReader(stream,
                encoding: encoding,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: StreamReaderBufferSize,
                leaveOpen: leaveOpen);

            return Read(textReader, path);
        }

        public Template Read(TextReader reader, string? path = null, bool leaveOpen = false)
        {
            Check.Argument.NotNull(reader, nameof(reader));
            try
            {
                return ReadInternal(reader, path);
            }
            finally
            {
                if (!leaveOpen) reader.Dispose();
            }
        }

        private Template ReadInternal(TextReader reader, string? path = null)
        {
            // TODO: Create ParserContext struct which will hold, reader, path, etc...

            var tokenReader = new TokenReader(reader, TokenReaderBufferSize);
            try
            {
                return ParseTemplate(ref tokenReader, path);
            }
            finally
            {
                tokenReader.Dispose();
            }
        }

        private Template ParseTemplate(ref TokenReader reader, string? path = null)
        {
            var template = new Template();
            template.Name = path ?? "";

            TemplateGroup? rootGroup = null;
            List<string>? fileNameHistory = null;

            while (true)
            {
                var token = reader.Read();
                if (token.Type == TokenType.EndOfStream)
                {
                    if (rootGroup == null)
                    {
                        // Template should have at least one group.
                        throw DiagnosticFactory
                            .UnexpectedEndOfStream(token.ToLocation(path))
                            .AsException();
                    }
                    break;
                }

                switch (token.Type)
                {
                    case TokenType.Group:
                        if (rootGroup != null)
                        {
                            // Template may have only one root group.
                            throw DiagnosticFactory
                                .UnexpectedToken(token.ToLocation(path), token.Type)
                                .AsException();
                        }
                        rootGroup = ParseGroup(ref reader, path);
                        break;

                    case TokenType.FileNameHistoryEntry:
                        if (fileNameHistory != null)
                        {
                            // Template may have only one filenamehistoryentry section.
                            throw DiagnosticFactory
                                .UnexpectedToken(token.ToLocation(path), token.Type)
                                .AsException();
                        }
                        fileNameHistory = ParseFileNameHistoryEntry(ref reader, path);
                        break;

                    default:
                        throw DiagnosticFactory
                            .UnexpectedToken(token.ToLocation(path), token.Type)
                            .AsException();
                }
            }

            Check.That(rootGroup != null);
            template.Root = rootGroup;
            if (fileNameHistory != null)
            {
                template.FileNameHistory = fileNameHistory;
            }

            return template;
        }

        private TemplateGroup ParseGroup(ref TokenReader tokenReader, string? path)
        {
            var beginToken = tokenReader.Read();
            if (beginToken.Type != TokenType.BeginBlock)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(beginToken.ToLocation(path), beginToken.Type)
                    .AsException();
            }

            var result = new TemplateGroup();

            string? name = null;
            string? type = null;

            while (true)
            {
                var token = tokenReader.Read();
                switch (token.Type)
                {
                    case TokenType.Name:
                        ReadStringProperty(ref name, in token, ref tokenReader, path);
                        break;

                    case TokenType.Type:
                        ReadStringProperty(ref type, in token, ref tokenReader, path);
                        break;

                    case TokenType.Group:
                        {
                            var group = ParseGroup(ref tokenReader, path);
                            result.Add(group);
                        }
                        break;

                    case TokenType.Variable:
                        {
                            var variable = ParseVariable(ref tokenReader, path);
                            result.Add(variable);
                        }
                        break;

                    default:
                        if (token.Type == TokenType.EndBlock)
                        {
                            break;
                        }
                        else
                        {
                            throw DiagnosticFactory
                                .UnexpectedToken(token.ToLocation(path), token.Type)
                                .AsException();
                        }
                }

                if (token.Type == TokenType.EndBlock)
                {
                    break;
                }
            }

            if (name == null)
            {
                throw DiagnosticFactory
                    .PropertyRequired(beginToken.ToLocation(path), "name")
                    .AsException();
            }

            if (type == null)
            {
                throw DiagnosticFactory
                    .PropertyRequired(beginToken.ToLocation(path), "type")
                    .AsException();
            }

            result.Name = name;
            result.Type = type;

            return result;
        }

        private TemplateVariable ParseVariable(ref TokenReader tokenReader, string? path)
        {
            var beginToken = tokenReader.Read();
            if (beginToken.Type != TokenType.BeginBlock)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(beginToken.ToLocation(path), beginToken.Type)
                    .AsException();
            }

            var result = new TemplateVariable();

            string? name = null;
            string? @class = null;
            string? type = null;
            string? description = null;
            string? value = null;
            string? defaultValue = null;

            while (true)
            {
                var token = tokenReader.Read();
                switch (token.Type)
                {
                    case TokenType.Name:
                        ReadStringProperty(ref name, in token, ref tokenReader, path);
                        break;

                    case TokenType.Class:
                        ReadStringProperty(ref @class, in token, ref tokenReader, path);
                        break;

                    case TokenType.Type:
                        ReadStringProperty(ref type, in token, ref tokenReader, path);
                        break;

                    case TokenType.Description:
                        ReadStringProperty(ref description, in token, ref tokenReader, path);
                        break;

                    case TokenType.Value:
                        ReadStringProperty(ref value, in token, ref tokenReader, path);
                        break;

                    case TokenType.DefaultValue:
                        ReadStringProperty(ref defaultValue, in token, ref tokenReader, path);
                        break;

                    default:
                        if (token.Type == TokenType.EndBlock)
                        {
                            break;
                        }
                        else
                        {
                            throw DiagnosticFactory
                                .UnexpectedToken(token.ToLocation(path), token.Type)
                                .AsException();
                        }
                }

                if (token.Type == TokenType.EndBlock)
                {
                    break;
                }
            }

            if (name == null)
            {
                throw DiagnosticFactory
                    .PropertyRequired(beginToken.ToLocation(path), "name")
                    .AsException();
            }
            result.Name = name;

            if (@class == null)
            {
                throw DiagnosticFactory
                    .PropertyRequired(beginToken.ToLocation(path), "class")
                    .AsException();
            }
            result.Class = @class;

            if (type == null)
            {
                throw DiagnosticFactory
                    .PropertyRequired(beginToken.ToLocation(path), "type")
                    .AsException();
            }
            result.Type = type;

            result.Description = description ?? "";
            result.Value = value ?? "";
            result.DefaultValue = defaultValue ?? "";

            return result;
        }

        private List<string> ParseFileNameHistoryEntry(ref TokenReader tokenReader, string? path)
        {
            var beginToken = tokenReader.Read();
            if (beginToken.Type != TokenType.BeginBlock)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(beginToken.ToLocation(path), beginToken.Type)
                    .AsException();
            }

            var result = new List<string>();

            while (true)
            {
                var token = tokenReader.Read();
                if (token.Type == TokenType.StringLiteral)
                {
                    result.Add(tokenReader.GetTokenValue(in token, _stringInterner));
                }
                else if (token.Type == TokenType.EndBlock)
                {
                    break;
                }
                else
                {
                    throw DiagnosticFactory
                        .UnexpectedToken(token.ToLocation(path), token.Type)
                        .AsException();
                }
            }

            return result;
        }

        private string ReadAssignmentAndValue(ref TokenReader tokenReader, string? path)
        {
            var token = tokenReader.Read();
            if (token.Type != TokenType.Assignment)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(token.ToLocation(path), token.Type)
                    .AsException();
            }

            token = tokenReader.Read();
            if (token.Type != TokenType.StringLiteral)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(token.ToLocation(path), token.Type)
                    .AsException();
            }

            return tokenReader.GetTokenValue(in token, _stringInterner);
        }

        private void ReadStringProperty(ref string? target, in Token currentToken, ref TokenReader tokenReader, string? path)
        {
            if (target != null)
            {
                throw DiagnosticFactory
                    .UnexpectedToken(currentToken.ToLocation(path), currentToken.Type)
                    .AsException();
            }
            target = ReadAssignmentAndValue(ref tokenReader, path);
        }
    }
}
