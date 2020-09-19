using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Glacie.Diagnostics
{
    /// <summary>
    /// Represents a diagnostic, such as a error or a warning, along with the location where it occurred.
    /// </summary>
    [DebuggerDisplay("{ToString(), nq}")]
    public sealed class Diagnostic : IEquatable<Diagnostic>, IFormattable
    {
        #region Factory 

        public static Diagnostic Create(DiagnosticDefinition definition)
        {
            return new Diagnostic(definition);
        }

        public static Diagnostic Create(DiagnosticDefinition definition, params object?[] messageArgs)
        {
            return new Diagnostic(definition, messageArgs: messageArgs);
        }

        public static Diagnostic Create(DiagnosticDefinition definition, Location location)
        {
            return new Diagnostic(definition, location: location);
        }

        //public static Diagnostic Create(DiagnosticDefinition definition, Location location, params object?[] messageArgs)
        //{
        //    return new Diagnostic(definition, location: location, messageArgs: messageArgs);
        //}

        public static Diagnostic Create(DiagnosticDefinition definition,
            Location? location = null,
            // IEnumerable<Location>? additionalLocations = null,
            // ImmutableDictionary<string, string>? properties = null,
            params object?[]? messageArgs)
        {
            return new Diagnostic(definition,
                location: location,
                //additionalLocations: additionalLocations,
                // properties: properties,
                messageArgs: messageArgs);
        }

        #endregion

        private readonly DiagnosticDefinition _definition;
        private readonly Location? _location;
        private readonly object[]? _messageArgs;

        private readonly Location[]? _additionalLocations;
        private readonly ImmutableDictionary<string, string>? _properties;

        private Diagnostic(DiagnosticDefinition definition,
            Location? location = null,
            // IEnumerable<Location>? additionalLocations = null,
            // ImmutableDictionary<string, string>? properties = null,
            object[]? messageArgs = null)
        {
            _definition = definition;
            _location = location;
            // _additionalLocations = additionalLocations.ToArray();
            // _properties = properties;
            _messageArgs = messageArgs;
        }

        public DiagnosticDefinition Definition => _definition;

        public Location Location => _location ?? Location.None;

        public DiagnosticSeverity Severity => _definition.DefaultSeverity;

        public IReadOnlyList<object> Arguments => _messageArgs ?? Array.Empty<object>();

        public string GetMessage()
        {
            if (_messageArgs == null || _messageArgs.Length == 0)
            {
                return _definition.MessageFormat;
            }

            var messageFormat = _definition.MessageFormat;
            try
            {
                return string.Format(CultureInfo.InvariantCulture, messageFormat, _messageArgs);
            }
            catch (Exception)
            {
                // Invalid arguments provided.
                return messageFormat;
            }
        }

        internal void FormatTo(StringBuilder builder)
        {
            if (_messageArgs == null || _messageArgs.Length == 0)
            {
                builder.Append(_definition.MessageFormat);
                return;
            }

            var messageFormat = _definition.MessageFormat;
            try
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, messageFormat, _messageArgs);
            }
            catch (Exception)
            {
                // Invalid arguments provided.
                builder.Append(messageFormat);
            }
        }

        public DiagnosticException AsException()
        {
            return new DiagnosticException(this);
        }


        public override string ToString()
        {
            return DiagnosticFormatter.Format(this);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return DiagnosticFormatter.Format(this);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_definition.GetHashCode(),
                Hash.Combine(_location ?? Location.None,
                Hash.CombineValues(_messageArgs)));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Diagnostic);
        }

        public bool Equals(Diagnostic? other)
        {
            if (other == null) return false;

            return _definition.Equals(other._definition)
                && ArrayEqual(_messageArgs, other._messageArgs)
                && _location == other._location;
        }

        private static bool ArrayEqual(object?[]? a, object?[]? b)
        {
            if (a == b) return true;
            if (a == null) return false;
            if (b == null) return false;

            if (a.Length != b.Length) return false;

            for (var i = 0; i < a.Length; i++)
            {
                var x = a[i];
                var y = b[i];
                if (object.ReferenceEquals(x, y)) continue;

                if (!x.Equals(b)) return false;
            }

            return true;
        }
    }
}
