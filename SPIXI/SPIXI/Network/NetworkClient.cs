using DLT;
using DLT.Meta;
using DLT.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SPIXI.Network
{
    class NetworkClient
    {
        private TcpClient tcpClient = null;
        public bool running;
        public string address = "127.0.0.1:10000";
        public bool dlt_mode = false;

        private string tcpHostname = "";
        private int tcpPort = 0;

        public NetworkClient(bool dlt = false)
        {
            initializeSocket();

            dlt_mode = dlt;
        }

        public void initializeSocket()
        {
            if (tcpClient != null)
                return;

            tcpClient = new TcpClient();

            // Don't allow another socket to bind to this port.
            tcpClient.Client.ExclusiveAddressUse = true;

            // The socket will linger for 3 seconds after 
            // Socket.Close is called.
            tcpClient.Client.LingerState = new LingerOption(true, 3);

            // Disable the Nagle Algorithm for this tcp socket.
            tcpClient.Client.NoDelay = true;
        }

        public bool connectToServer(string hostname, int port)
        {
            tcpHostname = hostname;
            tcpPort = port;
            address = string.Format("{0}:{1}", hostname, port);


            try
            {
                initializeSocket();

                if (tcpClient.Client != null)
                    if (tcpClient.Client.Connected)
                    {
                        tcpClient.Client.Shutdown(SocketShutdown.Both);
                        tcpClient.Client.Disconnect(true);

                    }

                tcpClient.Connect(hostname, port);
            }
            catch (Exception e)
            {
                Logging.warn(string.Format("Network client connection to {0}:{1} has failed. {2}", hostname, port, e.ToString()));
                // TODO: check why it's failing with socket already connected exception
                disconnect();
                running = false;
                return false;
            }

            Logging.info(string.Format("Network client connected to {0}:{1}", hostname, port));

            running = true;
            Thread thread = new Thread(new ThreadStart(onUpdate));
            thread.Start();

            return true;
        }

        // Reconnect with the previous settings
        public void reconnect()
        {
            if (tcpHostname.Length < 1)
            {
                Logging.warn("Network client reconnect failed due to invalid hostname.");
                return;
            }

            connectToServer(tcpHostname, tcpPort);
        }


        // Sends data over the network
        public void sendData(ProtocolMessageCode code, byte[] data)
        {
            byte[] ba = ProtocolMessage.prepareProtocolMessage(code, data);
            try
            {
                tcpClient.Client.Send(ba, SocketFlags.None);
            }
            catch (Exception)
            {
                Console.WriteLine("CLN: Socket exception, attempting to reconnect");
                reconnect();
            }
        }

        private void onUpdate()
        {
            // Check if we're in DLT mode and request the presence list for selecting the S2 node
            if (dlt_mode)
            {
                // Wait for the sockets a bit
                Thread.Sleep(500);
                requestPresenceList();
            }
            else
            {
                // If we're not in DLT mode, send a hello message
                sendHello();
            }

            while (running)
            {
                try
                {
                    // Let the protocol handler receive and handle messages
                    ProtocolMessage.readProtocolMessage(tcpClient.Client);
                }
                catch (Exception e)
                {
                    disconnect();
                    Thread.Yield();
                    return;
                }

                // Sleep a while to prevent cpu cycle waste
                Thread.Sleep(10);
            }

            disconnect();
            Thread.Yield();
        }

        public void disconnect()
        {
            // Check if socket already disconnected
            if (tcpClient == null)
            {
                return;
            }
            if (tcpClient.Client == null)
            {
                return;
            }

            // Stop reading protocol messages
            running = false;

            if (tcpClient.Client.Connected)
            {
                // TODO: verify the correct order
                tcpClient.Client.Shutdown(SocketShutdown.Both);
                tcpClient.Client.Disconnect(true);
                //tcpClient.GetStream().Close();
                tcpClient.Close();
            }
            if(tcpClient != null)
                tcpClient.Dispose();
            tcpClient = null;

        }

        // Send a hello message containing the public ip and port of this node
        public void sendHello()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    string publicHostname = string.Format("spixi:000"); //string.Format("{0}:{1}", NetworkStreamServer.publicIPAddress, Config.serverPort);
                    // Send the public IP address and port
                    writer.Write(publicHostname);

                    // Send the public node address
                    string address = Node.walletStorage.address;
                    writer.Write(address);

                    // Send the node type
                    char node_type = 'C'; // This is a Client node
                    writer.Write(node_type);

                    // Send the node device id
                    writer.Write(Config.device_id);

                    // Send the S2 public key
                    writer.Write(Node.walletStorage.encPublicKey);

                    // Send the wallet public key
                    writer.Write(Node.walletStorage.publicKey);

                    sendData(ProtocolMessageCode.hello, m.ToArray());

                    // Send a test message
                    //sendTestMessage();
                }
            }
        }

        // Request the presence list from our DLT connection
        public void requestPresenceList()
        {
            Console.WriteLine("Requesting presence list now");
            // Get presences
            sendData(ProtocolMessageCode.syncPresenceList, new byte[1]);
        }

        // Get the ip/hostname and port
        public string getFullAddress()
        {
            return string.Format("{0}:{1}", tcpHostname, tcpPort);
        }

        public bool isConnected()
        {
            try
            {
                if (tcpClient == null)
                {
                    return false;
                }

                if (tcpClient.Client == null)
                {
                    return false;
                }

                return tcpClient.Connected && running;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void sendTestMessage()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    string recipient_address = "3ce4e4e20b3323b2450ee468a820f8d1fdca7efe58794483ffc2616890a811ed";
                    writer.Write(recipient_address);

                    string transaction_id = "aaa";
                    writer.Write(transaction_id);


                    string message = "This is an IXIAN S2 test message";



                    byte[] encrypted_message = Encoding.UTF8.GetBytes(message);
                    int encrypted_count = encrypted_message.Count();

                    writer.Write(encrypted_count);
                    writer.Write(encrypted_message);

                    byte[] checksum = Crypto.sha256(encrypted_message);

                    //Transaction transaction = new Transaction(0, to, from);
                    //sendData(ProtocolMessageCode.newTransaction, transaction.getBytes());

                    sendData(ProtocolMessageCode.s2data, m.ToArray());
                }
            }
        }

        // Send a ping message to this specific client
        public void sendPing()
        {
            //sendData(ProtocolMessageCode.ping, new byte[1]);
            sendKeepAlive();
        }

        // Broadcasts a keepalive network message for this node PL address
        public bool sendKeepAlive()
        {
            try
            {
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {

                        string publicHostname = getFullAddress();
                        string wallet = Node.walletStorage.address;
                        writer.Write(wallet);
                        writer.Write(Config.device_id);
                        writer.Write(publicHostname);

                        // Add the unix timestamp
                        string timestamp = Clock.getTimestamp(DateTime.Now);
                        writer.Write(timestamp);

                        // Add a verifiable signature
                        string private_key = Node.walletStorage.privateKey;
                        string signature = CryptoManager.lib.getSignature(timestamp, private_key);
                        writer.Write(signature);

                    }

                    sendData(ProtocolMessageCode.keepAlivePresence, m.ToArray());
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
