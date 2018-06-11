namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ForkBbEvent : BbTelemetryEvent
    {
        public string ForkName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
