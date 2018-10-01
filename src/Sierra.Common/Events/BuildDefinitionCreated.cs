namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    /// <summary>
    /// event to capture build definition being created
    /// </summary>
    public class BuildDefinitionCreated : TelemetryEvent
    {
        /// <summary>
        /// pipeline name
        /// </summary>
        public string DefinitionName { get; set; }       
    }
}
