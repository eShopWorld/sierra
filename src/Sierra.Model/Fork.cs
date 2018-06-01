namespace Sierra.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// message payload to define a fork that is requested
    /// </summary>
    [DataContract]
    public class Fork
    {
        /// <summary>
        /// source repository name (within singular collection)
        /// </summary>
        [DataMember]
        [Required]
        public string SourceRepositoryName { get; set; }

        /// <summary>
        /// suffix to apply upon the original repo name for forking
        /// </summary>
        [DataMember]
        [Required, MinLength(2)]
        public string ForkSuffix { get; set; }
    }
}
