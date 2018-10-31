using DLT.Meta;
using SPIXI;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DLT.Network
{
    public class ProtocolMessage
    {

        // Prepare a network protocol message. Works for both client-side and server-side
        public static byte[] prepareProtocolMessage(ProtocolMessageCode code, byte[] data)
        {
            byte[] result = null;

            // Prepare the protocol sections
            int data_length = data.Length;
            byte[] data_checksum = Crypto.sha256(data);

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    // Protocol sections are code, length, checksum, data
                    // Write each section in binary, in that specific order
                    writer.Write((int)code);
                    writer.Write(data_length);
                    writer.Write(data_checksum);
                    writer.Write(data);
                }
                result = m.ToArray();
            }

            return result;
        }

        // Broadcast a protocol message across clients and nodes
        public static void broadcastProtocolMessage(ProtocolMessageCode code, byte[] data, Socket skipSocket = null)
        {
            if (data == null)
            {
                Logging.warn(string.Format("Invalid protocol message data for {0}", code));
                return;
            }

            // Skip presence updates
            if(code == ProtocolMessageCode.updatePresence || code == ProtocolMessageCode.removePresence || code == ProtocolMessageCode.syncPresenceList)
            {
                return;
            }

            NetworkClientManager.broadcastData(code, data);
            //NetworkServer.broadcastData(code, data);
        }

        // Server-side protocol reading
        public static void readProtocolMessage(Socket socket)
        {
            // Check for socket availability
            if (socket.Connected == false)
            {
                throw new Exception("Socket already disconnected at other end");
            }

            if (socket.Available < 1)
            {
                // Sleep a while to prevent cpu cycle waste
                Thread.Sleep(100);
                return;
            }
            // Read multi-packet messages
            // TODO: optimize this as it's not very efficient
            var big_buffer = new List<byte>();

            try
            {
                while (socket.Available > 0)
                {
                    var current_byte = new Byte[1];
                    var byteCounter = socket.Receive(current_byte, current_byte.Length, SocketFlags.None);

                    if (byteCounter.Equals(1))
                    {
                        big_buffer.Add(current_byte[0]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("NET: endpoint disconnected " + e);
                throw e;
            }

            byte[] recv_buffer = big_buffer.ToArray();

            ProtocolMessageCode code = ProtocolMessageCode.hello;
            byte[] data = null;

            using (MemoryStream m = new MemoryStream(recv_buffer))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    // Check for multi-message packets. One packet can contain multiple network messages.
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        int message_code = reader.ReadInt32();
                        code = (ProtocolMessageCode)message_code;

                        int data_length = reader.ReadInt32();
                        if (data_length < 0)
                            return;

                        byte[] data_checksum;

                        try
                        {
                            data_checksum = reader.ReadBytes(32); // sha256, 8 bits per byte
                            data = reader.ReadBytes(data_length);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("NET: dropped packet. " + e);
                            return;
                        }
                        // Compute checksum of received data
                        byte[] local_checksum = Crypto.sha256(data);

                        // Verify the checksum before proceeding
                        if (Crypto.byteArrayCompare(local_checksum, data_checksum) == false)
                        {
                            Logging.warn("Dropped message (invalid checksum)");
                            continue;
                        }

                        // For development purposes, output the proper protocol message
                        Console.WriteLine(string.Format("NET: {0} | {1} | {2}", code, data_length, Crypto.hashToString(data_checksum)));

                        // Can proceed to parse the data parameter based on the protocol message code.
                        // Data can contain multiple elements.
                        parseProtocolMessage(code, data, socket);
                    }
                }
            }




        }

        // Unified protocol message parsing
        public static void parseProtocolMessage(ProtocolMessageCode code, byte[] data, Socket socket)
        {
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
                                if (node_version < Config.nodeVersion)
                                {
                                    Console.WriteLine("Hello: Connected node version ({0}) is too old! Upgrade the node.", node_version);
                                    socket.Disconnect(true);
                                    return;
                                }

                                Console.WriteLine("Connected version : {0}", node_version);

                                // Get presences
                                socket.Send(prepareProtocolMessage(ProtocolMessageCode.syncPresenceList, new byte[1]), SocketFlags.None);
                            }
                        }
                        break;

                    case ProtocolMessageCode.s2data:
                        {
                            StreamProcessor.receiveData(data, socket);
                        }
                        break;

                    case ProtocolMessageCode.s2keys:
                        {
                            Console.WriteLine("NET: Receiving S2 keys!");
                            StreamProcessor.receivedKeys(data, socket);
                        }
                        break;

                    case ProtocolMessageCode.syncPresenceList:
                        {
                            byte[] pdata = PresenceList.getBytes();
                            byte[] ba = prepareProtocolMessage(ProtocolMessageCode.presenceList, pdata);
                            socket.Send(ba, SocketFlags.None);
                        }
                        break;

                    case ProtocolMessageCode.presenceList:
                        {
                            Logging.info("NET: Receiving complete presence list");
                            PresenceList.syncFromBytes(data);
                            NetworkClientManager.searchForStreamNode();
                        }
                        break;

                    case ProtocolMessageCode.updatePresence:
                        {
                            Console.WriteLine("NET: Receiving presence list update");
                            // Parse the data and update entries in the presence list
                            PresenceList.updateFromBytes(data);
                        }
                        break;

                    case ProtocolMessageCode.removePresence:
                        {
                            Console.WriteLine("NET: Receiving presence list entry removal");
                            // Parse the data and remove the entry from the presence list
                            Presence presence = new Presence(data);
                            PresenceList.removeEntry(presence);
                        }
                        break;

                    case ProtocolMessageCode.balance:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    string address = reader.ReadString();

                                    // Retrieve the latest balance
                                    IxiNumber balance = reader.ReadString();

                                    if(address.Equals(Node.walletStorage.address, StringComparison.Ordinal))
                                    {
                                        Node.balance = balance;
                                    }
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
                            if (transaction.to.Equals(Node.walletStorage.address, StringComparison.Ordinal))
                            {
                                TransactionCache.addTransaction(transaction);
                            }
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
