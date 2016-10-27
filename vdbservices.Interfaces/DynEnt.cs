using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vdbservices.Interfaces 
{
    public class DynEnt : TableEntity
    {
        public DynEnt(string partitionkey, string rowkey)
        {
            this.PartitionKey = partitionkey;
            this.RowKey = rowkey;
        }
        public DynEnt() { }
        public DateTime ReadingDateTime { get; set; }
        public String MeterReadingStatusType { get; set; }
    }
}
