using Eshopworld.DevOps;

namespace Sierra.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ScaleSetIdentity
    {
        [DataMember]
        public DeploymentEnvironment Environment { get; set; }

        [DataMember]
        public string ManagedIdentityId { get; set; }
    }
}
