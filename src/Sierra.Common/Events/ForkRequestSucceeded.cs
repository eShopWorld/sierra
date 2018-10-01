namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ForkRequestSucceeded : TelemetryEvent
    {
        public string ForkName { get; set; }     
        
    }
}
