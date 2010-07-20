﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Communication;
using System.Runtime.Serialization;

using System.Collections;
using System.Threading;


namespace DarPoolingNode
{

    public class ServiceNodeCore
    {
        private ServiceNode serviceNode;
        private ServiceHost serviceHost;

        private Type contract;
        private WSHttpBinding binding;
        private ServiceMetadataBehavior behavior;

        private IDarPooling client;
        private ChannelFactory<IDarPooling> channelFactory;
        
        public const string baseAddress = "http://localhost:1111/";
        public string nodeName { get; private set; }


        public ServiceNodeCore(string nodeName)
        {
            this.nodeName = nodeName;
        }

        public void StartService()
        {
            serviceNode = new ServiceNode(new Location("Ragusa"), "Ragusa1");

            binding = new WSHttpBinding();
            contract = typeof(IDarPooling);

            serviceHost = new ServiceHost(typeof(DarPoolingService), new Uri( baseAddress + nodeName));
            serviceHost.AddServiceEndpoint(contract, binding, "");

            behavior = new ServiceMetadataBehavior();
            behavior.HttpGetEnabled = true;

            serviceHost.Description.Behaviors.Add(behavior);
            serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            
            serviceHost.Open();
            Console.WriteLine(nodeName + "\t\tnode is now active.");        
            
        }


        public string CallNeighbour()
        {
            /* Verbose */
            EndpointAddress n_address = new EndpointAddress("http://localhost:1111/Milano");
            WSHttpBinding  n_binding = new WSHttpBinding();
            channelFactory = new ChannelFactory<IDarPooling>(n_binding);
            client = channelFactory.CreateChannel(n_address);
            return client.SayHello();
        }

        public void StopService()
        {
            Console.WriteLine("Closing the service host...");
            serviceHost.Close();
            if (channelFactory != null)
                channelFactory.Close();
        }


        public bool hasNeighbour(ServiceNode node)
        {
            return serviceNode.hasNeighbour(node);
        }

        public void addNeighbour(ServiceNode node)
        {
            serviceNode.addNeighbour(node);
        }

        public void removeNeighbour(ServiceNode node)
        {
            serviceNode.removeNeighbour(node);
        }

        public bool hasUser(UserNode node)
        {
            return serviceNode.hasUser(node);
        }

        public void addUser(UserNode node)
        {
            serviceNode.addUser(node);
        }

        public void removeUser(UserNode node)
        {
            serviceNode.removeUser(node);
        }
    }


    //[ServiceBehavior(ConcurrencyMode=ConcurrencyMode.Reentrant)]
    public class DarPoolingService : IDarPooling
    {
        public void SendCommand(Communication.Command command) {}

        public Communication.Result GetResult()
        {
            return new Result();
        }
        
    
        public SimpleUser[] GetSimpleUsers(SimpleUser[] inputUsers)
        {
            List<SimpleUser> result = new List<SimpleUser>();
            foreach (SimpleUser su in inputUsers)
            {
                if (su.userId < 10)
                {
                    result.Add(su);
                }

            }

            return result.ToArray();
        }

        public string SayHello() 
        {
            Console.WriteLine("I received a request");
            //var callback = OperationContext.Current.GetCallbackChannel<IDarPoolingCallback>();
            //callback.OnCallback();
            //Thread.Sleep(4000);
            return "Hi, dummy";
        }
    
    }
    
    public class Launcher
    {
        private static ArrayList nodes = new ArrayList();
    
        static void Main(string[] args)
        {
            Console.WriteLine("**** Starting the Backbone Nodes... ****\n");
            StartBackboneNodes();

            Console.WriteLine("\nAll Service nodes are now Active");
            Console.WriteLine("\nWaiting for incoming requests...");
            
            /*
            Console.WriteLine("\n\n" + "Insert a message: ");
            string tmp = Console.ReadLine();
            TestNodes(tmp);
            Console.ReadLine();
            */

            /* Press Enter to stop the services */
            Console.ReadLine();
            CloseBackboneNodes();
        }

        public static void StartBackboneNodes()
        {
            //string[] nodeNames = { "Catania" };
            string[] nodeNames = { "Chiasso", "Milano", "Roma", "Napoli", "Catania" };

            foreach (string name in nodeNames)
            {
                nodes.Add(new ServiceNodeCore(name.ToUpper()));
            }

            foreach (ServiceNodeCore node in nodes)
            {
                node.StartService();
            }

            //peer.Channel.Ping(peer.Id, tmp);
            //peer.Stop();
            //peerThread.Join();

        }

        public static void TestNodes(string message)
        {

            ServiceNodeCore n0 = (ServiceNodeCore) nodes[0];
            ServiceNodeCore n1 = (ServiceNodeCore)nodes[1];
            
            Console.WriteLine(n0.nodeName + " is calling Milano....");
            string mex = n0.CallNeighbour();
            Console.WriteLine("Got :" + mex);
        }

        public static void CloseBackboneNodes()
        {
            foreach(ServiceNodeCore node in nodes)
            {
                node.StopService();
            }
        }

    } //End Class
} //End Namespace