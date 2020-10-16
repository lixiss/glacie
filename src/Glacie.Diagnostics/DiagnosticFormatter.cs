using System;
using System.Globalization;
using System.Text;

namespace Glacie.Diagnostics
{
    internal static class DiagnosticFormatter
    {
        public static string Format(Diagnostic diagnostic)
        {
            var definition = diagnostic.Definition;

            var builder = new StringBuilder();

            if (diagnostic.Location.Kind != LocationKind.None)
            {
                diagnostic.Location.FormatTo(builder);
                builder.Append(": ");
            }

            var severityString = diagnostic.Severity switch
            {
                DiagnosticSeverity.Hidden => "hidden",
                //DiagnosticSeverity.Conformance => "conformance",
                DiagnosticSeverity.Information => "information",
                DiagnosticSeverity.Warning => "warning",
                DiagnosticSeverity.Error => "error",
                _ => throw Error.Argument(nameof(diagnostic.Severity)),
            };

            builder.Append(severityString);
            builder.Append(' ');
            builder.Append(definition.Id);
            builder.Append(": ");

            diagnostic.FormatTo(builder);

            return builder.ToString();
        }
    }
}
