using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vdb.msonline.helper
{
    [Serializable]
    public class AggregationRequest
    {
        public string ConnectionID { get; set; }
        public string Aggregationtype { get; set; }
        public DateTime Fromdate { get; set; }
        public DateTime Todate { get; set; }
        public string marketsegment { get; set; }
        public string RequestSource { get; set; }
    }
}
