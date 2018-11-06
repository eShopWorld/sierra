namespace Sierra.Actor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell",
            "S112:General exceptions should never be thrown", Justification =
                "This test actor is used only to check actor calling code.")]
        public Task<TestItem> Add(TestItem testItem)
        {
            var name = testItem?.Name?.ToLowerInvariant();
            switch (name)
            {
                case "throw":
                    throw new Exception("Actor failure requested.");
                case "task-throw":
                    return Task.FromException<TestItem>(new Exception("Actor failure requested."));
                case "empty":
                    return Task.FromResult<TestItem>(null);
                case "delay":
                    var delay = testItem.Delay ?? TimeSpan.FromSeconds(30);
                    Thread.Sleep(delay);
                    return Task.FromResult(new TestItem { Name = "Delayed for " + delay });
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
