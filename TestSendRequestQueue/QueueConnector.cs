using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using vdbservices.Interfaces;

namespace TestSendRequestQueue
{
    public static class QueueConnector
    {
        // Thread-safe. Recommended that you cache rather than recreating it
        // on every request.
        public static QueueClient QueueClient;

        // Obtain these values from the portal.
        public const string Namespace = "namevdbanalyticssb";

        // The name of your queue.
        public const string QueueName = "aggregationqueue";


        public static void Insert(AggregationRequest objAggReq)
        {
            try
            {
                QueueConnector.Initialize();
                // Create a message from the order.
                var message = new BrokeredMessage(objAggReq);

                // Submit the order.
                QueueConnector.QueueClient.Send(message);               
            }
            catch (MessagingException e)
            {
                if (!e.IsTransient)
                {
                }
            }
        }

        public static NamespaceManager CreateNamespaceManager()
        {
            // Create the namespace manager which gives you access to
            // management operations.
            var uri = ServiceBusEnvironment.CreateServiceUri(
                "sb", Namespace, String.Empty);
            var tP = TokenProvider.CreateSharedAccessSignatureTokenProvider(
             //   "RootManageSharedAccessKey", "RhD5iKyJr1QzVTq3fS0gsDv+I/r7yZASENto+EpKnhU=");
               "RootManageSharedAccessKey", "iPG1toRxfEuo8JD2Hye7DzX/nnfJUtwvBn8Q5znceKY=");
            return new NamespaceManager(uri, tP);
        }

        public static void Initialize()
        {
            // Using Http to be friendly with outbound firewalls.
            ServiceBusEnvironment.SystemConnectivity.Mode =
                ConnectivityMode.Http;

            // Create the namespace manager which gives you access to
            // management operations.
            var namespaceManager = CreateNamespaceManager();

            // Create the queue if it does not exist already.
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Get a client to the queue.
            var messagingFactory = MessagingFactory.Create(
                namespaceManager.Address,
                namespaceManager.Settings.TokenProvider);
            QueueConnector.QueueClient = messagingFactory.CreateQueueClient(
                "aggregationqueue");
        }

        private static void CreateQueue()
        {
            NamespaceManager namespaceManager = NamespaceManager.Create();

            Console.WriteLine("\nCreating Queue ‘{0}’…", QueueName);

            // Delete if exists
            if (namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.DeleteQueue(QueueName);
            }

            namespaceManager.CreateQueue(QueueName);
        }


    }
}