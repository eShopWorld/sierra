namespace Sierra.Model
{
    using System.ComponentModel.DataAnnotations;

    public class Tenant
    {
        [Key]
        [Required, MaxLength(4)]
        public string Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }
    }
}
