namespace Sierra.Common
{
    public class VstsConfiguration
    {
        public string VstsCollectionId { get; set; }

        public string VstsTargetProjectId { get; set; }

        public string VstsBaseUrl { get; set; }

        public string VstsPat { get; set; }

        public PipelineDefinitionConfig WebApiBuildDefinitionTemplate { get; set; }

        // ReSharper disable once InconsistentNaming
        public PipelineDefinitionConfig WebUIBuildDefinitionTemplate { get; set; }

        public PipelineDefinitionConfig WebApiReleaseDefinitionTemplate { get; set; }

        // ReSharper disable once InconsistentNaming
        public PipelineDefinitionConfig WebUIReleaseDefinitionTemplate { get; set; }

        public PipelineDefinitionConfig WebApiRingReleaseDefinitionTemplate { get; set; }

        // ReSharper disable once InconsistentNaming
        public PipelineDefinitionConfig WebUIRingReleaseDefinitionTemplate { get; set; }
    }

    public class PipelineDefinitionConfig
    {
        public int DefinitionId { get; set; }
        public int RevisionId { get; set; }
    }
}
