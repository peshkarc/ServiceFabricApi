using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using vdb.msonline.helper;

namespace vdbservices.Interfaces
{
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface ICounter : IService
    {
        Task<long> GetCountAsync();        

        Task<AggregationRequest> Get(string sKey);
        Task<IEnumerable<AggregationRequest>> GetAll();
        Task Save(AggregationRequest Aggreq,string sKey);
        Task Remove(string sKey);


        Task<List<AggregationEntities>> GetDetails(AggregationRequest AggReq);
        Task<ElectricEntities> GetLDNDetails(string connectionId, string marketSegment, DateTime ReadingDate);
        Task<GasEntities> GetGasDetails(string connectionId, string marketSegment, DateTime ReadingDate);

    }

    public class ElectricEntities : TableEntity
    {
        public double LDNSingleUsage { get; set; }
        public double LDNLowUsage { get; set; }
        public double LDNHighUsage { get; set; }
    }
    public class GasEntities : TableEntity
    {
        public double LDNGasUsage { get; set; }
    }
    public class AggregationEntities : TableEntity
    {
        public DateTime aggfromdate { get; set; }
        public DateTime aggtodate { get; set; }
        public string marketsegment { get; set; }
        public double LDNUsage { get; set; }
        public double LDNHighUsage { get; set; }
        public double LDNLowUage { get; set; }
        public double StartLDNLow { get; set; }
        public double StartLDNHigh { get; set; }
        public double EndLDNLow { get; set; }
        public double EndLDNHigh { get; set; }
        public double LDNGasPositionUsage { get; set; }

    }   
}


