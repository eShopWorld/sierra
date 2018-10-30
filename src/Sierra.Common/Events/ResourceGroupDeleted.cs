namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ResourceGroupDeleted : TelemetryEvent
    {
        public string EnvironmentName { get; set; }

        public string ResourceGroupName { get; set; }
    }
}
