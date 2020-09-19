using System;
using System.Globalization;

namespace Glacie.CommandLine.IO
{
    public static class StandardStreamWriterExtensions
    {
        public static void Write(this IStandardStreamWriter writer, string format, params object?[] args)
        {
            writer.Write(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        public static void WriteLine(this IStandardStreamWriter writer)
        {
            writer.Write(Environment.NewLine);
        }

        public static void WriteLine(this IStandardStreamWriter writer, string? value)
        {
            writer.Write(value);
            writer.Write(Environment.NewLine);
        }

        public static void WriteLine(this IStandardStreamWriter writer, string format, params object?[] args)
        {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}
