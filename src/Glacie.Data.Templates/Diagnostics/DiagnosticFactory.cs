using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Diagnostics;

namespace Glacie.Data.Templates
{
    internal static class DiagnosticFactory
    {
        public static Diagnostic UnexpectedEndOfStream(Location location)
        {
            return DiagnosticDefinitions.UnexpectedEndOfStream.Create(location);
        }

        public static Diagnostic UnexpectedToken(Location location, TokenType tokenType)
        {
            return DiagnosticDefinitions.UnexpectedToken.Create(location, tokenType);
        }

        public static Diagnostic UnknownToken(Location location, string? value)
        {
            return DiagnosticDefinitions.UnknownToken.Create(location, value);
        }

        public static Diagnostic PropertyRequired(Location location, string? value)
        {
            return DiagnosticDefinitions.PropertyRequired.Create(location, value);
        }
    }
}
