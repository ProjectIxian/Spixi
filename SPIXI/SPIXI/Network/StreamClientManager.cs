using DLT;
using DLT.Meta;
using DLT.Network;
using IXICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SPIXI
{
    class StreamClientManager
    {
        private static List<NetworkClient> streamClients = new List<NetworkClient>();
        private static List<string> connectingClients = new List<string>(); // A list of clients that we're currently connecting

        private static Thread reconnectThread;
        private static bool autoReconnect = true;

        public static void start()
        {
            streamClients = new List<NetworkClient>();

            // Public DEMO S2 node address: 209.141.49.106 port 11235
            // connectTo("209.141.49.106:11235");

            // Start the reconnect thread
            reconnectThread = new Thread(reconnectClients);
            autoReconnect = true;
            reconnectThread.Start();
        }

        public static void stop()
        {
            autoReconnect = false;
            isolate();

            // Force stopping of reconnect thread
            if (reconnectThread == null)
                return;
            reconnectThread.Abort();
            reconnectThread = null;
        }

        // Immediately disconnects all clients
        public static void isolate()
        {
            Logging.info("Isolating stream clients...");

            lock (streamClients)
            {
                // Disconnect each client
                foreach (NetworkClient client in streamClients)
                {
                    client.stop();
                }

                // Empty the client list
                streamClients.Clear();
            }
        }

        public static void restartClients()
        {
            Logging.info("Stopping stream clients...");
            stop();
            Thread.Sleep(100);
            Logging.info("Starting stream clients...");
            start();
        }

        // Send data to all connected nodes
        // Returns true if the data was sent to at least one client
        public static bool broadcastData(ProtocolMessageCode code, byte[] data, RemoteEndpoint skipEndpoint = null)
        {
            bool result = false;
            lock (streamClients)
            {
                foreach (NetworkClient client in streamClients)
                {
                    if (client.isConnected())
                    {
                        if (skipEndpoint != null)
                        {
                            if (client == skipEndpoint)
                                continue;
                        }

                        if (client.helloReceived == false)
                        {
                            continue;
                        }

                        client.sendData(code, data);
                        result = true;
                    }
                }
            }
            return result;
        }

        // Scan for and connect to a new stream node
        private static void connectToRandomStreamNode()
        {
            // TODO TODO TODO TODO improve this
            string neighbor = null;

            try
            {
                lock (PresenceList.presences)
                {
                    // Go through each presence again. This should be more optimized.
                    foreach (Presence s2presence in PresenceList.presences)
                    {
                        // Go through each single address
                        foreach (PresenceAddress s2addr in s2presence.addresses)
                        {
                            // Only check if it's a Relay node
                            if (s2addr.type == 'R')
                            {
                                // We found our s2 node
                                neighbor = s2addr.address;
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Logging.error("Exception looking up random stream node: " + e.Message);
                return;
            }

            if (neighbor != null)
            {
                Logging.info(string.Format("Attempting to add new stream node: {0}", neighbor));
                connectTo(neighbor, null);
            }
            else
            {
                Logging.error("FAILED TO ADD RANDOM STREAM NODE");
            }
        }

        private static void reconnectClients()
        {
            Random rnd = new Random();

            // Wait 5 seconds before starting the loop
            Thread.Sleep(CoreConfig.networkClientReconnectInterval);

            while (autoReconnect)
            {
                handleDisconnectedClients();

                List<NetworkClient> netClients = null;
                lock (streamClients)
                {
                    netClients = new List<NetworkClient>(streamClients);
                }

                // Check if we need to connect to more neighbors
                if (netClients.Count < 1)
                {
                    // Scan for and connect to a new neighbor
                    connectToRandomStreamNode();
                }

                // Wait 5 seconds before rechecking
                Thread.Sleep(CoreConfig.networkClientReconnectInterval);
            }
        }

        private static void handleDisconnectedClients()
        {
            List<NetworkClient> netClients = null;
            lock (streamClients)
            {
                netClients = new List<NetworkClient>(streamClients);
            }

            // Prepare a list of failed clients
            List<NetworkClient> failed_clients = new List<NetworkClient>();

            List<NetworkClient> dup_clients = new List<NetworkClient>();

            foreach (NetworkClient client in netClients)
            {
                if (dup_clients.Find(x => x.getFullAddress(true) == client.getFullAddress(true)) != null)
                {
                    failed_clients.Add(client);
                    continue;
                }
                dup_clients.Add(client);
                if (client.isConnected())
                {
                    continue;
                }
                // Check if we exceeded the maximum reconnect count
                if (client.getTotalReconnectsCount() >= CoreConfig.maximumNeighborReconnectCount || client.fullyStopped)
                {
                    // Remove this client so we can search for a new neighbor
                    failed_clients.Add(client);
                }
                else
                {
                    // Reconnect
                    client.reconnect();
                }
            }

            // Go through the list of failed clients and remove them
            foreach (NetworkClient client in failed_clients)
            {
                client.stop();
                lock (streamClients)
                {
                    streamClients.Remove(client);
                }
            }
        }

        // Connects to a specified node, with the syntax host:port
        // Returns the connected stream client
        // Returns null if connection failed
        public static NetworkClient connectTo(string host, byte[] wallet_address)
        {
            Logging.info(String.Format("Connecting to S2 node: {0}", host));

            if (host == null || host.Length < 3)
            {
                Logging.error(String.Format("Invalid host address {0}", host));
                return null;
            }

            string[] server = host.Split(':');
            if (server.Count() < 2)
            {
                Logging.warn(string.Format("Cannot connect to invalid hostname: {0}", host));
                return null;
            }

            // Resolve the hostname first
            string resolved_server_name = NetworkUtils.resolveHostname(server[0]);

            // Skip hostnames we can't resolve
            if (resolved_server_name.Length < 1)
            {
                Logging.warn(string.Format("Cannot resolve IP for {0}, skipping connection.", server[0]));
                return null;
            }

            string resolved_host = string.Format("{0}:{1}", resolved_server_name, server[1]);

            // Verify against the publicly disclosed ip
            // Don't connect to self
            if (resolved_server_name.Equals(NetworkClientManager.publicIP, StringComparison.Ordinal))
            {
                if (server[1].Equals(string.Format("{0}", NetworkServer.getListeningPort()), StringComparison.Ordinal))
                {
                    Logging.info(string.Format("Skipping connection to public self seed node {0}", host));
                    return null;
                }
            }

            // Get all self addresses and run through them
            List<string> self_addresses = CoreNetworkUtils.GetAllLocalIPAddresses();
            foreach (string self_address in self_addresses)
            {
                // Don't connect to self
                if (resolved_server_name.Equals(self_address, StringComparison.Ordinal))
                {
                    if (server[1].Equals(string.Format("{0}", NetworkServer.getListeningPort()), StringComparison.Ordinal))
                    {
                        Logging.info(string.Format("Skipping connection to self seed node {0}", host));
                        return null;
                    }
                }
            }

            lock (connectingClients)
            {
                foreach (string client in connectingClients)
                {
                    if (resolved_host.Equals(client, StringComparison.Ordinal))
                    {
                        // We're already connecting to this client
                        return null;
                    }
                }

                // The the client to the connecting clients list
                connectingClients.Add(resolved_host);
            }

            // Check if node is already in the client list
            lock (streamClients)
            {
                foreach (NetworkClient client in streamClients)
                {
                    if (client.getFullAddress(true).Equals(resolved_host, StringComparison.Ordinal))
                    {
                        // Address is already in the client list
                        return null;
                    }
                }
            }


            // Connect to the specified node
            NetworkClient new_client = new NetworkClient();
            // Recompose the connection address from the resolved IP and the original port
            bool result = new_client.connectToServer(resolved_server_name, Convert.ToInt32(server[1]), wallet_address);

            // Add this node to the client list if connection was successfull
            if (result == true)
            {
                // Add this node to the client list
                lock (streamClients)
                {
                    streamClients.Add(new_client);
                }

                // TODO set the primary s2 host more efficiently, perhaps allow for multiple s2 primary hosts
                Node.primaryS2Address = host;
                // Send a keepalive
                //Node.sendKeepAlive();
            }

            // Remove this node from the connecting clients list
            lock (connectingClients)
            {
                connectingClients.Remove(resolved_host);
            }

            return new_client;
        }

        // Check if we're connected to a certain host address
        // Returns StreamClient or null if not found
        public static NetworkClient isConnectedTo(string address)
        {
            lock (streamClients)
            {
                foreach (NetworkClient client in streamClients)
                {
                    if (client.remoteIP.Address.ToString().Equals(address, StringComparison.Ordinal))
                        return client;
                }
            }

            return null;
        }

    }
}
