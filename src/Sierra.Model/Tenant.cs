﻿namespace Sierra.Model
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

        /// <summary>
        /// Forks + Core repos (when supported)
        /// </summary>
        [DataMember]
        public List<BuildDefinition> BuildDefinitions { get; set; }       

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

            var newStateForks = newState.CustomSourceRepos.Select(r => new Fork (r.SourceRepositoryName, Code ));

            //update forks and build definitions (1:1) - additions and removals
            newStateForks
                .Except(CustomSourceRepos, _forkEqComparer)
                .ToList()
                .ForEach(f =>
                {
                    f.TenantCode = Code;
                    CustomSourceRepos.Add(f);
                    BuildDefinitions.Add(new BuildDefinition(f, Code));
                });

            CustomSourceRepos
                .Except(newStateForks, _forkEqComparer)
                .ToList()
                .ForEach(f =>
                {
                    f.State = EntityStateEnum.ToBeDeleted;
                    BuildDefinitions.Single(bd => bd.SourceCode == f).State = EntityStateEnum.ToBeDeleted;
                });                                      
        }
    }
}
