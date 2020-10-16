using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Glacie.Abstractions;
using Glacie.Logging;
using Glacie.Metadata.Builder;
using Glacie.Resources;
using Glacie.Targeting;

namespace Glacie.Metadata.Providers
{
    public sealed class TemplateMetadataProvider : MetadataProvider
    {
        private ResourceResolver? _resourceResolver;
        private readonly EngineType _engineType;
        private readonly Logger _log;

        private HashSet<string>? _mappedRecordTypePath;

        private MetadataBuilder _metadataBuilder;
        private TemplateMetadataReader? _metadataReader;

        public TemplateMetadataProvider(MetadataBuilder metadataBuilder, EngineType engineType, ResourceResolver resourceResolver, Logger? logger)
        {
            _metadataBuilder = metadataBuilder;
            _engineType = engineType;
            _resourceResolver = resourceResolver;
            _log = logger ?? Logger.Null;

            if (_metadataBuilder.BasePath.IsEmpty)
            {
                _metadataBuilder.BasePath = Path.Implicit("Database/Templates");
            }

            _metadataReader = new TemplateMetadataReader(_metadataBuilder,
                engineType.GetTemplateProcessor(),
                resourceExist: (templateName) =>
                {
                    return _resourceResolver.TryResolveResource(Path.Implicit(templateName), out var result);
                },
                resourceProvider: (templateName) =>
                {
                    var resource = _resourceResolver.ResolveResource(Path.Implicit(templateName)).Value;
                    Check.That(resource.Type == ResourceType.Template);
                    return resource.Open();
                },
                recordTypePathMapper: (path) =>
                {
                    if (!engineType.TryMapRecordTypePath(path, out var result))
                    {
                        if (_mappedRecordTypePath == null)
                        {
                            _mappedRecordTypePath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }

                        if (!_mappedRecordTypePath.Contains(path.ToString()))
                        {
                            _log.Warning("Record type path is not mapped: \"{0}\".", path);
                            _mappedRecordTypePath.Add(path.ToString());
                        }
                    }
                    return result;
                },
                recordTypeNameMapper: (name) => engineType.MapRecordTypeName(name));
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _resourceResolver?.Dispose();
                    _resourceResolver = null!;
                }
                base.Dispose(disposing);
            }
        }

        public MetadataBuilder PopulateMetadataBuilder()
        {
            if (_metadataReader != null)
            {
                Check.That(_resourceResolver != null);

                foreach (var x in _resourceResolver.SelectAll())
                {
                    if (_engineType.IsExcludedTemplateName(x.Path)) continue;
                    _metadataReader.Read(x.Name);
                }

                _metadataReader = null;
                _resourceResolver.Dispose();
                _resourceResolver = null;
            }

            return _metadataBuilder;
        }

        public override DatabaseType GetDatabaseType()
        {
            return PopulateMetadataBuilder().Build();
        }

        public override bool TryGetRecordTypeByName(string name, [NotNullWhen(true)] out RecordType? result)
        {
            if (_metadataReader != null)
            {
                return GetDatabaseType().TryGetRecordTypeByName(name, out result);
            }
            else
            {
                return GetDatabaseType().TryGetRecordTypeByName(name, out result);
            }
        }

        public override bool TryGetRecordTypeByPath(Path path, [NotNullWhen(true)] out RecordType? result)
        {
            if (_metadataReader != null)
            {
                return GetDatabaseType().TryGetRecordTypeByPath(path, out result);
            }
            else
            {
                return GetDatabaseType().TryGetRecordTypeByPath(path, out result);
            }
        }

        public override bool TryGetRecordTypeByTemplateName(Path templateName, [NotNullWhen(true)] out RecordType? result)
        {
            if (_metadataReader != null)
            {
                if (_metadataReader.TryRead(templateName.ToString(), out var recordTypeBuilder))
                {
                    result = recordTypeBuilder.Build();
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
            else
            {
                return GetDatabaseType().TryGetRecordTypeByTemplateName(templateName, out result);
            }
        }
    }
}
