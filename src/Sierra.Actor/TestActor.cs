namespace Sierra.Actor
{
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Sierra.Actor.Interfaces;
    using Sierra.Model;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// An actor used for testing only.
    /// </summary>
    [StatePersistence(StatePersistence.Volatile)]
    public class TestActor : Actor, ITestActor
    {
        public TestActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public Task<TestItem> Add(TestItem testItem)
        {
            switch (testItem?.Name?.ToLowerInvariant())
            {
                case "throw":
                    throw new Exception("Actor failure requested.");
                case "taskthrow":
                    return Task.FromException<TestItem>(new Exception("Actor failure requested."));
                case "empty":
                    return Task.FromResult<TestItem>(null);
                default:
                    return Task.FromResult(new TestItem { Name = "Processed " + testItem?.Name });
            }
        }

        public Task Remove(TestItem testItem)
        {
            return Task.CompletedTask;
        }
    }
}
