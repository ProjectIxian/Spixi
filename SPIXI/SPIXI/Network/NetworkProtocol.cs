using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SPIXI.Network
{
    public class ProtocolMessage
    {
        public static ProtocolMessageCode waitingFor = 0;
        public static bool blocked = false;

        public static void setWaitFor(ProtocolMessageCode value)
        {
            waitingFor = value;
            blocked = true;
        }

        public static void wait(int timeout_seconds)
        {
            DateTime start = DateTime.UtcNow;
            while (blocked)
            {
                if ((DateTime.UtcNow - start).TotalSeconds > timeout_seconds)
                {
                    Logging.warn("Timeout occured while waiting for " + waitingFor);
                    break;
                }
                Thread.Sleep(250);
            }
        }

        public static void resubscribeEvents()
        {
            lock (NetworkClientManager.networkClients)
            {
                foreach (var client in NetworkClientManager.networkClients)
                {
                    if (client.isConnected() && client.helloReceived)
                    {
                        byte[] event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.all, new byte[0]);
                        client.sendData(ProtocolMessageCode.detachEvent, event_data);
                        subscribeToEvents(client);
                    }
                }
            }
        }

        private static void subscribeToEvents(RemoteEndpoint endpoint)
        {
            if (endpoint.presenceAddress.type != 'M')
            {
                return;
            }
            // Get presences
            endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'R' });
            endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'M' });

            // TODO TODO TODO events can be optimized as there is no real need to subscribe them to every connected node

            // Subscribe to transaction events
            byte[] event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.transactionFrom, IxianHandler.getWalletStorage().getPrimaryAddress());
            endpoint.sendData(ProtocolMessageCode.attachEvent, event_data);

            event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.transactionTo, IxianHandler.getWalletStorage().getPrimaryAddress());
            endpoint.sendData(ProtocolMessageCode.attachEvent, event_data);

            event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.balance, IxianHandler.getWalletStorage().getPrimaryAddress());
            endpoint.sendData(ProtocolMessageCode.attachEvent, event_data);


            List<byte[]> match_addresses = FriendList.getHiddenMatchAddresses();
            if (match_addresses != null)
            {
                foreach (var address in match_addresses)
                {
                    event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.keepAlive, address);
                    endpoint.sendData(ProtocolMessageCode.attachEvent, event_data);
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
                                    if (CoreProtocolMessage.processHelloMessage(endpoint, reader))
                                    {
                                        byte[] challenge_response = null;

                                        int challenge_len = reader.ReadInt32();
                                        byte[] challenge = reader.ReadBytes(challenge_len);

                                        challenge_response = CryptoManager.lib.getSignature(challenge, IxianHandler.getWalletStorage().getPrimaryPrivateKey());

                                        CoreProtocolMessage.sendHelloMessage(endpoint, true, challenge_response);
                                        endpoint.helloReceived = true;
                                        return;
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
                                if (!CoreProtocolMessage.processHelloMessage(endpoint, reader))
                                {
                                    return;
                                }

                                ulong last_block_num = reader.ReadUInt64();
                                int bcLen = reader.ReadInt32();
                                byte[] block_checksum = reader.ReadBytes(bcLen);
                                int wsLen = reader.ReadInt32();
                                byte[] walletstate_checksum = reader.ReadBytes(wsLen);
                                int consensus = reader.ReadInt32(); // deprecated

                                endpoint.blockHeight = last_block_num;

                                int block_version = reader.ReadInt32();

                                Node.setLastBlock(last_block_num, block_checksum, walletstate_checksum, block_version);

                                // Check for legacy level
                                ulong legacy_level = reader.ReadUInt64(); // deprecated

                                int challenge_response_len = reader.ReadInt32();
                                byte[] challenge_response = reader.ReadBytes(challenge_response_len);
                                if (!CryptoManager.lib.verifySignature(endpoint.challenge, endpoint.serverPubKey, challenge_response))
                                {
                                    CoreProtocolMessage.sendBye(endpoint, ProtocolByeCode.authFailed, string.Format("Invalid challenge response."), "", true);
                                    return;
                                }

                                ulong highest_block_height = IxianHandler.getHighestKnownNetworkBlockHeight();
                                if (last_block_num + 10 < highest_block_height)
                                {
                                    CoreProtocolMessage.sendBye(endpoint, ProtocolByeCode.tooFarBehind, string.Format("Your node is too far behind, your block height is {0}, highest network block height is {1}.", last_block_num, highest_block_height), highest_block_height.ToString(), true);
                                    return;
                                }

                                // Process the hello data
                                endpoint.helloReceived = true;
                                NetworkClientManager.recalculateLocalTimeDifference();

                                if (endpoint.presenceAddress.type == 'R')
                                {
                                    string[] connected_servers = StreamClientManager.getConnectedClients(true);
                                    if (connected_servers.Count() == 1 || !connected_servers.Contains(StreamClientManager.primaryS2Address))
                                    {
                                        if(StreamClientManager.primaryS2Address == "")
                                        {
                                            FriendList.requestAllFriendsPresences();
                                        }
                                        // TODO set the primary s2 host more efficiently, perhaps allow for multiple s2 primary hosts
                                        StreamClientManager.primaryS2Address = endpoint.getFullAddress(true);
                                        // TODO TODO do not set if directly connectable
                                        IxianHandler.publicIP = endpoint.address;
                                        IxianHandler.publicPort = endpoint.incomingPort;
                                        PresenceList.forceSendKeepAlive = true;
                                        Logging.info("Forcing KA from networkprotocol");
                                    }
                                }else if (endpoint.presenceAddress.type == 'C')
                                {
                                    Friend f = FriendList.getFriend(endpoint.presence.wallet);
                                    if (f != null && f.bot)
                                    {
                                        StreamProcessor.sendGetMessages(f);
                                    }
                                }

                                if(endpoint.presenceAddress.type == 'M')
                                {
                                    subscribeToEvents(endpoint);
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.s2data:
                        {
                            StreamProcessor.receiveData(data, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.updatePresence:
                        {
                            Logging.info("NET: Receiving presence list update");
                            // Parse the data and update entries in the presence list
                            Presence p = PresenceList.updateFromBytes(data);
                        }
                        break;

                    case ProtocolMessageCode.keepAlivePresence:
                        {
                            byte[] address = null;
                            bool updated = PresenceList.receiveKeepAlive(data, out address, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.getPresence:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int walletLen = reader.ReadInt32();
                                    byte[] wallet = reader.ReadBytes(walletLen);

                                    Presence p = PresenceList.getPresenceByAddress(wallet);
                                    if (p != null)
                                    {
                                        lock (p)
                                        {
                                            byte[][] presence_chunks = p.getByteChunks();
                                            foreach (byte[] presence_chunk in presence_chunks)
                                            {
                                                endpoint.sendData(ProtocolMessageCode.updatePresence, presence_chunk, null);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // TODO blacklisting point
                                        Logging.warn(string.Format("Node has requested presence information about {0} that is not in our PL.", Base58Check.Base58CheckEncoding.EncodePlain(wallet)));
                                    }
                                }
                            }
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

                                    if(address.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
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
                            Transaction transaction = new Transaction(data, true);
                            TransactionCache.addTransaction(transaction);
                        }
                        break;

                    case ProtocolMessageCode.newTransaction:
                        {
                            Transaction transaction = new Transaction(data, true);
                            TransactionCache.addTransaction(transaction);
                        }
                        break;

                    case ProtocolMessageCode.bye:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    endpoint.stop();

                                    bool byeV1 = false;
                                    try
                                    {
                                        ProtocolByeCode byeCode = (ProtocolByeCode)reader.ReadInt32();
                                        string byeMessage = reader.ReadString();
                                        string byeData = reader.ReadString();

                                        byeV1 = true;

                                        switch (byeCode)
                                        {
                                            case ProtocolByeCode.bye: // all good
                                                break;

                                            case ProtocolByeCode.forked: // forked node disconnected
                                                Logging.info(string.Format("Disconnected with message: {0} {1}", byeMessage, byeData));
                                                break;

                                            case ProtocolByeCode.deprecated: // deprecated node disconnected
                                                Logging.info(string.Format("Disconnected with message: {0} {1}", byeMessage, byeData));
                                                break;

                                            case ProtocolByeCode.incorrectIp: // incorrect IP
                                                if (IxiUtils.validateIPv4(byeData))
                                                {
                                                    if (NetworkClientManager.getConnectedClients(true).Length < 2)
                                                    {
                                                        // TODO TODO do not set if not directly connectable
                                                        IxianHandler.publicIP = byeData;
                                                        Logging.info("Changed internal IP Address to " + byeData + ", reconnecting");
                                                    }
                                                }
                                                break;

                                            case ProtocolByeCode.notConnectable: // not connectable from the internet
                                                Logging.error("This node must be connectable from the internet, to connect to the network.");
                                                Logging.error("Please setup uPNP and/or port forwarding on your router for port " + IxianHandler.publicPort + ".");
                                                NetworkServer.connectable = false;
                                                break;

                                            default:
                                                Logging.warn(string.Format("Disconnected with message: {0} {1}", byeMessage, byeData));
                                                break;
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }
                                    if (byeV1)
                                    {
                                        return;
                                    }

                                    reader.BaseStream.Seek(0, SeekOrigin.Begin);

                                    // Retrieve the message
                                    string message = reader.ReadString();

                                    if (message.Length > 0)
                                        Logging.info(string.Format("Disconnected with message: {0}", message));
                                    else
                                        Logging.info("Disconnected");
                                }
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

            if (waitingFor == code)
            {
                blocked = false;
            }
        }
    }
}
