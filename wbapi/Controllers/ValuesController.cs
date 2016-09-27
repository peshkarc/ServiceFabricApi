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
        // GET api/values
        //[HttpGet]
        //public async Task<List<ElectricEntities>> Get()
        //{
        //    // ICounter counter =
        //    var counter = ServiceProxy.Create<ICounter>(new Uri("fabric:/Vanderbronsf/sfs"), new ServicePartitionKey(1));
        //    ReportParameter rptp = new ReportParameter();
        //    rptp.ConnectionID = "C59CC3B8-FCCE-4F59-BCBA-A30E0160886E";
        //    rptp.FromDate = new DateTime(2016, 06, 01);
        //    rptp.ToDate = new DateTime(2016, 08, 31);
        //    return await counter.GetDetails(rptp);
            
        //}

        //[HttpGet]
        //[Route("{connectionId}/{type}/{start}/{end}")]
        //public string usage(string connectionId, string type, string start, string end)
        //{
        //    return connectionId + "-" + type + "-" + start + "-" + end;
        //}


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
