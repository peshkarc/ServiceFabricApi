using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wbapi.Controllers.Electric
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
        public LDNHigh LDNHigh { get; set; }
        public LDNLow LDNLow { get; set; }
        public LDNSingle LDNSingle { get; set; }
    }
    public class LDNHigh
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    public class LDNLow
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    public class LDNSingle
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    public class ODNHigh
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    public class ODNLow
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    public class ODNSingle
    {
        public string start { get; set; }
        public string end { get; set; }
        public string consumption { get; set; }
    }

    
}
