namespace Sierra.Model
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Runtime.Serialization;

    [DataContract]
    public class Tenant
    {
        [DataMember]
        [Key, Required, MaxLength(6)]
        public string Code { get; set; }

        [DataMember]
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [DataMember]
        public List<SourceCodeRepository> SourceRepos { get; set; }

        /// <summary>
        /// Forks + Core repos (when supported)
        /// </summary>
        [DataMember]
        public List<VstsBuildDefinition> BuildDefinitions { get; set; }

        [DataMember]
        public List<VstsReleaseDefinition> ReleaseDefinitions { get; set; }

        [DataMember]
        public List<ResourceGroup> ResourceGroups { get; set; }

        [DataMember]
        public List<ManagedIdentity> ManagedIdentities { get; set; }

        private static readonly ToStringEqualityComparer<SourceCodeRepository> ForkEqComparer = new ToStringEqualityComparer<SourceCodeRepository>();

        public Tenant()
        {
            SourceRepos = new List<SourceCodeRepository>();
            BuildDefinitions = new List<VstsBuildDefinition>();
            ReleaseDefinitions = new List<VstsReleaseDefinition>();
            ResourceGroups = new List<ResourceGroup>();
            ManagedIdentities = new List<ManagedIdentity>();
        }

        public Tenant(string code) : this()
        {
            Code = code;
        }

        /// <summary>
        /// project new state onto the current instance
        /// </summary>
        /// <param name="newState">new intended state</param>
        /// <param name="prodEnvName">name of the production environment</param>
        public void Update(Tenant newState, IEnumerable<string> environments, string prodEnvName = "PROD")
        {
            if (newState == null)
                return;

            Name = newState.Name;

            var newStateForks = newState.SourceRepos.Select(r => new SourceCodeRepository(r.SourceRepositoryName, Code, r.ProjectType, r.Fork)).ToList();

            //update forks and build definitions (1:1) - additions and removals
            newStateForks
                .Except(SourceRepos, ForkEqComparer)
                .ToList()
                .ForEach(f =>
                {
                    f.TenantCode = Code;
                    SourceRepos.Add(f);
                    var bd = new VstsBuildDefinition(f, Code);
                    BuildDefinitions.Add(bd);
                    var rd = new VstsReleaseDefinition(bd, Code);
                    if (!f.Fork)
                        rd.SkipEnvironments = new[] {prodEnvName}; //for canary, no PROD env in non prod release pipeline

                    ReleaseDefinitions.Add(rd);
                });

            SourceRepos
                .Except(newStateForks, ForkEqComparer)
                .ToList()
                .ForEach(f =>
                {
                    f.State = EntityStateEnum.ToBeDeleted;
                    var bd = BuildDefinitions.Single(b => Equals(b.SourceCode, f));
                    bd.State = EntityStateEnum.ToBeDeleted;
                    bd.ReleaseDefinition.State = EntityStateEnum.ToBeDeleted;
                });

            var environmentList = environments.ToList();
            if (!ResourceGroups.Any())
            {
                foreach (var environmentName in environmentList)
                {
                    // TODO: validate that the list of resource groups and their names
                    ResourceGroups.Add(new ResourceGroup(Code, environmentName, $"checkout-{Code}-{environmentName}"));
                }
            }

            if (!ManagedIdentities.Any())
            {
                foreach (var environmentName in environmentList)
                {
                    // TODO: validate the list of created managed identities and their names
                    ManagedIdentities.Add(new ManagedIdentity
                    {
                        TenantCode = Code,
                        EnvironmentName = environmentName,
                        IdentityName = $"{Code}-{environmentName}",
                        ResourceGroupName = $"checkout-{Code}-{environmentName}",
                    });
                }
            }
        }
    }
}
