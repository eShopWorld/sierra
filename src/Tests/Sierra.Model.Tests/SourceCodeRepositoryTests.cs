using Eshopworld.Tests.Core;
using FluentAssertions;
using Sierra.Model;
using Xunit;

// ReSharper disable once CheckNamespace
public class SourceCodeRepositoryTests
{
    [Fact, IsUnit]
    public void ForkRepo_ToStringTest()
    {
        var sut = new SourceCodeRepository { SourceRepositoryName = "SourceRepoName", TenantCode = "TNT", Fork = true };
        sut.ToString().Should().Be("SourceRepoName-TNT");
    }

    [Fact, IsUnit]
    public void StandardComponent_ToStringTest()
    {
        var sut = new SourceCodeRepository { SourceRepositoryName = "SourceRepoName", TenantCode = "TNT", Fork = false };
        sut.ToString().Should().Be("SourceRepoName");
    }
}
