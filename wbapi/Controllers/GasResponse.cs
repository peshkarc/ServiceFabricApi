using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wbapi.Controllers.Gas
{
    //----------------------------------------------------------------------------------------------------------
    // This Entity is used for holding the output for Gas
    //----------------------------------------------------------------------------------------------------------
    public class Root
    {
        public Root(Request _request)
        {
            Request = _request;
        }
        public Request Request { get; set; }
    }
    public class Request
    {
        public string connectionId { get; set; }
        public string ClusterReference { get; set; }
        public string MarketSegment { get; set; }
        public string granularity { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public List<Consumption> consumption { get; set; }
    }
    public class Consumption
    {
        public string name { get; set; }              
      //  public LDNSingle GasUsage { get; set; }        
        public LDNSingle LDNSingle { get; set; }
    }
    public class LDNSingle
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }
}
