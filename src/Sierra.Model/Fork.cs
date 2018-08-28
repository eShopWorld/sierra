namespace Sierra.Model
{
    using Newtonsoft.Json;
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

        public Fork(string sourceRepoName, string tenantCode)
        {
            Id = new Guid();
            SourceRepositoryName = sourceRepoName;
            TenantCode = tenantCode;
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

        [JsonIgnore]
        public BuildDefinition BuildDefinition { get; set; }       

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
        /// parses out fork object out of fork repo name
        /// 
        /// using naming structure
        /// </summary>
        /// <param name="repoName">repository name</param>
        /// <returns>parsed out fork instance</returns>
        public static Fork Parse(string repoName)
        {
            if (string.IsNullOrWhiteSpace(repoName) || !repoName.Contains(RepoTenantDelimiter) || repoName.EndsWith(RepoTenantDelimiter))
                throw new ArgumentException($"Unexpected fork repository name {repoName}");

            var lastIndex = repoName.LastIndexOf(RepoTenantDelimiter, StringComparison.Ordinal);

            return new Fork(repoName.Substring(0, lastIndex), repoName.Substring(++lastIndex));
        }

        /// <summary>
        /// custom equality compare
        /// </summary>
        /// <param name="obj">object instance to copare</param>
        /// <returns>equality check result</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Fork))
                return false;

            var objFork = (Fork)obj;
            return string.Equals(objFork.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
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
        /// <param name="newState"></param>
        public void Update(Fork newState)
        {
            if (newState == null)
                return;

            ForkVstsId = newState.ForkVstsId;
            State = newState.State;
        }
    }

  
}
