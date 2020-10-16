using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Glacie.Metadata
{
    using RecordTypeMap = Dictionary<string, RecordType>;

    public sealed class DatabaseType
        : MetadataProvider
        , IDatabaseTypeContract
    {
        private readonly RecordTypeMap _recordTypeByNameMap;
        private readonly RecordTypeMap _recordTypeByPathMap;
        private readonly RecordTypeMap _recordTypeByTemplateNameMap;

        private readonly Path _basePath;
        private FieldVarGroup? _rootFieldVarGroup;

        #region Construction

        internal DatabaseType(Path basePath, bool caseSensitiveName, bool caseSensitivePath)
        {
            _recordTypeByNameMap = new RecordTypeMap(GetRecordMapStringComparer(caseSensitiveName));
            _recordTypeByPathMap = new RecordTypeMap(GetRecordMapPathComparer(caseSensitivePath));
            _recordTypeByTemplateNameMap = new RecordTypeMap(GetRecordMapPathComparer(caseSensitivePath));

            _basePath = basePath;
        }

        internal void Add(RecordType recordType)
        {
            recordType.AttachTo(this);

            // This will throw if already declared.
            _recordTypeByNameMap.Add(recordType.Name, recordType);
            _recordTypeByPathMap.Add(recordType.Path.ToString(), recordType);
            _recordTypeByTemplateNameMap.Add(recordType.TemplateName.ToString(), recordType);
        }

        internal void Add(FieldVarGroup fieldVarGroup)
        {
            Check.That(_rootFieldVarGroup == null);
            _rootFieldVarGroup = fieldVarGroup;
            fieldVarGroup.AttachTo(this);
        }

        #endregion

        public Path BasePath => _basePath;

        public FieldVarGroup RootFieldVarGroup => _rootFieldVarGroup!;

        public IReadOnlyCollection<RecordType> RecordTypes => _recordTypeByNameMap.Values;

        // TODO: need lookups by class

        #region IMetadataProvider

        public override DatabaseType GetDatabaseType() => this;

        public override bool TryGetRecordTypeByName(string name, [NotNullWhen(true)] out RecordType? result)
        {
            return _recordTypeByNameMap.TryGetValue(name, out result);
        }

        public override bool TryGetRecordTypeByPath(Path path, [NotNullWhen(true)] out RecordType? result)
        {
            return _recordTypeByNameMap.TryGetValue(path.ToString(), out result);
        }

        public override bool TryGetRecordTypeByTemplateName(Path templateName, [NotNullWhen(true)] out RecordType? result)
        {
            return _recordTypeByTemplateNameMap.TryGetValue(templateName.ToString(), out result);
        }

        #endregion

        internal static IEqualityComparer<string> GetRecordMapStringComparer(bool caseSensitive)
        {
            if (caseSensitive) return StringComparer.Ordinal;
            else return StringComparer.OrdinalIgnoreCase;
        }

        internal static IEqualityComparer<string> GetRecordMapPathComparer(bool caseSensitive)
        {
            if (caseSensitive) return PathComparer.Ordinal;
            else return PathComparer.OrdinalIgnoreCase;
        }
    }
}
