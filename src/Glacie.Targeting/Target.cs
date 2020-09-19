using Glacie.Data.Tpl;

namespace Glacie.Targeting
{
    public abstract class Target
    {
        private IVirtualPathMapper? _templateNameMapper;
        private ITemplateProcessor? _templateProcessor;

        public IVirtualPathMapper? GetTemplateNameMapper()
        {
            if (_templateNameMapper != null) return _templateNameMapper;
            return (_templateNameMapper = CreateTemplateNameMapper());
        }

        public ITemplateProcessor? GetTemplateProcessor()
        {
            if (_templateProcessor != null) return _templateProcessor;
            return (_templateProcessor = CreateTemplateProcessor());
        }

        protected abstract IVirtualPathMapper? CreateTemplateNameMapper();

        protected abstract ITemplateProcessor? CreateTemplateProcessor();
    }
}
