namespace Sierra.Api.Controllers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    [Route("/v1/tenants")]
    public class TenantsController : Controller
    {
        // GET api/values
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Tenant>), 200)]
        public IEnumerable<Tenant> Get()
        {
            return new Tenant[] {};
        }

        // GET api/values/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Tenant), 200)]
        public Tenant Get(int id)
        {
            return new Tenant();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]Tenant tenant)
        {
        }

        // PUT api/values/5
        [HttpPut]
        public void Put([FromBody]Tenant tenant)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
        }
    }
}
