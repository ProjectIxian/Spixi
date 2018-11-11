using DLT.Meta;
using IXICore;
using SPIXI;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DLT.Network
{
    public class ProtocolMessage
    {
        // Broadcast a protocol message across clients and nodes
        // Returns true if it sent the message at least one endpoint. Returns false if the message couldn't be sent to any endpoints
        public static bool broadcastProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint skipEndpoint = null, bool sendToSingleRandomNode = false)
        {
            if (data == null)
            {
                Logging.warn(string.Format("Invalid protocol message data for {0}", code));
                return false;
            }

            if (sendToSingleRandomNode)
            {
                int serverCount = NetworkClientManager.getConnectedClients().Count();

                Random r = new Random();
                int rIdx = r.Next(serverCount);

                RemoteEndpoint re = null;

                re = NetworkClientManager.getClient(rIdx);
                
                if (re != null && re.isConnected())
                {
                    re.sendData(code, data);
                    return true;
                }
                return false;
            }
            else
            {
                bool c_result = NetworkClientManager.broadcastData(code, data, skipEndpoint);

                if (!c_result)
                    return false;
            }

            return true;
        }

        public static void sendHelloMessage(RemoteEndpoint endpoint, bool sendHelloData)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    string publicHostname = string.Format("{0}:{1}", Config.publicServerIP, Config.serverPort);

                    // Send the node version
                    writer.Write(CoreConfig.protocolVersion);

                    // Send the public node address
                    byte[] address = Node.walletStorage.address;
                    writer.Write(address.Length);
                    writer.Write(address);

                    // Send the testnet designator
                    writer.Write(Config.isTestNet);

                    // Send the node type
                    char node_type = 'C'; // This is a Client node

                    writer.Write(node_type);

                    // Send the version
                    writer.Write(Config.version);

                    // Send the node device id
                    writer.Write(Config.device_id);

                    // Send the wallet public key
                    writer.Write(Node.walletStorage.publicKey.Length);
                    writer.Write(Node.walletStorage.publicKey);

                    // Send listening port
                    writer.Write(Config.serverPort);

                    // Send timestamp
                    long timestamp = Core.getCurrentTimestamp();
                    writer.Write(timestamp);

                    // send signature
                    byte[] signature = CryptoManager.lib.getSignature(Encoding.UTF8.GetBytes(CoreConfig.ixianChecksumLockString + "-" + Config.device_id + "-" + timestamp + "-" + publicHostname), Node.walletStorage.privateKey);
                    writer.Write(signature.Length);
                    writer.Write(signature);


                    if (sendHelloData)
                    {
                        // Write the legacy level
                        writer.Write(Legacy.getLegacyLevel());

                        endpoint.sendData(ProtocolMessageCode.helloData, m.ToArray());

                    }
                    else
                    {
                        endpoint.sendData(ProtocolMessageCode.hello, m.ToArray());
                    }
                }
            }
        }



        // Read a protocol message from a byte array
        public static void readProtocolMessage(byte[] recv_buffer, RemoteEndpoint endpoint)
        {
            if (endpoint == null)
            {
                Logging.error("Endpoint was null. readProtocolMessage");
                return;
            }

            ProtocolMessageCode code = ProtocolMessageCode.hello;
            byte[] data = null;

            using (MemoryStream m = new MemoryStream(recv_buffer))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    // Check for multi-message packets. One packet can contain multiple network messages.
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte[] data_checksum;
                        try
                        {
                            byte startByte = reader.ReadByte();

                            int message_code = reader.ReadInt32();
                            code = (ProtocolMessageCode)message_code;

                            int data_length = reader.ReadInt32();

                            // If this is a connected client, filter messages
                            if (endpoint.GetType() == typeof(RemoteEndpoint))
                            {
                                if (endpoint.presence == null)
                                {
                                    // Check for presence and only accept hello and syncPL messages if there is no presence.
                                    if (code == ProtocolMessageCode.hello || code == ProtocolMessageCode.syncPresenceList || code == ProtocolMessageCode.getBalance || code == ProtocolMessageCode.newTransaction)
                                    {

                                    }
                                    else
                                    {
                                        // Ignore anything else
                                        return;
                                    }
                                }
                            }




                            data_checksum = reader.ReadBytes(32); // sha256, 8 bits per byte
                            byte header_checksum = reader.ReadByte();
                            byte endByte = reader.ReadByte();
                            data = reader.ReadBytes(data_length);
                        }
                        catch (Exception e)
                        {
                            Logging.error(String.Format("NET: dropped packet. {0}", e));
                            return;
                        }
                        // Compute checksum of received data
                        byte[] local_checksum = Crypto.sha512sqTrunc(data);

                        // Verify the checksum before proceeding
                        if (local_checksum.SequenceEqual(data_checksum) == false)
                        {
                            Logging.error("Dropped message (invalid checksum)");
                            continue;
                        }

                        // For development purposes, output the proper protocol message
                        //Console.WriteLine(string.Format("NET: {0} | {1} | {2}", code, data_length, Crypto.hashToString(data_checksum)));

                        // Can proceed to parse the data parameter based on the protocol message code.
                        // Data can contain multiple elements.
                        //parseProtocolMessage(code, data, socket, endpoint);
                        NetworkQueue.receiveProtocolMessage(code, data, data_checksum, endpoint);
                    }
                }
            }
        }


        // Unified protocol message parsing
        public static void parseProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            if (endpoint == null)
            {
                Logging.error("Endpoint was null. parseProtocolMessage");
                return;
            }
            try
            {
                switch (code)
                {
                    case ProtocolMessageCode.hello:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    string hostname = reader.ReadString();
                                    Console.WriteLine("Received IP: {0}", hostname);

                                    // Another layer to catch any incompatible node exceptions for the hello message
                                    try
                                    {
                                        string pubkey = reader.ReadString();
                                        char node_type = reader.ReadChar();
                                        string device_id = reader.ReadString();

                                        Console.WriteLine("Received Address: {0} of type {1}", pubkey, node_type);
                                        /*
                                        // Store the presence address for this remote endpoint
                                        client.presenceAddress = new PresenceAddress(device_id, hostname, node_type);

                                        // Create a temporary presence with the client's address and device id
                                        Presence presence = new Presence(pubkey, client.presenceAddress);

                                        // Retrieve the final presence entry from the list (or create a fresh one)
                                        client.presence = PresenceList.updateEntry(presence);*/


                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Non compliant node connected. {0}", e.ToString());
                                    }

                                }
                            }

                        }
                        break;


                    case ProtocolMessageCode.helloData:
                       using (MemoryStream m = new MemoryStream(data))
                        {
                            using (BinaryReader reader = new BinaryReader(m))
                            {
                                int node_version = reader.ReadInt32();

                                // Check for incompatible nodes
                        /*        if (node_version < Config.nodeVersion)
                                {
                                    Console.WriteLine("Hello: Connected node version ({0}) is too old! Upgrade the node.", node_version);
                                    socket.Disconnect(true);
                                    return;
                                }*/

                                Console.WriteLine("Connected version : {0}", node_version);
                                endpoint.helloReceived = true;
                                // Get presences
                                endpoint.sendData(ProtocolMessageCode.syncPresenceList, new byte[1]);
                            }
                        }
                        break;

                    case ProtocolMessageCode.s2data:
                        {
                            StreamProcessor.receiveData(data, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.s2keys:
                        {
                            Console.WriteLine("NET: Receiving S2 keys!");
                    //        StreamProcessor.receivedKeys(data, socket);
                        }
                        break;

                    case ProtocolMessageCode.syncPresenceList:
                        {
                            byte[] pdata = PresenceList.getBytes();
                     //       byte[] ba = prepareProtocolMessage(ProtocolMessageCode.presenceList, pdata);
                     //       socket.Send(ba, SocketFlags.None);
                        }
                        break;

                    case ProtocolMessageCode.presenceList:
                        {
                            Logging.info("NET: Receiving complete presence list");
                            PresenceList.syncFromBytes(data);
                     //       NetworkClientManager.searchForStreamNode();
                        }
                        break;

                    case ProtocolMessageCode.updatePresence:
                        {
                            Console.WriteLine("NET: Receiving presence list update");
                            // Parse the data and update entries in the presence list
                            PresenceList.updateFromBytes(data);
                        }
                        break;


                    case ProtocolMessageCode.balance:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int address_length = reader.ReadInt32();
                                    byte[] address = reader.ReadBytes(address_length);

                                    // Retrieve the latest balance
                                    IxiNumber balance = reader.ReadString();

                                    if(address.SequenceEqual(Node.walletStorage.address))
                                    {
                                        Node.balance = balance;
                                    }

                                    // Retrieve the blockheight for the balance
                                    ulong blockheight = reader.ReadUInt64();
                                    Node.blockHeight = blockheight;
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.transactionData:
                        {
                            // TODO: check for errors/exceptions
                            Transaction transaction = new Transaction(data);
                            TransactionCache.addTransaction(transaction);
                        }
                        break;

                    case ProtocolMessageCode.newTransaction:
                        {
                            // Forward the new transaction message to the DLT network
                            Logging.info("RECIEVED NEW TRANSACTION");

                            Transaction transaction = new Transaction(data);
                            if (transaction.toList.Keys.First().SequenceEqual(Node.walletStorage.address))
                            {
                                TransactionCache.addTransaction(transaction);
                            }
                        }
                        break;

                    case ProtocolMessageCode.bye:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    // Retrieve the message
                                    string message = reader.ReadString();
                                    endpoint.stop();

                                    Logging.error(string.Format("Disconnected with message: {0}", message));
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.ping:
                        {
                            endpoint.sendData(ProtocolMessageCode.pong, new byte[1]);
                        }
                        break;

                    case ProtocolMessageCode.pong:
                        {
                            // do nothing
                        }
                        break;

                    default:
                        break;

                }
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error parsing network message. Details: {0}", e.ToString()));
            }
        }
    }
}
