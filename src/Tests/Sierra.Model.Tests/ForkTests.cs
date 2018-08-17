using Eshopworld.Tests.Core;
using FluentAssertions;
using System;
using Xunit;

namespace Sierra.Model.Tests
{
    public class ForkTests
    {
        [Fact, IsUnit]
        public void Parse_Success()
        {
            var fork = Fork.Parse("Repo-Tenant");
            fork.SourceRepositoryName.Should().Be("Repo");
            fork.TenantCode.Should().Be("Tenant");
        }

        [Fact, IsUnit]
        public void Parse_Success2()
        {
            var fork = Fork.Parse("Repo-Middle-Tenant");
            fork.SourceRepositoryName.Should().Be("Repo-Middle");
            fork.TenantCode.Should().Be("Tenant");
        }

        [Theory, IsUnit]
        [InlineData("a")]
        [InlineData("a-")]
        [InlineData("")]
        [InlineData(null)]
        public void Parse_Failure(string input)
        {
            Assert.Throws<ArgumentException>(() => Fork.Parse(input));
        }
    }
}
