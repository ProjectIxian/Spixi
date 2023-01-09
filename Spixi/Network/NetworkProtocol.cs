﻿using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace SPIXI.Network
{
    public class ProtocolMessage
    {
        public static void resubscribeEvents()
        {
            lock (NetworkClientManager.networkClients)
            {
                foreach (var client in NetworkClientManager.networkClients)
                {
                    if (client.isConnected() && client.helloReceived)
                    {
                        if (client.presenceAddress.type != 'M' && client.presenceAddress.type != 'H')
                        {
                            continue;
                        }

                        // Get presences
                        client.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'R' });
                        client.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'M' });
                        client.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'H' });

                        byte[] event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.all, new byte[0]);
                        client.sendData(ProtocolMessageCode.detachEvent, event_data);
                        subscribeToEvents(client);
                    }
                }
            }
        }

        private static void subscribeToEvents(RemoteEndpoint endpoint)
        {
            CoreProtocolMessage.subscribeToEvents(endpoint);

            byte[] friend_matcher = FriendList.getFriendCuckooFilter();
            if (friend_matcher != null)
            {
                byte[] event_data = NetworkEvents.prepareEventMessageData(NetworkEvents.Type.keepAlive, friend_matcher);
                endpoint.sendData(ProtocolMessageCode.attachEvent, event_data);
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
                                    CoreProtocolMessage.processHelloMessageV6(endpoint, reader);
                                }
                            }

                        }
                        break;


                    case ProtocolMessageCode.helloData:
                       using (MemoryStream m = new MemoryStream(data))
                        {
                            using (BinaryReader reader = new BinaryReader(m))
                            {
                                if (!CoreProtocolMessage.processHelloMessageV6(endpoint, reader))
                                {
                                    return;
                                }

                                ulong last_block_num = reader.ReadIxiVarUInt();
                                int bcLen = (int)reader.ReadIxiVarUInt();
                                byte[] block_checksum = reader.ReadBytes(bcLen);

                                endpoint.blockHeight = last_block_num;

                                int block_version = (int)reader.ReadIxiVarUInt();

                                if (endpoint.presenceAddress.type != 'C')
                                {
                                    ulong highest_block_height = IxianHandler.getHighestKnownNetworkBlockHeight();
                                    if (last_block_num + 10 < highest_block_height)
                                    {
                                        CoreProtocolMessage.sendBye(endpoint, ProtocolByeCode.tooFarBehind, string.Format("Your node is too far behind, your block height is {0}, highest network block height is {1}.", last_block_num, highest_block_height), highest_block_height.ToString(), true);
                                        return;
                                    }
                                }

                                // Process the hello data
                                endpoint.helloReceived = true;
                                NetworkClientManager.recalculateLocalTimeDifference();

                                if (endpoint.presenceAddress.type == 'R')
                                {
                                    string[] connected_servers = StreamClientManager.getConnectedClients(true);
                                    if (connected_servers.Count() == 1 || !connected_servers.Contains(StreamClientManager.primaryS2Address))
                                    {
                                        if (StreamClientManager.primaryS2Address == "")
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
                                }
                                else if (endpoint.presenceAddress.type == 'C')
                                {
                                    Friend f = FriendList.getFriend(endpoint.presence.wallet);
                                    if (f != null && f.bot)
                                    {
                                        StreamProcessor.sendGetBotInfo(f);
                                    }
                                }

                                if (endpoint.presenceAddress.type == 'M' || endpoint.presenceAddress.type == 'H')
                                {
                                    Node.setNetworkBlock(last_block_num, block_checksum, block_version);

                                    // Get random presences
                                    endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'R' });
                                    endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'M' });
                                    endpoint.sendData(ProtocolMessageCode.getRandomPresences, new byte[1] { (byte)'H' });

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
                            Presence p = PresenceList.updateFromBytes(data, 0);
                            if (p == null)
                                return;

                            Friend f = FriendList.getFriend(p.wallet);
                            if(f != null)
                            {
                                f.relayIP = p.addresses[0].address;
                            }
                        }
                        break;

                    case ProtocolMessageCode.keepAlivePresence:
                        {
                            Address address = null;
                            long last_seen = 0;
                            byte[] device_id = null;
                            bool updated = PresenceList.receiveKeepAlive(data, out address, out last_seen, out device_id, endpoint);
                            Presence p = PresenceList.getPresenceByAddress(address);
                            if (p == null)
                                return;

                            Friend f = FriendList.getFriend(p.wallet);
                            if (f != null)
                            {
                                f.relayIP = p.addresses[0].address;
                            }
                        }
                        break;

                    case ProtocolMessageCode.getPresence:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int walletLen = reader.ReadInt32();
                                    Address wallet = new Address(reader.ReadBytes(walletLen));

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
                                        Logging.warn(string.Format("Node has requested presence information about {0} that is not in our PL.", wallet.ToString()));
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.getPresence2:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int walletLen = (int)reader.ReadIxiVarUInt();
                                    Address wallet = new Address(reader.ReadBytes(walletLen));

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
                                        Logging.warn(string.Format("Node has requested presence information about {0} that is not in our PL.", wallet.ToString()));
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
                                    Address address = new Address(reader.ReadBytes(address_length));

                                    // Retrieve the latest balance
                                    IxiNumber balance = reader.ReadString();

                                    if (address.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
                                    {
                                        // Retrieve the blockheight for the balance
                                        ulong block_height = reader.ReadUInt64();

                                        if (block_height > Node.balance.blockHeight && (Node.balance.balance != balance || Node.balance.blockHeight == 0))
                                        {
                                            byte[] block_checksum = reader.ReadBytes(reader.ReadInt32());

                                            Node.balance.address = address;
                                            Node.balance.balance = balance;
                                            Node.balance.blockHeight = block_height;
                                            Node.balance.blockChecksum = block_checksum;
                                            Node.balance.verified = false;
                                        }
                                        Node.balance.lastUpdate = Clock.getTimestamp();
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.balance2:
                        {
                            using (MemoryStream m = new MemoryStream(data))
                            {
                                using (BinaryReader reader = new BinaryReader(m))
                                {
                                    int address_length = (int)reader.ReadIxiVarUInt();
                                    Address address = new Address(reader.ReadBytes(address_length));

                                    // Retrieve the latest balance
                                    IxiNumber balance = new IxiNumber(new BigInteger(reader.ReadBytes((int)reader.ReadIxiVarUInt())));

                                    if (address.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
                                    {
                                        // Retrieve the blockheight for the balance
                                        ulong block_height = reader.ReadIxiVarUInt();

                                        if (block_height > Node.balance.blockHeight && (Node.balance.balance != balance || Node.balance.blockHeight == 0))
                                        {
                                            byte[] block_checksum = reader.ReadBytes((int)reader.ReadIxiVarUInt());

                                            Node.balance.address = address;
                                            Node.balance.balance = balance;
                                            Node.balance.blockHeight = block_height;
                                            Node.balance.blockChecksum = block_checksum;
                                            Node.balance.verified = false;
                                        }
                                        Node.balance.lastUpdate = Clock.getTimestamp();
                                    }
                                }
                            }
                        }
                        break;

                    case ProtocolMessageCode.transactionData:
                        {
                            // TODO: check for errors/exceptions
                            Transaction transaction = new Transaction(data, true);

                            if (endpoint.presenceAddress.type == 'M' || endpoint.presenceAddress.type == 'H')
                            {
                                PendingTransactions.increaseReceivedCount(transaction.id, endpoint.presence.wallet);
                            }

                            TransactionCache.addUnconfirmedTransaction(transaction);

                            Node.tiv.receivedNewTransaction(transaction);
                        }
                        break;

                    case ProtocolMessageCode.transactionData2:
                        {
                            // TODO: check for errors/exceptions
                            Transaction transaction = new Transaction(data, true, true);

                            if (endpoint.presenceAddress.type == 'M' || endpoint.presenceAddress.type == 'H')
                            {
                                PendingTransactions.increaseReceivedCount(transaction.id, endpoint.presence.wallet);
                            }

                            TransactionCache.addUnconfirmedTransaction(transaction);

                            Node.tiv.receivedNewTransaction(transaction);
                        }
                        break;

                    case ProtocolMessageCode.bye:
                        CoreProtocolMessage.processBye(data, endpoint);
                        break;

                    case ProtocolMessageCode.blockHeaders3:
                        {
                            // Forward the block headers to the TIV handler
                            Node.tiv.receivedBlockHeaders3(data, endpoint);
                        }
                        break;

                    case ProtocolMessageCode.pitData2:
                        {
                            Node.tiv.receivedPIT2(data, endpoint);
                        }
                        break;

                    default:
                        break;

                }
            }
            catch (Exception e)
            {
                Logging.error("Error parsing network message. Details: {0}", e.ToString());
            }
        }
    }
}
