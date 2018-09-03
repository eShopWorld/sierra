namespace Sierra.Common
{
    public class VstsConfiguration
    {
        public string VstsCollectionId { get; set; }

        public string VstsTargetProjectId { get; set; }

        public string VstsBaseUrl { get; set; }

        public string VstsPat { get; set; }

        public BuildDefinitionConfig WebApiBuildDefinitionTemplate { get; set; }
    }

    public class BuildDefinitionConfig
    {
        public int DefinitionId { get; set; }
        public int RevisionId { get; set; }
    }
}
