namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    /// <summary>
    /// fork deletion event 
    /// </summary>
    public class ForkDeleted : TelemetryEvent
    {
        /// <summary>
        /// name of the deleted fork
        /// </summary>
        public string ForkName { get; set; }
    }
}
