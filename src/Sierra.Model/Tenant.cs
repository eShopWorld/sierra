namespace Sierra.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    [DataContract]
    public class Tenant
    {
        [DataMember]
        [Key, Required, MaxLength(6)]
        public string Id { get; set; }

        [DataMember]
        [Required, MaxLength(100)]
        public string Name { get; set; }
    }
}
