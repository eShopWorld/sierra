namespace Sierra.Model
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class BuildDefinition
    {
        public BuildDefinition()
        {

        }

        public BuildDefinition(Fork sourceCode, string tenantCode)
        {
            Id = new Guid();
            SourceCode = sourceCode;
            TenantCode = tenantCode;
            State = EntityStateEnum.NotCreated;
        }

        [DataMember]
        [Required, Key]
        public Guid Id { get; set; }

        [DataMember]
        [Required]
        public Fork SourceCode { get; set; }
        
        [DataMember]
        [Required] 
        public string TenantCode { get; set; }

        [DataMember]
        public string VstsBuildDefinitionId { get; set; }

        [DataMember] 
        public EntityStateEnum State { get; set; }

        /// <summary>
        /// update <see cref="BuildDefinition"/> and its state with new state as received
        /// </summary>
        /// <param name="newState">new logical state of the entity</param>
        public void Update(BuildDefinition newState)
        {
            VstsBuildDefinitionId = newState.VstsBuildDefinitionId;
            State = newState.State;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{SourceCode.ToString()}BuildDefinition";
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is BuildDefinition))
                return false;

            var bd = (BuildDefinition)obj;
            return string.Equals(bd.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
        }

    }
}
