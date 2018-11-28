namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ReleaseDefinitionUpdated : TelemetryEvent
    {
        public string DefinitionName { get; set; }
    }
}
