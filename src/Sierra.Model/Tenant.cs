namespace Sierra.Model
{
    using System.Linq;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
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
        public List<Fork> CustomSourceRepos { get; set; }
        
        [JsonIgnore]
        [NotMapped]
        public List<Fork> ForksToAdd { get; private set; }

        [JsonIgnore]
        [NotMapped]
        public List<Fork> ForksToRemove { get; private set; }

        /// <summary>
        /// Forks + Core repos (when supported)
        /// </summary>
        [DataMember]
        public List<BuildDefinition> BuildDefinitions { get; set; }

        [JsonIgnore]
        [NotMapped]
        public List<BuildDefinition> BuildDefinitionsToAdd { get; set; }

        [JsonIgnore]
        [NotMapped]
        public List<BuildDefinition> BuildDefinitionsToRemove { get; set; }

        private static ToStringEqualityComparer<Fork> _forkEqComparer = new ToStringEqualityComparer<Fork>();
        private static ToStringEqualityComparer<BuildDefinition> _buildDefinitionEqComparer = new ToStringEqualityComparer<BuildDefinition>();

        public Tenant()
        {
            CustomSourceRepos = new List<Fork>();
            BuildDefinitions = new List<BuildDefinition>();
        }

        public Tenant(string code):this()
        {
            Code = code;
        }

        /// <summary>
        /// project new state onto the current instance
        /// </summary>
        /// <param name="newState">new intended state</param>
        public void Update(Tenant newState)
        {
            if (newState == null)
                return;

            Name = newState.Name;

            //update Fork state
            var newStateForks = newState.CustomSourceRepos.Select(r => new Fork (r.SourceRepositoryName, Code ));

            //forks to add = new forks + those in "not created" state (remember idenmpotency)
            ForksToAdd = newStateForks.Except(CustomSourceRepos, _forkEqComparer).ToList();
            var forksNotCreated= CustomSourceRepos.Where(r => r.State == EntityStateEnum.NotCreated).ToList();
            ForksToAdd.AddRange(forksNotCreated);
            //forks to delete = forks not referred in target state + those in "to be deleted" state
            var forksToBeDeleted = CustomSourceRepos.Where(r => r.State == EntityStateEnum.ToBeDeleted).ToList();
            ForksToRemove = CustomSourceRepos.Except(newStateForks, _forkEqComparer).ToList();
            //mark them for deletion in DB
            ForksToRemove.ForEach(f => f.State = EntityStateEnum.ToBeDeleted);                             
            ForksToRemove = ForksToRemove.Union(forksToBeDeleted, _forkEqComparer).ToList();

            CustomSourceRepos.AddRange(ForksToAdd);

            //Build Definitions - 1:1 to forks
            //build definitions to add = new forks + build definitions "not created"
            BuildDefinitionsToAdd = ForksToAdd.Select(f => new BuildDefinition(f, Code)).ToList();
            BuildDefinitionsToAdd.AddRange(BuildDefinitions.Where(d => d.State == EntityStateEnum.NotCreated));
            //build definitions to remove = forks to be removed + build definitions "to be deleted"
            BuildDefinitionsToRemove = ForksToRemove.Select(f => BuildDefinitions.Single(d => d.SourceCode == f)).ToList();
            //mark the for deletion in DB
            BuildDefinitionsToRemove.ForEach(d => d.State = EntityStateEnum.ToBeDeleted);
            BuildDefinitionsToRemove = BuildDefinitionsToRemove.Union(BuildDefinitions.Where(d => d.State == EntityStateEnum.ToBeDeleted), _buildDefinitionEqComparer).ToList();

            BuildDefinitions.AddRange(BuildDefinitionsToAdd);
        }
    }
}
