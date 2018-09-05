namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    /// <summary>
    /// event to capture build definition removed
    /// </summary>
    public class BuildDefinitionDeleted : BbTelemetryEvent
    {
        /// <summary>
        /// name of the definition
        /// </summary>
        public string DefinitionName { get; set; }
    }
}
