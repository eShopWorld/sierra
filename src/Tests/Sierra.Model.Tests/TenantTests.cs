using Eshopworld.Tests.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Sierra.Model.Tests
{
    public class TenantTests
    {
        [Fact, IsUnit]
        public void Update()
        {
            var currentTenant = new Tenant();
            currentTenant.Code = "TenantA";
            currentTenant.Name = "oldName";
            //currentTenant.CustomSourceRepos = new { new Fork{SourceRepositoryName = "RepoA", TenantId = "TenantA", TenantName = ""}
        }
    }
}
