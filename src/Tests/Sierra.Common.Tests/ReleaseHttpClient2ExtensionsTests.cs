using System;
using System.Threading.Tasks;
using Sierra.Common;
using Autofac;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Sierra.Common.Tests;
using Xunit;

[Collection(nameof(CommonContainerCollection))]
// ReSharper disable once CheckNamespace
public class ReleaseHttpClient2ExtensionsTests
{
    private readonly CommonContainerFixture _containerFixture;

    public ReleaseHttpClient2ExtensionsTests(CommonContainerFixture container)
    {
        _containerFixture = container;
    }

    [Fact, IsLayer1]
    public async Task FlowTest()
    {
        using (var scope = _containerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<ReleaseHttpClient2>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();
            ReleaseDefinition rd = null;

            try
            {
                var template = await sut.GetReleaseDefinitionAsync(vstsConfig.VstsTargetProjectId,
                    vstsConfig.WebApiReleaseDefinitionTemplate.DefinitionId);

                template.Name = Guid.NewGuid().ToString();

                rd = await sut.CreateOrResetDefinition(template, vstsConfig.VstsTargetProjectId);

                //asserts
                var rds = await sut.GetReleaseDefinitionsAsync(vstsConfig.VstsTargetProjectId, template.Name);
                rds.Should().ContainSingle();
            }
            finally
            {
                if (rd != null)
                    await sut.DeleteReleaseDefinitionIfFExists(vstsConfig.VstsTargetProjectId, rd.Name);
            }
        }
    }
}