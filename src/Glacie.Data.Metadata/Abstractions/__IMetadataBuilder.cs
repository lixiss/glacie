using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata.Builder
{
    /// <summary>
    /// Underlying implementations may resolve record types lazily and return
    /// not attached record type(s), when accessed by via TryGetRecordType or
    /// GetRecordType methods (even when GetRecordType().Build() method is
    /// called, returned record will not change own identity).
    /// Call into GetDatabaseType ensures what all metadata completely read.
    ///
    /// Returned entities not needed to be completely valid (however at this
    /// stage record types had established identity and doesn't change it). Rest
    /// validation occurs only when GetDatabaseType().Build() will be called.
    ///
    /// This interface for implementing by actual implementations from
    /// Glacie.Metadata assembly.
    ///
    /// Record.Name / get by name values and respect to record declaration
    /// (always case-sensitive). There is primary record's identity.
    ///
    /// RecordType.Path also always stored and accessed as it declared.
    /// However actual metadata builders may implement own rules.
    ///
    /// Normal RecordType.Path value is something like, "Actor.tpl"
    /// or "actor.tpl": there is different paths, and both of them are not
    /// prefixed with "database/templates/".
    ///
    /// Resolving templateName to RecordType is responsibility of resolver, by
    /// in-game rules. Note, that, RecordType also may store templateName field
    /// in desired form, and/or with special type to assist validation
    /// or conformance checker or optimization code to process field in a unified
    /// way.
    /// </summary>
    internal interface __IMetadataBuilder
    {
        MetadataBuilder GetDatabaseType();

        bool TryGetRecordTypeByName(string name, [NotNullWhen(true)] out RecordTypeBuilder? result);
        RecordTypeBuilder GetRecordTypeByName(string name);

        bool TryGetRecordTypeByPath(in Path1 path, [NotNullWhen(true)] out RecordTypeBuilder? result);
        RecordTypeBuilder GetRecordTypeByPath(in Path1 path);
    }
}
