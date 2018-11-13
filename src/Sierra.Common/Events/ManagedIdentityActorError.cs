using System;

namespace Sierra.Common.Events
{
    using Eshopworld.Core;

    public class ManagedIdentityActorError : ExceptionEvent
    {
        public ManagedIdentityActorError(Exception exception) : base(exception)
        {
        }

        public string Stage { get; set; }

        public string SubscriptionId { get; set; }

        public string EnvironmentName { get; set; }

        public string IdentityName { get; set; }

        public string ResourceGroupName { get; set; }
    }
}
