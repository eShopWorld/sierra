namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ForkRequestSucceeded : BbTelemetryEvent
    {
        public string ForkName { get; set; }     
        
    }
}
