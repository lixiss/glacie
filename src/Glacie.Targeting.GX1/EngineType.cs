using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Metadata;
using Glacie.Metadata.Resolvers;
using Glacie.Targeting.Generic;
using Glacie.Targeting.Infrastructure;

namespace Glacie.Targeting
{
    // TODO: Define DatabaseConventions / etc

    // We need few mappings: first is to how query resources.
    // Second mapping: how to create new resource names.

    /// <inheritdoc cref="IEngineType"/>
    public abstract class EngineType : IEngineType
    {
        internal const PathValidations CommonResourcePathValidators =
            PathValidations.Relative
            | PathValidations.Normalized
            // | PathValidations.DirectorySeparator
            | PathValidations.AsciiChars
            | PathValidations.FileNameCharacters
            // | PathValidations.LowerInvariantChars
            | PathValidations.SegmentNoLeadingWhiteSpace
            | PathValidations.SegmentNoTrailingWhiteSpace
            | PathValidations.SegmentNoTrailingDot
            | PathValidations.NoLeadingWhiteSpace;


        #region Default Factory

        private static Dictionary<EngineClass, EngineType>? s_map;

        public static EngineType GetFrom(EngineClass engineTypeId)
        {
            if (s_map == null) s_map = new Dictionary<EngineClass, EngineType>();
            if (s_map.TryGetValue(engineTypeId, out var engineType)) return engineType;
            else
            {
                engineType = CreateFrom(engineTypeId);
                s_map.Add(engineTypeId, engineType);
            }
            return engineType;
        }

        private static EngineType CreateFrom(EngineClass engineTypeId)
        {
            if (engineTypeId == EngineClass.TQ)
            {
                return new TqaeEngineType();
            }
            else if (engineTypeId == EngineClass.TQIT)
            {
                return new TqaeEngineType();
            }
            else if (engineTypeId == EngineClass.TQAE)
            {
                return new TqaeEngineType();
            }
            else if (engineTypeId == EngineClass.GD)
            {
                return new GDEngineType();
            }
            else throw Error.Argument(nameof(engineTypeId));
        }

        #endregion

        private ITemplateProcessor? _templateProcessor;
        [Obsolete("Remove it.")] private IPath1Mapper? _templateNameMapper;

        private DatabaseConventions? _databaseConventions;

        public abstract EngineClass EngineClass { get; }

        public DatabaseConventions DatabaseConventions
        {
            get
            {
                if (_databaseConventions != null) return _databaseConventions;
                return _databaseConventions = CreateDatabaseConventions();
            }
        }

        public ITemplateProcessor? GetTemplateProcessor()
        {
            if (_templateProcessor != null) return _templateProcessor;
            return (_templateProcessor = CreateTemplateProcessor());
        }

        protected abstract ITemplateProcessor? CreateTemplateProcessor();

        public abstract bool IsExcludedTemplateName(Path templateName);

        public virtual string MapRecordTypeName(string name) { return name; }

        public virtual bool TryMapRecordTypePath(Path path, [NotNullWhen(true)] out Path result)
            => UnifiedRecordPathCaseFixer.TryMapValue(path, out result);

        public Path MapRecordTypePath(Path path)
            => TryMapRecordTypePath(path, out var mappedPath) ? mappedPath : path;

        // TODO: Should resolvers really take ownership on providers? meh...
        public virtual MetadataResolver CreateMetadataResolver(MetadataProvider metadataProvider, bool disposeMetadataProvider = false)
        {
            return new DefaultMetadataResolver(metadataProvider, disposeMetadataProvider);
        }

        // TODO: -- below is unsure

        public abstract Path1Form TemplatePathForm { get; }

        public abstract Path1Form RecordPathForm { get; }

        public abstract Path1Form ResourcePathForm { get; }

        // TODO: TemplatePathForm { get; }  Metadata should use it, instead of hard-coded.

        [Obsolete("Remove it.")]
        public IPath1Mapper? GetTemplateNameMapper()
        {
            if (_templateNameMapper != null) return _templateNameMapper;
            return (_templateNameMapper = CreateTemplateNameMapper());
        }

        [Obsolete("Remove it.")]
        protected abstract IPath1Mapper? CreateTemplateNameMapper();

        protected abstract DatabaseConventions CreateDatabaseConventions();
    }
}
