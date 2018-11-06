namespace Sierra.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ManagedIdentityAssignment
    {
        [DataMember]
        public string EnvironmentName { get; set; }

        [DataMember]
        public string IdentityName { get; set; }

        [DataMember]
        public string IdentityId { get; set; }

        [DataMember]
        public string ResourceGroupName { get; set; }

        [DataMember]
        public string VirtualMachineScaleSetResourceGroupName { get; set; }

        [DataMember]
        public string VirtualMachineScaleSetName { get; set; }

        [DataMember]
        public EntityStateEnum State { get; set; }
    }
}
