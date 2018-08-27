
namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class BuildDefinition
    {
        [DataMember]
        [Required, Key]
        public Guid Id { get; set; }


    }
}
