using Eshopworld.DevOps;

namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class ManagedIdentity
    {
        [Key, DataMember]
        public Guid Id { get; set; }

        [DataMember]
        [MaxLength(6), Required]
        public string TenantCode { get; set; }

        [DataMember]
        public DeploymentEnvironment Environment { get; set; }

        [DataMember]
        [MaxLength(50), Required]
        public string IdentityName { get; set; }

        [DataMember]
        [MaxLength(500)]
        public string IdentityId { get; set; }

        [DataMember]
        [MaxLength(90), Required]
        public string ResourceGroupName { get; set; }

        [DataMember]
        public EntityStateEnum State { get; set; }

        /// <summary>
        /// encapsulate naming strategy
        /// </summary>
        /// <returns>desired name</returns>
        public override string ToString()
        {
            return $"{IdentityName}/{Environment}";
        }

        /// <summary>
        /// custom equality compare
        /// </summary>
        /// <param name="obj">object instance to compare</param>
        /// <returns>equality check result</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is ManagedIdentity mi))
                return false;

            return string.Equals(mi.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
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
        public void Update(ManagedIdentity newState)
        {
            if (newState == null)
                return;

            State = newState.State;
            IdentityId = newState.IdentityId;
        }
    }
}
