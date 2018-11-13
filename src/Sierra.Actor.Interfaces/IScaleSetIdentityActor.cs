namespace Sierra.Actor.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Model;

    public interface IScaleSetIdentityActor : IActor
    {
        Task<ScaleSetIdentity> Add(ScaleSetIdentity scaleSetIdentity);

        Task Remove(ScaleSetIdentity scaleSetIdentity);
    }
}
