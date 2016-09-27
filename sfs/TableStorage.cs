using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using vdbservices.Interfaces;
namespace sfs
{

    

    /// <summary>
    /// Simple Table storage generic helper class
    /// </summary>
    public class TableStorage
    {
        /// <summary>
        /// Handle to the Azure table.
        /// </summary>
        /// <value>
        /// The table.
        /// </value>
        private CloudTable table { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableStorage"/> class.
        /// </summary>
        /// <param name="TableName">Name of the table.</param>
        public TableStorage(string TableName)
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            this.table = tableClient.GetTableReference(TableName);
            table.CreateIfNotExists();
        }

        /// <summary>
        /// Inserts the specified data.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="data">The data.</param>
        public void Insert<T>(T data) where T : TableEntity
        {
            // Create the TableOperation that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(data);

            // Execute the insert operation.
            this.table.Execute(insertOperation);
        }

        /// <summary>
        /// Inserts a list of table entries as a batch.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="data">The data.</param>
        public void InsertBatch<T>(List<T> data) where T : TableEntity
        {
            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Add both customer entities to the batch insert operation.
            foreach (TableEntity d in data)
            {
                batchOperation.Insert(d);
            }

            // Execute the batch operation.
            table.ExecuteBatch(batchOperation);
        }

        /// <summary>
        /// Gets all data corresponding to a partition key.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="PartitionKey">The partition key.</param>
        /// <returns>A list of T that has the corresponding partion key</returns>
        public List<T> GetAll<T>(string PartitionKey) where T : TableEntity
        {
            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<AggregationEntities> query = new TableQuery<AggregationEntities>().Where(
              TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey));

            List<T> Results = new List<T>();
            // Print the fields for each customer.
            foreach (AggregationEntities entity in table.ExecuteQuery(query))
            {
                Results.Add(entity as T);
                //Console.WriteLine("{0}, {1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey,
                //    entity.Email, entity.PhoneNumber);
            }
            return Results;
        }

        /// <summary>
        /// Gets the single.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="PartitionKey">The partition key.</param>
        /// <param name="RowKey">The row key.</param>
        /// <returns></returns>
        public T GetSingle<T>(string PartitionKey, string RowKey) where T : TableEntity
        {

            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            T result = null;
            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                result = retrievedResult.Result as T;
            }
            return result;
        }

        public ElectricEntities GetLDNDetails<T>(string connectionId, string marketSegment, DateTime ReadingDate)
        {
            List<ElectricEntities> list = new List<ElectricEntities>();
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, connectionId.ToUpper());
            string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, ReadingDate.Ticks.ToString() + "0000");
            string rkUpperFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, ReadingDate.Ticks.ToString() + "9999");

            string combinedFilter = string.Format("({0}) {1} ({2}) {3} ({4})", pkFilter, TableOperators.And, rkLowerFilter, TableOperators.And, rkUpperFilter);

            TableQuery<ElectricEntities> query = new TableQuery<ElectricEntities>().Where(combinedFilter);
            foreach (ElectricEntities entity in table.ExecuteQuery(query))
            {

                list.Add(entity);
            }

            return (list.Count > 0) ? list[0] : new ElectricEntities();
        }

        public GasEntities GetGasDetails<T>(string connectionId, string marketSegment, DateTime ReadingDate)
        {
            List<GasEntities> list = new List<GasEntities>();
            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, connectionId.ToUpper());
            string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, ReadingDate.Ticks.ToString() + "0000");
            string rkUpperFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, ReadingDate.Ticks.ToString() + "9999");

            string combinedFilter = string.Format("({0}) {1} ({2}) {3} ({4})", pkFilter, TableOperators.And, rkLowerFilter, TableOperators.And, rkUpperFilter);

            TableQuery<GasEntities> query = new TableQuery<GasEntities>().Where(combinedFilter);
            foreach (GasEntities entity in table.ExecuteQuery(query))
            {

                list.Add(entity);
            }

            return (list.Count > 0) ? list[0] : new GasEntities();
        }
        /// <summary>
        /// Replaces the specified partition key.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="PartitionKey">The partition key.</param>
        /// <param name="RowKey">The row key.</param>
        /// <param name="ReplacementData">The replacement data.</param>
        /// <param name="InsertOrReplace">The insert O replace.</param>
        public void Replace<T>(string PartitionKey, string RowKey,
               T ReplacementData, Boolean InsertOrReplace) where T : TableEntity
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(PartitionKey, RowKey);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity object.
            T updateEntity = retrievedResult.Result as T;

            if (updateEntity != null)
            {
                ReplacementData.PartitionKey = updateEntity.PartitionKey;
                ReplacementData.RowKey = updateEntity.RowKey;

                // Create the InsertOrReplace TableOperation
                TableOperation updateOperation;
                if (InsertOrReplace)
                {
                    updateOperation = TableOperation.InsertOrReplace(ReplacementData);
                }
                else
                {
                    updateOperation = TableOperation.Replace(ReplacementData);
                }

                // Execute the operation.
                table.Execute(updateOperation);

                Console.WriteLine("Entity updated.");
            }

            else
                Console.WriteLine("Entity could not be retrieved.");
        }

        /// <summary>
        /// Deletes the entry.
        /// </summary>
        /// <typeparam name="T">DTO that inherits from TableEntity</typeparam>
        /// <param name="PartitionKey">The partition key.</param>
        /// <param name="RowKey">The row key.</param>
        /// <param name="ReplacementData">The replacement data.</param>
        public void DeleteEntry<T>(string PartitionKey, string RowKey, T ReplacementData) where T : TableEntity
        {

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(PartitionKey, RowKey);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            T deleteEntity = retrievedResult.Result as T;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                Console.WriteLine("Entity deleted.");
            }

            else
                Console.WriteLine("Could not retrieve the entity.");

        }

        /// <summary>
        /// Deletes the table.
        /// </summary>
        public void DeleteTable()
        {
            // Delete the table it if exists.
            table.DeleteIfExists();
        }
    }
}