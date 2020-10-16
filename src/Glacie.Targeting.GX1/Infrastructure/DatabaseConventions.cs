using System;
using System.Collections.Generic;
using System.Text;

namespace Glacie.Targeting.Infrastructure
{
    // Database take multiple source databases.
    // Each record in source database treated as path in normal form.
    // This means what no additional conversions occurs, and it always behave
    // in directory-separator insensitive way.
    // This behavior internally driven by PathComparer, but configured by
    // IsCaseSensitive (which respect to PathComparer.OrdinalIgnoreCase or
    // PathComparer.Ordinal). No any normalization occurs, except which
    // PathComparer handle (PathComparer now always insensible to directory
    // separators (even if there is series of separators, it handle them), but
    // PathComparer doesn't do any normalization, so relative paths will behave
    // as distinct records. However, this is good, because references can be in
    // almost any format, except what they almost always are in normal form.
    // E.g. Database when linking records rely not on normalization, but just
    // on PathComparer, which is configurable by IsCaseSensitive.
    // No more adjustments it do.
    //
    // OutputPathConversions is resposible how database's record name will be
    // converted when turned into ArzDatabase.
    //
    // OutputPathValidations is responsible how database validate output record
    // names. Source record's also validated, but there is responsiblity of
    // source, because they are might be not equal, but DatabaseConventions is
    // preferred.
    //
    // This type is not inteded to used directly, unless you create own/custom
    // EngineType.

    // TODO: Add PathConventions => e.g. how paths referenced across all forms.

    public sealed class DatabaseConventions
    {
        public DatabaseConventions(bool isCaseSensitive,
            PathConversions outputRecordPathConversions,
            PathValidations outputRecordPathValidations)
        {
            IsCaseSensitive = isCaseSensitive;
            OutputRecordPathConversions = outputRecordPathConversions;
            OutputRecordPathValidations = outputRecordPathValidations;
        }

        public bool IsCaseSensitive { get; }

        public PathConversions OutputRecordPathConversions { get; }

        public PathValidations OutputRecordPathValidations { get; }
    }
}
