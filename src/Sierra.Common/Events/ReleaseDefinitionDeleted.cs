namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ReleaseDefinitionDeleted : TelemetryEvent
    {
        public string DefinitionName { get; set; }
    }
}
