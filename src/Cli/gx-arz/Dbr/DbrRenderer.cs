using System;
using System.Globalization;
using System.Text;

using Glacie.Data.Arz;

namespace Glacie.Cli.Arz
{
    internal sealed class DbrRenderer
    {
        private static readonly CultureInfo s_invariantCulture = CultureInfo.InvariantCulture;
        private static readonly string s_newLine = Environment.NewLine;

        private readonly StringBuilder _builder = new StringBuilder();

        public string Render(ArzRecord record)
        {
            _builder.Clear();

            // TODO: Need option to sort fields. templateName then rest. also option to use numerical sort.
            // However, ArzWriter also need option to sort fields, so this function may be part of ArzRecord.

            foreach (var field in record.GetAll())
            {
                _builder.Append(field.Name);
                _builder.Append(',');
                AppendField(_builder, in field);
                _builder.Append(',');
                _builder.Append(s_newLine);
            }

            var result = _builder.ToString();
            _builder.Clear();
            return result;
        }

        private static void AppendField(StringBuilder builder, in ArzField field)
        {
            var fieldCount = field.Count;
            if (fieldCount == 1)
            {
                switch (field.ValueType)
                {
                    case ArzValueType.Integer:
                        AppendInt32(builder, field.Get<int>());
                        break;

                    case ArzValueType.Real:
                        AppendFloat32(builder, field.Get<float>());
                        break;

                    case ArzValueType.String:
                        AppendString(builder, field.Get<string>());
                        break;

                    case ArzValueType.Boolean:
                        AppendBoolean(builder, field.Get<bool>());
                        break;

                    default:
                        throw Error.Unreachable();
                }
            }
            else
            {
                switch (field.ValueType)
                {
                    case ArzValueType.Integer:
                        for (var i = 0; i < fieldCount; i++)
                        {
                            if (i > 0) builder.Append(';');
                            AppendInt32(builder, field.Get<int>(i));
                        }
                        break;

                    case ArzValueType.Real:
                        for (var i = 0; i < fieldCount; i++)
                        {
                            if (i > 0) builder.Append(';');
                            AppendFloat32(builder, field.Get<float>(i));
                        }
                        break;

                    case ArzValueType.String:
                        for (var i = 0; i < fieldCount; i++)
                        {
                            if (i > 0) builder.Append(';');
                            AppendString(builder, field.Get<string>(i));
                        }
                        break;

                    case ArzValueType.Boolean:
                        for (var i = 0; i < fieldCount; i++)
                        {
                            if (i > 0) builder.Append(';');
                            AppendBoolean(builder, field.Get<bool>(i));
                        }
                        break;

                    default:
                        throw Error.Unreachable();
                }
            }
        }

        private static void AppendInt32(StringBuilder builder, int value)
        {
            builder.AppendFormat(s_invariantCulture, "{0}", value);
        }

        // TODO: Create tests for this.
        // var numbers = new float[] {0.123456789f, 0.12345678f, 0.1234567f,
        //        0.123456f, 0.12345f, 0.1234f, 0.123f, 0.12f,
        //        0.1f, 1, 12, 123, 1234, 12345, 123456, 1234567, 12345678,
        //        123456789, 1e20f };
        private static void AppendFloat32(StringBuilder builder, float value)
        {
            // Append general formatted value, and then check it.
            // If it contains only digits (e.g. it looks like integer), the add ".0" suffix.

            var pos = builder.Length;
            builder.AppendFormat(s_invariantCulture, "{0:g}", value);
            var epos = builder.Length;

            // Skip leading minus.
            if (builder[pos] == '-') pos++;

            for (; pos < epos; pos++)
            {
                if (!char.IsDigit(builder[pos]))
                {
                    // Appended string has non-digit character (e.g. decimal separator
                    // or scientific notation, so we doesn't need modify it.)
                    return;
                }
            }

            builder.Append(".0");
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append(value);
        }

        private static void AppendBoolean(StringBuilder builder, bool value)
        {
            builder.Append(value ? "1" : "0");
        }
    }
}
