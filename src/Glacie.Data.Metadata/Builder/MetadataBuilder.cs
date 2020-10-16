using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Glacie.Metadata.Builder
{
    using RecordTypeBuilderMap = Dictionary<string, RecordTypeBuilder>;

    public sealed class MetadataBuilder
        : Infrastructure.Builder<DatabaseType>
        , IMetadataBuilderContract
    {
        private const PathConversions InternalPathForm
            = PathConversions.Relative
            // | PathConversions.Strict
            // | PathConversions.Normalized
            | PathConversions.DirectorySeparator;

        private readonly RecordTypeBuilderMap _recordTypeMap;
        private readonly FieldVarGroupBuilder _rootFieldVarGroup;
        private int _fieldVarGroupIdGenerator;

        private Path _basePath;
        private readonly bool _isCaseSensitiveName;
        private readonly bool _isCaseSensitivePath;

        public MetadataBuilder() : this(basePath: default) { }

        public MetadataBuilder(Path basePath, bool caseSensitiveName = true, bool caseSensitivePath = false)
        {
            _isCaseSensitiveName = caseSensitiveName;
            _isCaseSensitivePath = caseSensitivePath;

            _recordTypeMap = new RecordTypeBuilderMap(DatabaseType.GetRecordMapStringComparer(caseSensitiveName));
            _rootFieldVarGroup = new FieldVarGroupBuilder(this, parent: null, name: null, system: false);

            _basePath = NormalizePath(basePath);
        }

        public bool IsCaseSensitiveName => _isCaseSensitiveName;

        public bool IsCaseSensitivePath => _isCaseSensitivePath;

        public Path BasePath
        {
            get => _basePath;
            set
            {
                ThrowIfBuilt();
                _basePath = NormalizePath(value);
            }
        }

        public bool IsEmpty => _basePath.IsEmpty && RecordTypes.Count == 0;

        public FieldVarGroupBuilder RootFieldVarGroup => _rootFieldVarGroup;

        public IReadOnlyCollection<RecordTypeBuilder> RecordTypes => _recordTypeMap.Values;

        // TODO: method should be named TryGetRecordType
        public bool TryGetRecordType(string name,
            [NotNullWhen(returnValue: true)] out RecordTypeBuilder? result)
        {
            return _recordTypeMap.TryGetValue(name, out result);
        }

        public RecordTypeBuilder GetRecordType(string name)
        {
            if (TryGetRecordType(name, out var result))
            {
                return result;
            }
            throw Error.InvalidOperation("RecordDefinitionBuilder not found: \"{0}\".", name);
        }


        //public RecordDefinitionBuilder GetRecordDefinition(NameSymbol nameSymbol)
        //{
        //    if (TryGetRecordDefinition(nameSymbol, out var result))
        //    {
        //        return result;
        //    }
        //    throw Error.InvalidOperation("RecordDefinitionBuilder not found: \"{0}\".", nameSymbol.GetValue());
        //}

        public RecordTypeBuilder DefineRecordType(string name)
        {
            ThrowIfBuilt();

            if (TryGetRecordType(name, out var existingRecordType))
            {
                if (existingRecordType.IsDefined)
                {
                    throw Error.InvalidOperation("Record type \"{0}\" already defined.", name);
                }
                else
                {
                    existingRecordType.Define();
                    return existingRecordType;
                }
            }

            var result = new RecordTypeBuilder(this, name, defined: true);
            _recordTypeMap.Add(result.Name, result);
            return result;
        }

        /// <summary>
        /// Act similarly to DefineRecordType, to get builder for referencing purposes.
        /// MetadataBuilder internally validate state if references was obtained,
        /// but record types is still not defined.
        /// </summary>
        public RecordTypeBuilder GetRecordTypeReference(string name)
        {
            if (TryGetRecordType(name, out var existingRecordType))
            {
                return existingRecordType;
            }

            ThrowIfBuilt();

            var result = new RecordTypeBuilder(this, name, defined: false);
            _recordTypeMap.Add(result.Name, result);
            return result;
        }

        public bool TryGetRecordTypeByPath(Path path, [NotNullWhen(true)] out RecordTypeBuilder? result)
        {
            // TODO: Should it be normalized (as was)?
            result = _recordTypeMap.Values
                .Where(x => Path.Equals(path, x.Path, GetPathComparison()))
                .SingleOrDefault();
            return result != null;
        }

        public bool TryGetRecordTypeByTemplateName(Path path, [NotNullWhen(true)] out RecordTypeBuilder? result)
        {
            // TODO: Should it be RelativeTo? Should it be normalized (as was)?
            var recordPath = path.TrimStart(_basePath, GetPathComparison());
            return TryGetRecordTypeByPath(recordPath, out result);
        }

        protected override DatabaseType BuildCore()
        {
            var result = new DatabaseType(_basePath, _isCaseSensitiveName, _isCaseSensitivePath);

            foreach (var x in _recordTypeMap.Values)
            {
                result.Add(x.Build());
            }

            result.Add(_rootFieldVarGroup.Build());

            return result;
        }

        internal int GetNextFieldVarGroupIdentifier()
        {
            return _fieldVarGroupIdGenerator++;
        }

        public Path NormalizePath(Path path)
        {
            return path.ConvertNonEmpty(InternalPathForm, check: true);
        }

        private PathComparison GetPathComparison()
            => _isCaseSensitivePath ? PathComparison.Ordinal : PathComparison.OrdinalIgnoreCase;
    }
}
