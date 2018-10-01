namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    /// <summary>
    /// Defines a common contract for the TenantActor.
    /// </summary>
    public interface ITestActor : IActor
    {
        /// <summary>
        /// Simulates the Add method.
        /// </summary>
        /// <param name="testItem"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task<TestItem> Add(TestItem testItem);

        /// <summary>
        /// Simulates the Remove method
        /// </summary>
        /// <param name="testItem"></param>
        /// <returns>The async <see cref="Task"/> wrapper.</returns>
        Task Remove(TestItem testItem);
    }
}
