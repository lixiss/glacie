using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Targeting.GD;
using Glacie.Targeting.Infrastructure;

namespace Glacie.Targeting
{
    public class GDEngineType : EngineType
    {
        public override EngineClass EngineClass => EngineClass.GD;

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
            return GDTemplateNameExcludeList.IsExcluded(templateName);
        }

        public override Path1Form TemplatePathForm => ResourcePathForm;

        public override Path1Form RecordPathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator;
            // | PathForm.LowerInvariant;

        public override Path1Form ResourcePathForm =>
            Path1Form.Relative
            | Path1Form.Strict
            | Path1Form.Normalized
            | Path1Form.DirectorySeparator;
            // | PathForm.LowerInvariant;

        protected override IPath1Mapper? CreateTemplateNameMapper()
        {
            return new GDTemplateNameMapper();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            return new GDTemplateProcessor();
        }

        // TODO: suppress templates, because they can't be parsed (and they not used anyway):
        // "database/templates/copy of lootitemtable_dynweightdynaffix.tpl"
        // "database/templates/copy of lootitemtable_dynweighted_dynaffix.tpl"

        private sealed class GDTemplateNameMapper : Path1Mapper
        {
            public override bool TryMap(in Path1 path, out Path1 result)
            {
                var value = path.Value;

                if (string.IsNullOrEmpty(path.Value))
                {
                    result = path;
                    return true;
                }

                result = path;
                return true;
            }
        }
    }
}
