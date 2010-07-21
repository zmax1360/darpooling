﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;

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

        /* Service Settings */
        private Type contract;
        private WSDualHttpBinding http_binding;
        private NetTcpBinding tcp_binding;
        private ServiceMetadataBehavior mex_behavior;

        private IDarPooling client;
        private ChannelFactory<IDarPooling> channelFactory;
        
        public const string baseHTTPAddress = "http://localhost:1111/";
        public const string baseTCPAddress = "net.tcp://localhost:1112/";


        public ServiceNodeCore(string locName, string nodeName)
        {
            /* Obtain the Location of the Node */
            string[] coords = GMapsAPI.addrToLatLng(locName);
            double latitude = double.Parse(coords[0]);
            double longitude = double.Parse(coords[1]);
            Location nodeLocation = new Location(locName, latitude, longitude);
            
            serviceNode = new ServiceNode(nodeName,nodeLocation);
        }


        public void StartService()
        {
            /* Address */
            Uri http_uri = new Uri(baseHTTPAddress + NodeName);
            Uri tcp_uri = new Uri(baseTCPAddress + NodeName);
            /* Binding */
            http_binding = new WSDualHttpBinding();
            tcp_binding = new NetTcpBinding();
            /* Contract */
            contract = typeof(IDarPooling);
            /* Behavior */
            mex_behavior = new ServiceMetadataBehavior();
            mex_behavior.HttpGetEnabled = true;
            
            /* Hosting the service */
            serviceHost = new ServiceHost(typeof(DarPoolingService), http_uri);
            serviceHost.AddServiceEndpoint(contract, http_binding, "");
            serviceHost.AddServiceEndpoint(contract, tcp_binding, tcp_uri);
            serviceHost.Description.Behaviors.Add(mex_behavior);
            serviceHost.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            
            /* Run the service */
            serviceHost.Open();
            Console.WriteLine(NodeName + "\t\tnode is now active.");        
        }


        public void StopService()
        {
            Console.WriteLine("Closing the service host...");
            serviceHost.Close();
            if (channelFactory != null)
                channelFactory.Close();
        }

        // Properties
        public string NodeName
        {
            get { return serviceNode.NodeName; }
            private set { serviceNode.NodeName = value; }
        }

        public Location NodeLocation
        {
            get { return serviceNode.Location; }
            private set { serviceNode.Location = value; }
        }

        public int NumNeighbours 
        {
            get { return serviceNode.NumNeighbours; }
        }

        public ServiceNode ServiceNode
        {
            get { return serviceNode; }
        }

        #region ServiceNode methods

        public bool hasNeighbour(ServiceNodeCore node)
        {
            return serviceNode.hasNeighbour(node.ServiceNode);
        }

        public void addNeighbour(ServiceNodeCore node)
        {
            serviceNode.addNeighbour(node.ServiceNode);
        }

        public void removeNeighbour(ServiceNodeCore node)
        {
            serviceNode.removeNeighbour(node.ServiceNode);
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
        #endregion

    }

    /// <summary>
    /// This class implements the interface of the Darpooling service
    /// </summary>
    public class DarPoolingService : IDarPooling
    {
        public void SendCommand(Communication.Command command) 
        {
        
        }

        public Communication.Result GetResult()
        {
            return new Result("");
        }
        
        public void GetData(string value)
        {
            Console.WriteLine("I've received the following Request: {0}", value);
            //User[] users = new User[] { new User("Daniele"), new User("Antonio") };
            Console.WriteLine("Satisfying Client Request...");
            Result res = new Result("There are 5 Trips available");
            Thread.Sleep(4000);
            Console.WriteLine("Ready to send data back to Client...");
            OperationContext.Current.GetCallbackChannel<IDarPoolingCallback>().GetResult(res);
        }
    
    }
    
    public class Launcher
    {
        private static ArrayList nodes = new ArrayList();
    
        static void Main(string[] args)
        {
            Console.WriteLine("**** Starting the Backbone Nodes... ****\n");
            StartBackboneNodes();
            /*    Console.ReadLine(); // Press Enter to stop the services
                CloseBackboneNodes();
            */

            //WriteXML();
            Console.ReadLine();
        }





        

        public static void StartBackboneNodes()
        {
            ServiceNodeCore chiasso = new ServiceNodeCore("Chiasso", "Chiasso");
            ServiceNodeCore milano  = new ServiceNodeCore("Milano", "Milano");
            ServiceNodeCore roma    = new ServiceNodeCore("Roma", "Roma");
            ServiceNodeCore napoli  = new ServiceNodeCore("Napoli", "Napoli");                                                             
            ServiceNodeCore catania = new ServiceNodeCore("Catania", "Catania");
            ServiceNodeCore catania2= new ServiceNodeCore("Catania", "Catania2");

            /* Set of Backbone Nodes */
            ServiceNodeCore[] nodes = 
                new ServiceNodeCore[] { chiasso, milano, roma, napoli, catania, catania2
                                      };

            foreach (ServiceNodeCore node in nodes)
                { node.StartService(); }
            Console.WriteLine("\nAll Service nodes are now Active");

            /* Set Topology */
            chiasso.addNeighbour(milano);
            milano.addNeighbour(roma);
            roma.addNeighbour(napoli);
            napoli.addNeighbour(catania);
            napoli.addNeighbour(catania2);
            catania.addNeighbour(catania2);

            foreach (ServiceNodeCore n in nodes)
            {
                Console.WriteLine("I'm {0}. My Coords are : {1} and {2}. I have {3} neighbours", n.NodeName, n.NodeLocation.Latitude, n.NodeLocation.Longitude, n.NumNeighbours);
            }
            Console.WriteLine("\nWaiting for incoming requests...");
    
        }
  
        
        public static void CloseBackboneNodes()
        {
            foreach(ServiceNodeCore node in nodes)
            {
                node.StopService();
            }
        }

        public static void WriteXML()
        {

            string filename = @"..\..\..\config\chiasso.xml";
            XmlDocument xmlDoc = new XmlDocument();

            // Create a new XML file
            XmlTextWriter textWriter = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
            textWriter.Formatting = Formatting.Indented;
            textWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
            textWriter.WriteComment("Configuration File for a DarPooling Service Node");
            textWriter.WriteStartElement("configuration");
            textWriter.Close();

            xmlDoc.Load(filename);
            XmlNode root = xmlDoc.DocumentElement;
            XmlElement neighbours = xmlDoc.CreateElement("neighbours");
            XmlElement neighbour = xmlDoc.CreateElement("neighbour");
            //XmlText textNode = xmlDoc.CreateTextNode("hello");
            //textNode.Value = "hello, world";

            root.AppendChild(neighbours);
            neighbours.AppendChild(neighbour);
            neighbour.SetAttribute("Name", "Milano");
            //neighbour.AppendChild(textNode);

            //textNode.Value = "replacing hello world";
            xmlDoc.Save(filename);


            // Read a document
            XmlTextReader textReader = new XmlTextReader(filename);
            // Read until end of file
            while (textReader.Read())
            {
                // Do something
                //Console.WriteLine(textReader.Value);   
            }

        }

    } //End Launcher
} //End Namespace


/*
public string CallNeighbour()
{
    EndpointAddress n_address = new EndpointAddress("http://localhost:1111/Milano");
    WSDualHttpBinding  n_binding = new WSDualHttpBinding();
    channelFactory = new ChannelFactory<IDarPooling>(n_binding);
    client = channelFactory.CreateChannel(n_address);
    return client.SayHello();
}
*/
/*      public string SayHello() 
      {
          Console.WriteLine("I received a request");
          return "Hi, dummy";
      }
      */

/*
public static void TestNeighbour(string message)
{

    ServiceNodeCore n0 = (ServiceNodeCore) nodes[0];
    ServiceNodeCore n1 = (ServiceNodeCore)nodes[1];
            
    Console.WriteLine(n0.NodeName + " is calling Milano....");
    string mex = n0.CallNeighbour();
    Console.WriteLine("Got :" + mex);
}
*/