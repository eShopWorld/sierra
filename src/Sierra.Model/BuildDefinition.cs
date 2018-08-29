
namespace Sierra.Model
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class BuildDefinition
    {
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
    }
}
