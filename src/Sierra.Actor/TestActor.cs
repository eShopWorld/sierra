namespace Sierra.Actor
{
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Interfaces;
    using Model;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S112:General exceptions should never be thrown", Justification = "This test actor is used only to check actor calling code.")]
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
