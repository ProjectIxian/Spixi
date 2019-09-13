using IXICore;
using IXICore.Meta;
using IXICore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SPIXI
{
    class StreamClientManager
    {
        private static List<NetworkClient> streamClients = new List<NetworkClient>();
        private static List<string> connectingClients = new List<string>(); // A list of clients that we're currently connecting

        private static Thread reconnectThread;
        private static bool autoReconnect = true;

        public static string primaryS2Address = "";

        public static void start()
        {
            streamClients = new List<NetworkClient>();

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
                List<Presence> presences = PresenceList.getPresencesByType('R');
                if(presences.Count > 0)
                {
                    List<Presence> tmp_presences = presences.FindAll(x => x.addresses.Find(y => y.type == 'R' && y.nodeVersion == "xs2c-0.3.0") != null); // TODO tmp_presences can be removed after protocol is finalized

                    Random rnd = new Random();
                    Presence p = tmp_presences[rnd.Next(tmp_presences.Count)];
                    lock(p)
                    {
                        neighbor = p.addresses.Find(x => x.type == 'R').address;
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
                CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M' }, ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'R' }, null, null);
            }
        }

        private static void connectToBotNodes()
        {
            lock(FriendList.friends)
            {
                var bot_list = FriendList.friends.FindAll(x => x.bot);
                foreach(var bot_entry in bot_list)
                {
                    connectTo(bot_entry.searchForRelay(), bot_entry.walletAddress);
                }
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

                string[] netClients = getConnectedClients();

                // Check if we need to connect to more neighbors
                if (netClients.Length < 1 || !netClients.Contains(primaryS2Address))
                {
                    // Scan for and connect to a new neighbor
                    connectToRandomStreamNode();
                }

                connectToBotNodes();

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
                // Remove this node from the connecting clients list
                lock (connectingClients)
                {
                    connectingClients.Remove(client.getFullAddress(true));
                }
            }
        }

        // Connects to a specified node, with the syntax host:port
        // Returns the connected stream client
        // Returns null if connection failed
        public static NetworkClient connectTo(string host, byte[] wallet_address)
        {
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

            if (NetworkServer.isRunning())
            {
                // Verify against the publicly disclosed ip
                // Don't connect to self
                if (resolved_server_name.Equals(IxianHandler.publicIP, StringComparison.Ordinal))
                {
                    if (server[1].Equals(string.Format("{0}", IxianHandler.publicPort), StringComparison.Ordinal))
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
                        if (server[1].Equals(string.Format("{0}", IxianHandler.publicPort), StringComparison.Ordinal))
                        {
                            Logging.info(string.Format("Skipping connection to self seed node {0}", host));
                            return null;
                        }
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

        // Returns all the connected clients
        public static string[] getConnectedClients(bool only_fully_connected = false)
        {
            List<String> result = new List<String>();

            lock (streamClients)
            {
                foreach (NetworkClient client in streamClients)
                {
                    if (client.isConnected())
                    {
                        if (only_fully_connected && !client.helloReceived)
                        {
                            continue;
                        }

                        try
                        {
                            string client_name = client.getFullAddress(true);
                            result.Add(client_name);
                        }
                        catch (Exception e)
                        {
                            Logging.error(string.Format("NetworkClientManager->getConnectedClients: {0}", e.ToString()));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        public static bool sendToClient(string neighbor, ProtocolMessageCode code, byte[] data, byte[] helper_data)
        {
            NetworkClient client = null;
            lock (streamClients)
            {
                foreach (NetworkClient c in streamClients)
                {
                    if (c.getFullAddress() == neighbor)
                    {
                        client = c;
                        break;
                    }
                }
            }

            if (client != null)
            {
                client.sendData(code, data, helper_data);
                return true;
            }

            return false;
        }
    }
}
