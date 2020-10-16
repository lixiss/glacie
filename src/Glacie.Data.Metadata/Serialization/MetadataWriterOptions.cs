namespace Glacie.Metadata.Serialization
{
    public sealed class MetadataWriterOptions
    {
        /// <summary>
        /// Selects which document to write, GXMD or GXMP.
        /// </summary>
        public bool EmitPatchBoilerplate { get; set; }

        public bool? OmitXmlDeclaration { get; set; }
        public bool? UseXmlNamespace { get; set; }

        public bool? EmitRootVarGroup { get; set; }
        public bool? EmitVarGroups { get; set; }
        public bool? IncludeOnlyVarProperties { get; set; }
        public bool? ExcludeVarProperties { get; set; }
        public bool? EmitVarPropertyAsAttribute { get; set; }
    }
}
