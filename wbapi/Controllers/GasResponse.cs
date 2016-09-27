using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wbapi.Controllers.Gas
{
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
        public string GasUsage { get; set; }
    }
}
