using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;

namespace TestSendRequestQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            string sConnectionString = "Endpoint=sb://namevdbanalyticssb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=iPG1toRxfEuo8JD2Hye7DzX/nnfJUtwvBn8Q5znceKY=";
            string sQueueName = "aggregationsbqueue";
            try
            {
                AggregationRequest req = new AggregationRequest();
                req.ConnectionID = "0D0E9A6E-05CA-44D6-B03E-A57C0144D31B";
                req.Aggregationtype = "D";                
                req.Fromdate = Convert.ToDateTime("03/24/2016 12:00:00 AM");
                req.Todate = Convert.ToDateTime("03/24/2016 11:45:00 PM");
                req.marketsegment = "Electricity";

                // Create the queue if it does not exist already             
                var namespaceManager = NamespaceManager.CreateFromConnectionString(sConnectionString);
                if (!namespaceManager.QueueExists(sQueueName))
                {
                    namespaceManager.CreateQueue(sQueueName);
                }
                // Initialize the connection to Service Bus Queue
                var client = QueueClient.CreateFromConnectionString(sConnectionString, sQueueName);

                // Create message, with the message body being automatically serialized
                var brokeredMessage = new BrokeredMessage(req);
                // Send Request
                client.Send(brokeredMessage);

               // QueueConnector.Insert(req);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                
            }
            Console.ReadLine();
        }
    }

    [Serializable]
    public class AggregationRequest
    {
        public string ConnectionID { get; set; }
        public string Aggregationtype { get; set; }
        //[Yearly / Monthly / Qtrly / Weekly / Daily / Hourly / 15 - mins]
        public DateTime Fromdate { get; set; }
        public DateTime Todate { get; set; }

        public string marketsegment { get; set; }
    }
}
