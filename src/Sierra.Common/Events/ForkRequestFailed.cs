namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ForkRequestFailed : TelemetryEvent
    {
        public string ForkName { get; set; }     
        public string Message { get; set; }
    }
}
