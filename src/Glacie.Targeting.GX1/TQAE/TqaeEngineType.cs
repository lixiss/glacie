using System;

using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Metadata;
using Glacie.Targeting.Infrastructure;
using Glacie.Targeting.TQAE;

namespace Glacie.Targeting
{
    public class TqaeEngineType : EngineType
    {
        public override EngineClass EngineClass => EngineClass.TQAE;

        protected override DatabaseConventions CreateDatabaseConventions()
        {
            return new DatabaseConventions(
                isCaseSensitive: false,
                outputRecordPathConversions:
                    PathConversions.Relative
                    | PathConversions.Strict
                    | PathConversions.Normalized
                    | PathConversions.AltDirectorySeparator
                    | PathConversions.LowerInvariant,
                outputRecordPathValidations:
                    EngineType.CommonResourcePathValidators
                    | PathValidations.AltDirectorySeparator
                    | PathValidations.LowerInvariantChars
                );
        }


        public override bool IsExcludedTemplateName(Path templateName)
        {
            return TqaeTemplateNameExcludeList.IsExcluded(templateName);
        }

        public override MetadataResolver CreateMetadataResolver(MetadataProvider metadataProvider, bool disposeMetadataProvider = false)
        {
            return new TqaeMetadataResolver(metadataProvider, disposeMetadataProvider);
        }

        public override Path1Form TemplatePathForm => ResourcePathForm;

        public override Path1Form RecordPathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.AltDirectorySeparator
            | Path1Form.LowerInvariant;

        public override Path1Form ResourcePathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator
            | Path1Form.LowerInvariant;

        protected override IPath1Mapper? CreateTemplateNameMapper()
        {
            return new TqaeTemplateNameMapper();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            return new TqaeTemplateProcessor();
        }

        private sealed class TqaeTemplateNameMapper : Path1Mapper
        {
            public override bool TryMap(in Path1 path, out Path1 result)
            {
                if (string.IsNullOrEmpty(path.Value))
                {
                    result = path;
                    return true;
                }

                // TODO: This should be part of TemplateNameResolver, not provider or pathmapper
                result = path
                    .TrimStart(Path1.From("custommaps/art_tqx3"), Path1Comparison.OrdinalIgnoreCase);

                //const PathForm TemplatePathForm =
                //    PathForm.Relative
                //    | PathForm.Strict
                //    | PathForm.Normalized
                //    | PathForm.DirectorySeparator
                //    | PathForm.LowerInvariant;

                //result = result.ToForm(TemplatePathForm);

                return true;
            }
        }
    }
}
