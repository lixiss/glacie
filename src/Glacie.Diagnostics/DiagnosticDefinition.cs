using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Glacie.Diagnostics
{
    public sealed class DiagnosticDefinition : IEquatable<DiagnosticDefinition>
    {
        private readonly string _id;
        private readonly string _messageFormat;
        private readonly DiagnosticSeverity _defaultSeverity;
        private readonly string? _title;
        private readonly string? _description;
        private readonly string? _helpLinkUrl;
        private readonly string? _category;

        public DiagnosticDefinition(string id,
            string messageFormat,
            DiagnosticSeverity defaultSeverity,
            string? title = null,
            string? description = null,
            string? helpLinkUrl = null,
            string? category = null)
        {
            _id = id;
            _messageFormat = messageFormat;
            _defaultSeverity = defaultSeverity;
            _title = title;
            _description = description;
            _helpLinkUrl = helpLinkUrl;
            _category = category;
        }

        public string Id => _id;

        public string MessageFormat => _messageFormat;

        public DiagnosticSeverity DefaultSeverity => _defaultSeverity;

        public string? Title => _title;

        public string? Description => _description;

        public string? HelpLinkUrl => _helpLinkUrl;

        public string? Category => _category;

        #region Create

        public Diagnostic Create()
        {
            return Diagnostic.Create(this);
        }

        public Diagnostic Create(Location location)
        {
            return Diagnostic.Create(this, location);
        }

        //public Diagnostic Create(Location location, params object?[] messageArgs)
        //{
        //    return Diagnostic.Create(this, location, messageArgs: messageArgs);
        //}

        public Diagnostic Create(Location? location = null,
            // IEnumerable<Location>? additionalLocations = null,
            // ImmutableDictionary<string, string>? properties = null,
            params object?[]? messageArgs)
        {
            return Diagnostic.Create(this, location: location,
                // additionalLocations: additionalLocations,
                // properties: properties,
                messageArgs: messageArgs);
        }

        #endregion

        public override int GetHashCode()
        {
            return Hash.Combine(_id,
                Hash.Combine(_messageFormat,
                Hash.Combine((int)_defaultSeverity,
                Hash.Combine(_title,
                Hash.Combine(_description,
                Hash.Combine(_helpLinkUrl,
                Hash.Combine(_category, 0)))))));
        }

        public bool Equals(DiagnosticDefinition? other)
        {
            if ((object)this == other) return true;

            return other != null
                && _id == other._id
                && _messageFormat == other._messageFormat;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DiagnosticDefinition);
        }
    }
}
