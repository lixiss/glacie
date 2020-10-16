using Glacie.Data;
using Glacie.Data.Templates;
using Glacie.Targeting.Infrastructure;

namespace Glacie.Targeting.TQ
{
    public class TQEngineType : EngineType
    {
        public override EngineClass EngineClass => EngineClass.TQ;

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

        public override Path1Form TemplatePathForm => throw new System.NotImplementedException();

        public override Path1Form RecordPathForm => throw new System.NotImplementedException();

        public override Path1Form ResourcePathForm => throw new System.NotImplementedException();

        public override bool IsExcludedTemplateName(Path templateName)
        {
            throw Error.NotImplemented();
        }

        protected override IPath1Mapper? CreateTemplateNameMapper()
        {
            throw Error.NotImplemented();
        }

        protected override ITemplateProcessor? CreateTemplateProcessor()
        {
            throw Error.NotImplemented();
        }
    }
}
