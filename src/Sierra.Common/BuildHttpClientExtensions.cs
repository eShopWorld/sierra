namespace Sierra.Common
{
    using Microsoft.TeamFoundation.Build.WebApi;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// contains Sierra extensions for <see cref="BuildHttpClient"/>
    /// </summary>
    public static class BuildHttpClientExtensions
    {
        /// <summary>
        /// create or update build definition
        ///
        /// definition result
        /// </summary>
        /// <param name="buildClient">build client instance</param>
        /// <param name="definition">build definition to process</param>
        /// <param name="targetProjectId">target project to scope search for</param>
        /// <returns>definition instance</returns>
        public static async Task<BuildDefinition> CreateOrUpdateDefinition(this BuildHttpClient buildClient, BuildDefinition definition, string targetProjectId)
        {    
            //check whether fork build definition already exists
            var vstsBuildDefinition = (await buildClient.GetFullDefinitionsAsync(targetProjectId, name: definition.Name)).FirstOrDefault();

            if (vstsBuildDefinition != null)
            {
                definition.Id = vstsBuildDefinition.Id;
                definition.Url = vstsBuildDefinition.Url;
                definition.Uri = vstsBuildDefinition.Uri;
                definition.Revision = vstsBuildDefinition.Revision;

                return await buildClient.UpdateDefinitionAsync(definition, targetProjectId, vstsBuildDefinition.Id);
            }

            return await buildClient.CreateDefinitionAsync(definition);
        }

        /// <summary>
        /// remove build definition - by name - if it exists
        /// </summary>
        /// <param name="buildClient">extension method link</param>
        /// <param name="targetProjectId">id of the target project to look the definition in</param>
        /// <param name="definitionName">name of the definition to remove</param>
        /// <returns></returns>
        public static async Task DeleteDefinitionIfExists(this BuildHttpClient buildClient, string targetProjectId, string definitionName)
        {
            BuildDefinitionReference definition;

            if ((definition = (await buildClient.GetDefinitionsAsync(targetProjectId, name: definitionName)).FirstOrDefault()) != null)
                await buildClient.DeleteDefinitionAsync(targetProjectId, definition.Id);
        }
    }
}
