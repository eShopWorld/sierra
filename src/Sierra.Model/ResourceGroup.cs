namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Message payload to specify a resource group to be created or deleted.
    /// </summary>
    [DataContract]
    public class ResourceGroup
    {
        public ResourceGroup()
        {
        }

        public ResourceGroup(string tenantCode, string environmentName, string name)
        {
            TenantCode = tenantCode;
            EnvironmentName = environmentName;
            Name = name;
        }

        [Key, DataMember]
        public Guid Id { get; set; }

        [Required, MaxLength(6), DataMember]
        public string TenantCode { get; set; }

        [Required, DataMember]
        public string EnvironmentName { get; set; }

        [Required, DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ResourceId { get; set; }

        [DataMember]
        public EntityStateEnum State { get; set; }

        /// <summary>
        /// encapsulate naming strategy
        /// </summary>
        /// <returns>desired name</returns>
        public override string ToString()
        {
            return $"{Name}/{EnvironmentName}/{TenantCode}";
        }

        /// <summary>
        /// custom equality compare
        /// </summary>
        /// <param name="obj">object instance to compare</param>
        /// <returns>equality check result</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is ResourceGroup fork))
                return false;

            return string.Equals(fork.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// custom hash code
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// update current instance 
        /// </summary>
        /// <param name="newState">new state</param>
        public void Update(ResourceGroup newState)
        {
            if (newState == null)
                return;

            State = newState.State;
            ResourceId = newState.ResourceId;
        }
    }
}
