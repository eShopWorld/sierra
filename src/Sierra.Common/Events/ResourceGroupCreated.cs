namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ResourceGroupCreated : TelemetryEvent
    {
        public string EnvironmentName { get; set; }

        public string ResourceGroupName { get; set; }
    }
}
