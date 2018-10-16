using System.Threading.Tasks;
using Autofac;
using Eshopworld.Tests.Core;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Sierra.Common;
using Sierra.Common.Tests;
using Xunit;

[Collection(nameof(CommonContainerCollection))]
public class ReleaseHttpClient2ExtensionsTests
{
    private readonly CommonContainerFixture _containerFixture;

    public ReleaseHttpClient2ExtensionsTests(CommonContainerFixture container)
    {
        _containerFixture = container;
    }

    [Fact, IsLayer1]
    public async Task CreateOrResetDefinition()
    {
        using (var scope = _containerFixture.Container.BeginLifetimeScope())
        {
            var sut = scope.Resolve<ReleaseHttpClient2>();
            var vstsConfig = scope.Resolve<VstsConfiguration>();

        }
    }
}