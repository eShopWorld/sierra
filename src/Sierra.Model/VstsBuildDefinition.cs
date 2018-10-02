namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class VstsBuildDefinition
    {
        public VstsBuildDefinition()
        {
        }

        public VstsBuildDefinition(Fork sourceCode, string tenantCode)
        {
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
        public int VstsBuildDefinitionId { get; set; }

        [DataMember] 
        public EntityStateEnum State { get; set; }

        /// <summary>
        /// update <see cref="VstsBuildDefinition"/> and its state with new state as received
        /// </summary>
        /// <param name="newState">new logical state of the entity</param>
        public void Update(VstsBuildDefinition newState)
        {
            VstsBuildDefinitionId = newState.VstsBuildDefinitionId;
            State = newState.State;
        }

        /// <summary>
        /// update instance with vsts information when available
        /// </summary>
        /// <param name="vstsDefinitionId">vsts build definition id</param>
        public void UpdateWithVstsDefinition(int vstsDefinitionId)
        {
            VstsBuildDefinitionId = vstsDefinitionId;
            State = EntityStateEnum.Created;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{SourceCode}VstsBuildDefinition";
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is VstsBuildDefinition build))
                return false;

            return string.Equals(build.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
