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
        public Fork()
        {

        }

        public Fork(string sourceRepoName, string tenantCode)
        {
            SourceRepositoryName = sourceRepoName;
            TenantCode = tenantCode;
            State = NotCreatedState;
        }

        private const string RepoTenantDelimiter = "-";

        internal const string NotCreatedState = "NotCreated";
        internal const string CreatedState = "Created";
        internal const string ToBeDeletedState = "ToBeDeleted";

        [DataMember]
        [JsonIgnore]
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
        [JsonIgnore]       
        public string TenantCode { get; set; }

        [DataMember]
        [JsonIgnore]
        [Required, MaxLength(20)]
        public string State { get; set; }

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

            var lastIndex = repoName.LastIndexOf(RepoTenantDelimiter);                

            return new Fork (repoName.Substring(0, lastIndex), repoName.Substring(++lastIndex));            
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
            return String.Equals(objFork.ToString(), ToString(), StringComparison.OrdinalIgnoreCase);
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
            if (vstsRepo!=Guid.Empty)
            {
                ForkVstsId = vstsRepo;
                State = CreatedState;
            }
        }
    }
}
