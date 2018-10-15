namespace Sierra.Common
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
    using System.Linq;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
    using Newtonsoft.Json;

    /// <summary>
    /// extensions methods for release http client
    /// </summary>
    public static class ReleaseHttpClient2Extensions
    {
        /// <summary>
        /// create or reset - delete & create definition
        /// </summary>
        /// <param name="client">extension method entry-point</param>
        /// <param name="definition">definition to process</param>
        /// <param name="targetProject">target vsts project id</param>
        /// <returns>vsts release definition model</returns>
        public static async Task<ReleaseDefinition> CreateOrResetDefinition(this ReleaseHttpClient2 client, ReleaseDefinition definition, string targetProject)
        {
            var vstsDef =
                (await client.GetReleaseDefinitionsAsync(targetProject, definition.Name, isExactNameMatch: true))
                .FirstOrDefault();

            if (vstsDef != null)
            {
                await client.DeleteReleaseDefinitionIfFExists(targetProject, vstsDef.Id, true);
            }

            //create new
            return await client.CreateReleaseDefinitionAsync(definition, targetProject);
        }

        /// <summary>
        /// get release definition specific revision
        ///
        /// NOTE that due to the current state of the REST API this must be manually parsed out
        /// </summary>
        /// <param name="client">extension method entry-point</param>
        /// <param name="targetProject">vsts target project id</param>
        /// <param name="definitionId">id of the requested definition</param>
        /// <param name="revisionId">requested revision</param>
        /// <returns>definition model</returns>
        public static async Task<ReleaseDefinition> GetReleaseDefinitionRevision(this ReleaseHttpClient2 client, string targetProject, int definitionId,
            int revisionId)
        {
            using (var defStream =
                await client.GetReleaseDefinitionRevisionAsync(targetProject, definitionId, revisionId))
            {
                using (var textReader = new JsonTextReader(new StreamReader(defStream)))
                {
                    return JsonSerializer.CreateDefault()
                        .Deserialize<ReleaseDefinition>(textReader);
                }
            }                
        }

        /// <summary>
        /// delete release definition if it exists
        /// </summary>
        /// <param name="client">extension method entry-point</param>
        /// <param name="targetProject">vsts target project id</param>
        /// <param name="definitionId">definition id</param>
        /// <param name="skipCheck">flag to skip definition check</param>
        /// <returns>task instance</returns>
        public static async Task DeleteReleaseDefinitionIfFExists(this ReleaseHttpClient2 client, string targetProject, int definitionId, bool skipCheck=false)
        {
            if (skipCheck || (await client.GetReleaseDefinitionAsync(targetProject, definitionId)) != null)
                await client.DeleteReleaseDefinitionAsync(targetProject, definitionId, forceDelete: true);
        }
    }
}
