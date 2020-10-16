using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Targeting.GD;
using Glacie.Targeting.Infrastructure;
using Glacie.Targeting.TQAE;
using Glacie.Targeting.Unified;

namespace Glacie.Targeting
{
    public class UnifiedEngineType : EngineType
    {
        public override EngineClass EngineClass => EngineClass.Unified;

        protected override DatabaseConventions CreateDatabaseConventions()
        {
            return new DatabaseConventions(
                isCaseSensitive: false,
                outputRecordPathConversions:
                    PathConversions.Relative
                    | PathConversions.Strict
                    | PathConversions.Normalized
                    | PathConversions.DirectorySeparator
                    | PathConversions.LowerInvariant,
                outputRecordPathValidations:
                    EngineType.CommonResourcePathValidators
                    | PathValidations.DirectorySeparator
                    | PathValidations.LowerInvariantChars
                );
        }

        public override bool IsExcludedTemplateName(Path templateName)
        {
            return TqaeTemplateNameExcludeList.IsExcluded(templateName)
                || GDTemplateNameExcludeList.IsExcluded(templateName);
        }

        public override Path1Form RecordPathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator
            | Path1Form.LowerInvariant;

        public override Path1Form ResourcePathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator
            | Path1Form.LowerInvariant;

        public override Path1Form TemplatePathForm => ResourcePathForm;

        protected override IPath1Mapper? CreateTemplateNameMapper()
        {
            return new UnifiedTemplateNameMapper();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            return new UnifiedTemplateProcessor();
        }

        private sealed class UnifiedTemplateNameMapper : Path1Mapper
        {
            public override bool TryMap(in Path1 path, out Path1 result)
            {
                var value = path.Value;

                if (string.IsNullOrEmpty(path.Value))
                {
                    result = path;
                    return true;
                }

                result = path
                    .TrimStart(Path1.From("custommaps/art_tqx3"), Path1Comparison.OrdinalIgnoreCase);

                return true;
            }
        }
    }
}
