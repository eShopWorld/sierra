namespace Sierra.Actor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Model;
    using System.Fabric;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;
    using Common;

    [StatePersistence(StatePersistence.Volatile)]
    internal class ForkActor : Actor, IForkActor
    {
        private Config _config;        

        public ForkActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
        }

        /// <summary>
        /// for the source repository
        /// </summary>
        /// <param name="fork">request payload</param>
        /// <returns>Task instance</returns>
        public async Task ForkRepo(Fork fork)
        {
            if (!ValidateRequest(fork))
                throw new Exception("Invalid message received");

            var accessToken = await ForkUtilities.ObtainVstsAccessToken(_config.KeyVaultUrl,
                _config.KeyVaultClientId, _config.KeyVaultClientSecret, _config.VstsConfiguration);

            var repos = await ForkUtilities.ListRepos(accessToken, _config.VstsConfiguration);

            var sourceRepo = repos.Value.FirstOrDefault(i => i.Name == fork.SourceRepositoryName);
            if (sourceRepo == null)
                throw new Exception($"Repository {fork.SourceRepositoryName} not found");

            await ForkUtilities.CreateFork(fork.ForkSuffix, sourceRepo, accessToken, _config.VstsConfiguration);            
        }     

        private static bool ValidateRequest(Fork fork)
        {
            return !string.IsNullOrWhiteSpace(fork?.SourceRepositoryName) && !string.IsNullOrWhiteSpace(fork.ForkSuffix);
        }

        protected override async Task OnActivateAsync()
        {
            await LoadConfiguration();            
        }

        private async Task LoadConfiguration()
        {
            ConfigurationPackage configPackage = ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            KeyedCollection<string, ConfigurationProperty> forkSettings = configPackage.Settings.Sections["ForkActorAppConfigSection"].Parameters;
            Config newConfig = new Config
            {
                KeyVaultUrl = forkSettings["KeyVaultUrl"].Value,
                KeyVaultClientId = forkSettings["KeyVaultClientId"].Value,
                KeyVaultClientSecret = forkSettings["KeyVaultClientSecret"].Value,               
            };

            _config = newConfig;           

            _config.VstsConfiguration = await ForkUtilities.LoadVstsConfiguration(_config.KeyVaultUrl, _config.KeyVaultClientId, _config.KeyVaultClientSecret);
        }         

        private class Config
        {

            internal string KeyVaultUrl { get; set; }
            internal string KeyVaultClientId { get; set; }
            internal string KeyVaultClientSecret { get; set; }
            internal ForkUtilities.VstsConfiguration VstsConfiguration { get; set; }
        }
    }
}
