using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Eshopworld.DevOps;

namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class VstsReleaseDefinition
    {
        [DataMember]
        [Required, Key]
        public Guid Id { get; set; }

        [DataMember]
        [Required]
        public VstsBuildDefinition BuildDefinition { get; set; }

        [JsonIgnore]
        public Guid BuildDefinitionId { get; set; }

        [DataMember]
        [Required]
        public string TenantCode { get; set; }

        [DataMember]
        public TenantSize TenantSize { get; set; } 

        [DataMember]
        public EntityStateEnum State { get; set; }

        [DataMember]
        public int VstsReleaseDefinitionId { get; set; }

        [NotMapped]
        [DataMember]
        public IEnumerable<DeploymentEnvironment> SkipEnvironments { get; set; }

        [DataMember]
        public bool RingBased { get; set; }

        public VstsReleaseDefinition()
        {
            
        }

        public VstsReleaseDefinition(VstsBuildDefinition buildDefinition, string tenantCode, TenantSize tenantSize, bool ringBased)
        {
            BuildDefinition = buildDefinition;
            TenantCode = tenantCode;
            TenantSize = tenantSize;
            RingBased = ringBased;

            State = EntityStateEnum.NotCreated;
        }

        public void Update(VstsReleaseDefinition newState)
        {
            VstsReleaseDefinitionId = newState.VstsReleaseDefinitionId;
            State = newState.State;
        }

        public void UpdateWithVstsReleaseDefinition(int defId)
        {
            VstsReleaseDefinitionId = defId;
            State = EntityStateEnum.Created;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var repoLink = BuildDefinition.SourceCode.ToString();
            if (BuildDefinition.SourceCode.Fork)
                return $"{repoLink}ReleaseDefinition";

            return RingBased ? $"{repoLink}RingReleaseDefinition" : $"{repoLink}{TenantCode}NonProdReleaseDefinition";
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is VstsReleaseDefinition release))
                return false;

            return string.Equals(release.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
