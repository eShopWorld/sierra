namespace Sierra.Common
{
    internal class VstsConfiguration
    {
        public string VstsTokenEndpoint { get; set; }
        public string VstsAppSecret { get; set; }
        public string VstsOAuthCallbackUrl { get; set; }
        public string VstsCollectionId { get; set; }
        public string VstsTargetProjectId { get; set; }
        public string VstsBaseUrl { get; set; }
    }
}
