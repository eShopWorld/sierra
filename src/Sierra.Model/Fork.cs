namespace Sierra.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// message payload to define a fork that is requested
    /// </summary>
    [DataContract]
    public class Fork
    {
        private const string RepoTenantDelimiter = "-";

        public Fork()
        {
        }

        public Fork(string sourceRepoName, string tenantCode, ProjectTypeEnum projectType)
        {
            SourceRepositoryName = sourceRepoName;
            TenantCode = tenantCode;
            ProjectType = projectType;
            State = EntityStateEnum.NotCreated;
        }

        [DataMember]
        [Key]
        public Guid Id { get; set; }

        [DataMember]
        public Guid ForkVstsId { get; set; }

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
        [Required, MaxLength(6)]
        public string TenantCode { get; set; }

        [DataMember]
        public EntityStateEnum State { get; set; }

        [DataMember]
        [Required]
        public ProjectTypeEnum ProjectType { get; set; }

        /// <summary>
        /// encapsulate fork naming strategy
        /// </summary>
        /// <returns>desired fork name</returns>
        public override string ToString()
        {
            return $"{SourceRepositoryName}{RepoTenantDelimiter}{TenantCode}";
        }        

        /// <summary>
        /// custom equality compare
        /// </summary>
        /// <param name="obj">object instance to compare</param>
        /// <returns>equality check result</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Fork fork))
                return false;

            return string.Equals(fork.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// custom hash code for Fork
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// link vsts repo guid to this model instance
        /// </summary>
        /// <param name="vstsRepo">vsts repository guid</param>
        public void UpdateWithVstsRepo(Guid vstsRepo)
        {
            if (vstsRepo != Guid.Empty)
            {
                ForkVstsId = vstsRepo;
                State = EntityStateEnum.Created;
            }
        }

        /// <summary>
        /// update current instance 
        /// </summary>
        /// <param name="newState">new state</param>
        public void Update(Fork newState)
        {
            if (newState == null)
                return;

            ForkVstsId = newState.ForkVstsId;
            State = newState.State;
        }
    }
}
