namespace Sierra.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ScaleSetIdentity
    {
        [DataMember]
        public string EnvironmentName { get; set; }

        [DataMember]
        public string ManagedIdentityId { get; set; }
    }
}
