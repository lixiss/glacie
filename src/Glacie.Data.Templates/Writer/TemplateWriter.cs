using System.Collections.Generic;

using IO = System.IO;

namespace Glacie.Data.Templates
{
    public static class TemplateWriter
    {
        public static void Write(IO.TextWriter writer, Template template)
        {
            Check.Argument.NotNull(writer, nameof(writer));
            Check.Argument.NotNull(template, nameof(template));

            var context = new WriterContext(writer);
            context.Write(template);
        }

        private struct WriterContext
        {
            private IO.TextWriter _writer;
            private int _indent;
            private bool _needIndent;

            public WriterContext(IO.TextWriter writer)
            {
                _writer = writer;
                _indent = 0;
                _needIndent = true;
            }

            public void Write(Template template)
            {
                Write(template.Root);
                WriteFileNameHistory(template.FileNameHistory);
            }

            private void Write(TemplateGroup group)
            {
                WriteToken("Group");
                WriteBeginBlock();
                WriteProperty("name", group.Name);
                WriteProperty("type", group.Type);

                foreach (var x in group.Children)
                {
                    WriteNewLine();

                    if (x is TemplateVariable v) Write(v);
                    else if (x is TemplateGroup g) Write(g);
                    else throw Error.Unreachable();
                }

                WriteEndBlock();
            }

            private void Write(TemplateVariable variable)
            {
                WriteToken("Variable");
                WriteBeginBlock();
                WriteProperty("name", variable.Name);
                WriteProperty("class", variable.Class);
                WriteProperty("type", variable.Type);
                WriteProperty("description", variable.Description);
                WriteProperty("value", variable.Value);
                WriteProperty("defaultValue", variable.DefaultValue);
                WriteEndBlock();
            }

            private void WriteFileNameHistory(List<string>? fileNameHistory)
            {
                if (fileNameHistory == null || fileNameHistory.Count == 0) return;

                WriteNewLine();
                WriteToken("fileNameHistoryEntry");
                WriteBeginBlock();
                foreach (var x in fileNameHistory)
                {
                    WriteStringLiteral(x);
                    WriteNewLine();
                }
                WriteEndBlock();
            }

            private void WriteToken(string value)
            {
                WriteIndent();
                _writer.Write(value);
            }

            private void WriteAssignment()
            {
                _writer.Write(" = ");
            }

            private void WriteBeginBlock()
            {
                WriteNewLine();
                WriteIndent();
                _writer.Write("{");
                WriteNewLine();
                _indent++;
            }

            private void WriteEndBlock()
            {
                _indent--;
                WriteIndent();
                _writer.Write("}");
                WriteNewLine();
            }

            private void WriteStringLiteral(string? value)
            {
                // TODO: (Low) (TemplateWriter) .tpl files can't have double quote inside string (there is no way to quote this character in .tpl file)
                // So, we might write some replacement, or throw exception.

                WriteIndent();
                _writer.Write('\"');
                _writer.Write(value);
                _writer.Write('\"');
            }

            private void WriteProperty(string propertyName, string value)
            {
                WriteIndent();
                WriteToken(propertyName);
                WriteAssignment();
                WriteStringLiteral(value);
                WriteNewLine();
            }

            private void WriteNewLine()
            {
                _writer.WriteLine();
                _needIndent = true;
            }

            private void WriteIndent()
            {
                if (!_needIndent) return;
                for (var i = 0; i < _indent; i++)
                {
                    _writer.Write('\t');
                }
                _needIndent = false;
            }
        }
    }
}
