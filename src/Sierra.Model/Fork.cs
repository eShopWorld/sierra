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
        public string TenantName { get; set; }

        /// <summary>
        /// encapsulate fork naming strategy
        /// </summary>
        /// <returns>desired fork name</returns>
        public override string ToString()
        {
            return $"{SourceRepositoryName}{RepoTenantDelimiter}{TenantName}";
        }

        /// <summary>
        /// parses out fork object out of fork repo name
        /// 
        /// using naming structure
        /// </summary>
        /// <param name="repoName">repository name</param>
        /// <returns></returns>
        public static Fork Parse(string repoName)
        {
            if (string.IsNullOrWhiteSpace(repoName) || !repoName.Contains(RepoTenantDelimiter) || repoName.EndsWith(RepoTenantDelimiter))
                throw new ArgumentException($"Unexpected fork repository name {repoName}");

            var lastIndex = repoName.LastIndexOf(RepoTenantDelimiter);                

            return new Fork { SourceRepositoryName = repoName.Substring(0, lastIndex), TenantName = repoName.Substring(++lastIndex) };            
        }
    }
}
