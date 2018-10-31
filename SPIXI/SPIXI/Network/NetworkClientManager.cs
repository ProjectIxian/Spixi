using DLT;
using DLT.Meta;
using DLT.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SPIXI.Network
{
    class NetworkClientManager
    {

        public static NetworkClient dltClient = new NetworkClient(true); // The main DLT client connection
        public static bool initialNodeConnected = false;

        private static List<NetworkClient> networkClients = new List<NetworkClient>(); // S2 connections

        private static Thread reconnectThread = null;
        private static bool autoReconnect = true;

        private static NetworkClientManager singletonInstance = new NetworkClientManager();
        static NetworkClientManager()
        {
        }

        private NetworkClientManager()
        {
        }

        public static NetworkClientManager singleton
        {
            get
            {
                return singletonInstance;
            }
        }


        public static void startClients()
        {
            networkClients = new List<NetworkClient>();
            initialNodeConnected = false;

            Logging.info("Connecting to SEED ixian node");

            // Connect to a random seed node
            Thread dlt_thread = new Thread(dltConnectTo);
            // Todo: store this as static to achieve uniformity in generated numbers
            Random rnd = new Random();
            string chosenSeed = CoreNetworkUtils.seedNodes[rnd.Next(0, CoreNetworkUtils.seedNodes.Length)];
            dlt_thread.Start(chosenSeed);

            // Start the reconnect thread
            reconnectThread = new Thread(reconnectClients);
            autoReconnect = true;
            reconnectThread.Start();
                  
        }

        public static void stopClients()
        {
            autoReconnect = false;
            isolate();

            // Force stopping of reconnect thread
            if (reconnectThread == null)
                return;
            reconnectThread.Abort();
        }

        // Immediately disconnects all clients
        public static void isolate()
        {
            Logging.info("Stopping network clients...");

            lock (networkClients)
            {
                // Disconnect each client
                foreach (NetworkClient client in networkClients)
                {
                    client.disconnect();
                }

                // Empty the client list
                networkClients.Clear();
            }
        }

        // Reconnects to network clients
        public static void restartClients()
        {
            Logging.info("Stopping network clients...");
            stopClients();
            Thread.Sleep(100);
            Logging.info("Starting network clients...");
            startClients();
        }

        //////////////////////
        // DLT-specific section
        //

        // Connects to a DLT node, with the syntax host:port
        private static void dltConnectTo(object data)
        {
            if (!(data is string))
            {
                throw new Exception(String.Format("Exception in dlt connection thread {0}", data.GetType().ToString()));
            }

            string host = (string)data;

            /*
// Go through each seed node until we have a valid connection
for (int i = 0; i < CoreNetworkUtils.seedNodes.Length; i++)
{
    // Connect client immediately
    connectTo(CoreNetworkUtils.seedNodes[i]);
}*/


            string[] server = host.Split(':');
            if (server.Count() < 2)
            {
                Logging.warn(string.Format("Cannot connect to invalid dlt hostname: {0}", host));
                return;
            }

            // Check if nodes is already in the client list
            lock (dltClient)
            {
                if (dltClient.address.Equals(host, StringComparison.Ordinal))
                {
                    // Address is already used
                    return;
                }           
            }
            Logging.warn(string.Format("DLT connect to hostname: {0}", host));

            // Connect to the specified dlt node
            bool result = dltClient.connectToServer(server[0], Convert.ToInt32(server[1]));

            // Add this node to the client list if connection was successfull
            if (result == true)
            {
                Logging.info("DLT Connected.");
                requestBalanceFromDLT();
            }

            Thread.Yield();
        }

        // Send data to all connected nodes
        public static void sendDLTData(ProtocolMessageCode code, byte[] data)
        {
            lock (dltClient)
            {
                if (dltClient.isConnected())
                {
                    dltClient.sendData(code, data);
                }
            }
        }

        // Requests the latest balance information from the DLT
        // TODO: could merge this with the reverse PING network message
        public static void requestBalanceFromDLT()
        {
            // Skip if the dlt client is not connected
            if (dltClient.isConnected() == false)
                return;

            // Return the balance for the matching address
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(m))
                {
                    w.Write(Node.walletStorage.address);
                    dltClient.sendData(ProtocolMessageCode.getBalance, m.ToArray());
                }
            }
        }

        // Called after receiving a full presence list.
        // Searches for an S2 node to connect to initially
        public static void searchForStreamNode()
        {
            if (initialNodeConnected)
                return;

            Logging.info("Searching for stream node candidates");

            // Build a list of candidate nodes to select from
            List<PresenceAddress> nodeCanditates = new List<PresenceAddress>();

            foreach (Presence presence in PresenceList.presences)
            {
                foreach (PresenceAddress addr in presence.addresses)
                {
                    // Only check Relay nodes
                    if (addr.type == 'R')
                    {
                        nodeCanditates.Add(addr);
                    }
                }
            }

            // Todo: store this as static to achieve uniformity in generated numbers
            Random rnd = new Random();
            PresenceAddress chosenPresence = nodeCanditates[rnd.Next(nodeCanditates.Count)];

            Logging.info(string.Format("Connecting to stream node: {0}", chosenPresence.address));


            // TODO: check if it actually connected
            connectTo(chosenPresence.address);

            initialNodeConnected = true;
        }

        // Connects to a specific S2 node
        public static void connectToStreamNode(string nodeip)
        {
            // TODO: verify that nodeip has a corresponding R node entry in the PL
            Logging.info(string.Format("Connecting to auxiliary stream node: {0}", nodeip));

            connectTo(nodeip);
        }

        ///////////////////////
        // S2-specific section
        //

        // Connects to a specified node, with the syntax host:port
        private static void threadConnectTo(object data)
        {
            if (data is string)
            {

            }
            else
            {
                throw new Exception(String.Format("Exception in client connection thread {0}", data.GetType().ToString()));
            }

            string host = (string)data;

            string[] server = host.Split(':');
            if (server.Count() < 2)
            {
                Logging.warn(string.Format("Cannot connect to invalid hostname: {0}", host));
                return;
            }

            // Check if nodes is already in the client list
            lock (networkClients)
            {
                foreach (NetworkClient client in networkClients)
                {
                    if (client.address.Equals(host, StringComparison.Ordinal))
                    {
                        // Address is already in the client list
                        Logging.warn(string.Format("Node already connected: {0}", host));
                        return;
                    }
                }
            }

            // Connect to the specified node
            NetworkClient new_client = new NetworkClient();
            bool result = new_client.connectToServer(server[0], Convert.ToInt32(server[1]));

            // Add this node to the client list if connection was successfull
            if (result == true)
            {
                lock (networkClients)
                {
                    networkClients.Add(new_client);
                }
            }

            Thread.Yield();
        }

        // Connects to a specified node, with the syntax host:port
        // It does so by spawning a temporary thread
        public static void connectTo(string host)
        {
            if (host == null)
                return;

            Thread conn_thread = new Thread(threadConnectTo);
            conn_thread.Start(host);
        }

        // Send data to all connected nodes
        public static void broadcastData(ProtocolMessageCode code, byte[] data)
        {
            lock (networkClients)
            {
                foreach (NetworkClient client in networkClients)
                {
                    if (client.isConnected())
                    {
                        client.sendData(code, data);
                        //Console.WriteLine("CLNMGR-BROADCAST SENT: {0}", code);
                    }
                }
            }
        }

        // Send data to a specific node
        public static void sendData(ProtocolMessageCode code, byte[] data, string hostname)
        {
            lock (networkClients)
            {
                foreach (NetworkClient client in networkClients)
                {
                    if (client.address.Equals(hostname, StringComparison.Ordinal))
                    {
                        client.sendData(code, data);
                        return;
                    }
                }
            }
        }

        // Returns all the connected clients
        public static string[] getConnectedClients()
        {
            List<String> result = new List<String>();

            lock (networkClients)
            {
                foreach (NetworkClient client in networkClients)
                {
                    if (client.isConnected())
                    {
                        try
                        {
                            string client_name = client.getFullAddress();
                            result.Add(client_name);
                        }
                        catch (Exception e)
                        {
                            Logging.warn(string.Format("NetworkClientManager->getConnectedClients: {0}", e.ToString()));
                        }
                    }
                }
            }

            return result.ToArray();
        }


        // Checks for missing clients
        private static void reconnectClients()
        {
            while (autoReconnect)
            {
                // Wait 5 seconds before rechecking
                Thread.Sleep(Config.networkClientReconnectInterval);

                Logging.warn("Checking clients for reconnect");
                // First check the dlt client connection
                lock(dltClient)
                {
                    if (dltClient.isConnected() == false)
                    {
                        Logging.warn("Reconnecting DLT");
                        dltClient.reconnect();
                    }
                }

                // Then check for S2 node connections
                lock (networkClients)
                {
                    foreach (NetworkClient client in networkClients)
                    {
                        if (client.isConnected() == false)
                        {
                            Logging.warn("Reconnecting S2");
                            client.reconnect();
                        }
                        else
                        {
                            // Send a ping to maintain connection status
                            client.sendPing();
                        }
                    }
                }


            }

            Thread.Yield();
        }

        // Checks if the DLT connection is established
        public static bool isDltConnected()
        {
            lock(dltClient)
            {
                if (dltClient.isConnected())
                    return true;
            }
            return false;
        }

        // Check if a network node is connected based on it's ip and port
        public static bool isNodeConnected(string host)
        {
            // Check if nodes is already in the client list
            lock (networkClients)
            {
                foreach (NetworkClient client in networkClients)
                {
                    if (client.address.Equals(host, StringComparison.Ordinal))
                    {
                        if (client.isConnected())
                            return true;
                    }
                }
            }
            return false;
        }

        // Request the Presence List from the DLT
        public static void requestPresenceList()
        {
            dltClient.requestPresenceList();
        }

    }
}
