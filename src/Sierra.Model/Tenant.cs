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

        [DataMember]
        [JsonIgnore]
        [NotMapped]
        public List<Fork> ForksToAdd { get; private set; }

        [DataMember]
        [JsonIgnore]
        [NotMapped]
        public List<Fork> ForksToRemove { get; private set; }

        private static ForkEqualityComparer _forkEqComparer = new ForkEqualityComparer();

        public Tenant()
        {
            CustomSourceRepos = new List<Fork>();
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

            //update Fork state
            var newStateForks = newState.CustomSourceRepos.Select(r => new Fork (r.SourceRepositoryName, Code ));

            //forks to add = new forks + those in "not created" state (remember idenmpotency)
            ForksToAdd = newStateForks.Except(CustomSourceRepos, _forkEqComparer).ToList();
            var forksNotCreated= CustomSourceRepos.Where(r => r.State == ForkState.NotCreated).ToList();

            //forks to delete = forks not referred in target state + those in "to be deleted" state
            var forksToBeDeleted = CustomSourceRepos.Where(r => r.State == ForkState.ToBeDeleted).ToList();
            ForksToRemove = CustomSourceRepos.Except(newStateForks, _forkEqComparer).ToList();
            //mark them for deletion in DB
            ForksToRemove.ForEach(f => f.State = ForkState.ToBeDeleted);

            Name = newState.Name;
            CustomSourceRepos.AddRange(ForksToAdd);
            ForksToAdd.AddRange(forksNotCreated);
            ForksToRemove.AddRange(forksToBeDeleted);

            //other states to follow
        }
    }
}
