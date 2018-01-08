using System;
using System.Runtime.Serialization;

namespace Sierra.Model
{
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
        public string SourceRepositoryName { get; set; }

        /// <summary>
        /// suffix to apply upon the original repo name for forking
        /// </summary>
        [DataMember]
        public string ForkSuffix { get; set; }
    }
}
