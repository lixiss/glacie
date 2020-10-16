using Glacie.Data.Arz;
using Glacie.Data.Templates;
using Glacie.Logging;

namespace Glacie.Metadata.V1
{
    public sealed class MetadataProviderFactoryOptions
    {
        public ArzReaderOptions? ArzReaderOptions { get; set; }

        public Logger? Logger { get; set; }

        /// <summary>
        /// Allow to remap input path into other path.
        /// Useful to remap paths like: %TEMPLATE_DIR%\some\some.tpl
        /// or CustomMaps\Art_TQX3\some.tpl to another (standard location).
        /// </summary>
        public IPath1Mapper? TemplateNameMapper { get; set; }

        /// <summary>
        /// Allow modify template (to fixup some errors).
        /// </summary>
        public ITemplateProcessor? TemplateProcessor { get; set; }
    }
}
