using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using vdbservices.Interfaces;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

using System.Data;
using System.Data.SqlClient;

using System.Configuration;

// You will need the following using statements 
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Auth;
using Newtonsoft.Json;
using vdb.msonline.helper;

namespace sfs
{

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class sfs : StatefulService, ICounter
    {
        public const string CacheName = "AggregationRepo";
        public static string accountName = ConfigurationManager.AppSettings["accountName"];
        public static string accountKey = ConfigurationManager.AppSettings["accountKey"];
        public static string connstring = ConfigurationManager.ConnectionStrings["vdbconn"].ConnectionString;
        public static string TableName = ConfigurationManager.AppSettings["TableName"];
        public sfs(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<long> GetCountAsync()
        {
            var myDictionary =
              await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, "Counter");
                return result.HasValue ? result.Value : 0;
            }
        }

        public async Task<AggregationRequest> Get(string sKey)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var Requests = await StateManager.GetOrAddAsync<IReliableDictionary<string, AggregationRequest>>(CacheName);
                var request = await Requests.TryGetValueAsync(tx, sKey);
                return request.HasValue ? request.Value : null;
            }
        }
        public async Task<IEnumerable<AggregationRequest>> GetAll()
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var projects = await StateManager.GetOrAddAsync<IReliableDictionary<string, AggregationRequest>>(CacheName);

                var Requests = await projects.CreateEnumerableAsync(tx);
                var result = new List<AggregationRequest>();
                using (var asyncEnumerator = Requests.GetAsyncEnumerator())
                {
                    while (await asyncEnumerator.MoveNextAsync(CancellationToken.None))
                    {
                        result.Add(asyncEnumerator.Current.Value);
                    }
                }
                return result;
            }
        }
        public async Task Save(AggregationRequest Aggreq,string sKey)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var employees = await StateManager.GetOrAddAsync<IReliableDictionary<string, AggregationRequest>>(CacheName);               
                await employees.AddOrUpdateAsync(tx, sKey, Aggreq, (key, value) => Aggreq);
                await tx.CommitAsync();
            }           
        }
        public async Task Remove(string sKey)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var projects = await StateManager.GetOrAddAsync<IReliableDictionary<string, AggregationRequest>>(CacheName);
                await projects.TryRemoveAsync(tx, sKey);
                await tx.CommitAsync();
            }
        }

        public async Task<List<AggregationEntities>> GetDetails(AggregationRequest AggReq)
        {
            // Instanciate or TableStorage class
            TableStorage t = new TableStorage(TableName);
            List<AggregationEntities> list = t.GetAll<AggregationEntities>(AggReq.ConnectionID);
            var result = list.OfType<AggregationEntities>().Where(lt => (lt.aggfromdate >= AggReq.Fromdate && lt.aggtodate <= AggReq.Todate) && lt.marketsegment == AggReq.marketsegment && lt.RowKey.StartsWith(AggReq.Aggregationtype));
            return result.ToList<AggregationEntities>();
        }
        public async Task<string> GetLastElement(string connid, string dtFrom, string MktSeg)
        {
            StorageCredentials creds = new StorageCredentials(accountName, accountKey);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);

            CloudTableClient client = account.CreateCloudTableClient();
            string tablename = (MktSeg.Trim() == "Electricity") ? "ElectricMeterReading" : (MktSeg.Trim() == "Gas") ? "GasMeterReading" : "";

            CloudTable table = client.GetTableReference(tablename);

            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, connid.ToUpper());
            //string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, DateTime.Parse(dtFrom).Ticks.ToString() + EndRowKeyAdd);
            string combinedFilter = string.Format("({0})", pkFilter);
            TableQuery<DynEnt> query = new TableQuery<DynEnt>().Where(combinedFilter).Select(new List<string> { "ReadingDateTime", "MeterReadingStatusType" });
            List<DynEnt> lstEle = table.ExecuteQuery(query)
                                            .Where(x => x.MeterReadingStatusType != "Int")
                                            .OrderByDescending(y => y.ReadingDateTime).
                                            Take(1).ToList();
            if (lstEle.Count > 0)
                return lstEle[0].ReadingDateTime.ToString();
            else
                return "Invalid";
        }
        public async Task<ElectricEntities> GetLDNDetails(string connectionId,string marketSegment,DateTime ReadingDate)
        {
            string tblname = "ElectricMeterReading";
            // Instanciate or TableStorage class
            TableStorage t = new TableStorage(tblname);
            ElectricEntities objElectric = t.GetLDNDetails<ElectricEntities>(connectionId, marketSegment, ReadingDate);
            return objElectric;
        }

        public async Task<GasEntities> GetGasDetails(string connectionId, string marketSegment, DateTime ReadingDate)
        {
            string tblname = "GasMeterReading" ;
            // Instanciate or TableStorage class
            TableStorage t = new TableStorage(tblname);
            GasEntities objElectric = t.GetGasDetails<GasEntities>(connectionId, marketSegment, ReadingDate);
            return objElectric;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>()
            {
                new ServiceReplicaListener(
                    (context) =>
                        this.CreateServiceRemotingListener(context))
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);
                    

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

   
}
