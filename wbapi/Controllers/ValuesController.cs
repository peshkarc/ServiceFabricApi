using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using vdbservices.Interfaces;
using Microsoft.ServiceFabric.Services.Client;
using System.Net.Http;


namespace wbapi.Controllers
{
    //[System.Web.Http.RoutePrefix("api/[controller]")]
    public class ValuesController : System.Web.Http.ApiController
    {
 
        // GET api/values/5
        [HttpGet]
        [Route("api/values/{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
