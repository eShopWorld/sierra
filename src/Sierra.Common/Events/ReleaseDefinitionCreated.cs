namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ReleaseDefinitionCreated : TelemetryEvent
    {
        public string DefinitionName { get; set; }
    }
}
