namespace Sierra.Common.Tests
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Eshopworld.Tests.Core;
    using FluentAssertions;
    using Microsoft.TeamFoundation.Build.WebApi;
    using Xunit;

    [Collection(nameof(CommonContainerCollection))]
    public class BuildHttpClientExtensionsTests
    {
        private readonly CommonContainerFixture _containerFixture;            

        public BuildHttpClientExtensionsTests(CommonContainerFixture container)
        {
            _containerFixture = container;
        }

        [Fact, IsLayer1]
        public async Task CreateOrUpdateDefinition_Success()
        {
            using (var scope = _containerFixture.Container.BeginLifetimeScope())
            {
                var buildHttpClient = scope.Resolve<BuildHttpClient>();
                var vstsConfig = scope.Resolve<VstsConfiguration>();

                var template = await buildHttpClient.GetDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    vstsConfig.WebApiBuildDefinitionTemplate.RevisionId);

                var pipelineName = $"CIPipeline{Guid.NewGuid().ToString()}";
                const string repositoryId = "2c71775d-25a9-4e16-b880-9880cc8b9f1c";
                template.Repository.Id = repositoryId;
                template.Name = pipelineName;

                var result = await buildHttpClient.CreateOrUpdateDefinition(template, vstsConfig.VstsTargetProjectId);
                result.Should().NotBeNull();
                
                //check the list of all pipelines
                var list = await buildHttpClient.GetFullDefinitionsAsync(project: vstsConfig.VstsTargetProjectId);
                list.Should().ContainSingle(p => p.Name == pipelineName && p.Repository.Id == repositoryId);
                //delete
                await buildHttpClient.DeleteDefinitionAsync(vstsConfig.VstsTargetProjectId, result.Id);
            }
        }

        [Fact, IsLayer1]
        public async Task DeleteDefinitionIfExists_Success()
        {
            using (var scope = _containerFixture.Container.BeginLifetimeScope())
            {
                var buildHttpClient = scope.Resolve<BuildHttpClient>();
                var vstsConfig = scope.Resolve<VstsConfiguration>();

                var template = await buildHttpClient.GetDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    vstsConfig.WebApiBuildDefinitionTemplate.DefinitionId,
                    vstsConfig.WebApiBuildDefinitionTemplate.RevisionId);

                var pipelineName = $"CIPipeline{Guid.NewGuid().ToString()}";
                const string repositoryId = "2c71775d-25a9-4e16-b880-9880cc8b9f1c";
                template.Repository.Id = repositoryId;
                template.Name = pipelineName;

                var result = await buildHttpClient.CreateOrUpdateDefinition(template, vstsConfig.VstsTargetProjectId);
                result.Should().NotBeNull();

                await buildHttpClient.DeleteDefinitionIfExists(vstsConfig.VstsTargetProjectId, result.Name);
                //confirm deletion
                (await buildHttpClient.GetFullDefinitionsAsync(project: vstsConfig.VstsTargetProjectId)).Should()
                    .NotContain(d => d.Name == result.Name);
            }
        }

        [Fact, IsLayer1]
        public async Task DeleteDefinitionIfExists_NonExistentDefinition()
        {
            using (var scope = _containerFixture.Container.BeginLifetimeScope())
            {
                var buildHttpClient = scope.Resolve<BuildHttpClient>();
                var vstsConfig = scope.Resolve<VstsConfiguration>();

                await buildHttpClient.DeleteDefinitionIfExists(vstsConfig.VstsTargetProjectId, Guid.NewGuid().ToString());
            }
        }
    }
}
