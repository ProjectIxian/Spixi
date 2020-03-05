using IXICore;
using IXICore.Meta;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SPIXI
{
    class FileTransfer
    {
        public string uid = null;
        public string fileName = null;
        public ulong fileSize = 0;
        public byte[] preview = null;       // Additional preview data
        public byte[] sender = null;        // Additional sender address field

        public bool incoming = false;       // Incoming or outgoing flag
        public bool completed = false;

        public string filePath = "";
        public Stream fileStream = null;    

        public long lastTimeStamp = 0;      // Last activity timestamp in seconds
        public ulong lastPacket = 0;        // Last processed packet number

        public FileTransfer()
        {
            uid = Guid.NewGuid().ToString("N");
            fileName = "New File";
            fileSize = 0;
            preview = null;
            completed = false;
            lastTimeStamp = 0;
            lastPacket = 0;
        }

        public FileTransfer(string in_name, Stream in_stream)
        {
            uid = Guid.NewGuid().ToString("N");
            fileName = in_name;
            fileStream = in_stream;
            fileSize = (ulong)fileStream.Length;
            preview = null;
            completed = false;
            lastTimeStamp = 0;
            lastPacket = 0;
        }

        public FileTransfer(string in_uid, string in_name, ulong in_size, byte[] in_preview = null)
        {
            uid = in_uid;
            fileName = in_name;
            fileSize = in_size;
            preview = in_preview;
            completed = false;
            lastTimeStamp = 0;
            lastPacket = 0;
        }

        public FileTransfer(byte[] bytes)
        {
            try
            {
                using (MemoryStream m = new MemoryStream(bytes))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        uid = reader.ReadString();
                        fileName = reader.ReadString();
                        fileSize = reader.ReadUInt64();

                        int data_length = reader.ReadInt32();
                        if (data_length > 0)
                            preview = reader.ReadBytes(data_length);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while trying to construct FileTransfer from bytes: " + e);
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(uid);

                    writer.Write(fileName);

                    writer.Write(fileSize);

                    // Write the data
                    if (preview != null)
                    {
                        writer.Write(preview.Length);
                        writer.Write(preview);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                }
                return m.ToArray();
            }
        }

        public byte[] getPacketData(ulong packet_num)
        {
            byte[] data = new byte[Config.packetDataSize];

            if ((ulong)Config.packetDataSize * packet_num > fileSize + (ulong)Config.packetDataSize)
                return data;

            fileStream.Seek((int)packet_num * Config.packetDataSize, SeekOrigin.Begin);
            fileStream.Read(data, 0, Config.packetDataSize);

            return data;
        }

        public void updateActivity(ulong last_packet)
        {
            lastTimeStamp = Clock.getTimestamp();
            lastPacket = last_packet;
        }

    }

    class FilePacket
    {
        public string transfer_uid;
        public ulong packet_number = 0;
        public bool added = false;
        public long timestamp = 0;

        public FilePacket(string uid, ulong number)
        {
            transfer_uid = uid;
            packet_number = number;
            added = false;
            timestamp = Clock.getTimestamp();
        }

    }

    class TransferManager
    {
        private static Thread tm_thread = null;
        private static bool running = false;

        static List<FileTransfer> outgoingTransfers = new List<FileTransfer>();
        static List<FileTransfer> incomingTransfers = new List<FileTransfer>();

        static List<FilePacket> incomingPacketsLog = new List<FilePacket>();

        public static void start()
        {
            if (running)
            {
                return;
            }

            running = true;
            // Start the thread
            tm_thread = new Thread(onUpdate);
            tm_thread.Name = "TransferManager_Update_Thread";
            tm_thread.Start();
        }

        public static void onUpdate()
        {
            while (running)
            {
                lock (incomingTransfers)
                {
                    foreach(FileTransfer transfer in incomingTransfers)
                    {
                        if (transfer.completed || transfer.fileStream == null)
                            continue;

                        if (transfer.lastTimeStamp == 0)
                            continue;

                        if(Clock.getTimestamp() - transfer.lastTimeStamp > Config.packetRequestTimeout)
                        {
                            requestFileData(transfer.sender, transfer.uid, transfer.lastPacket);
                        }
                    }

                }

                Thread.Sleep(1000);

                Thread.Yield();
            }
        }

        public static void stop()
        {
            if (!running)
            {
                return;
            }

            running = false;
        }

        public static FileTransfer prepareFileTransfer(string filename, Stream stream, string filepath = null)
        {
            FileTransfer transfer = new FileTransfer(filename, stream);
            if (filepath != null)
                transfer.filePath = filepath;
            outgoingTransfers.Add(transfer);
            return transfer;
        }

        public static FileTransfer prepareIncomingFileTransfer(byte[] data, byte[] sender)
        {
            FileTransfer transfer = new FileTransfer(data);
            transfer.incoming = true;
            transfer.sender = sender;
            incomingTransfers.Add(transfer);

            Logging.info("File Transfer Size: {0} {1}", transfer.fileName, transfer.fileSize);

            return transfer;
        }

        // Check if a packet already exists in the incomingPackets log
        public static bool checkPacketLog(FilePacket packet)
        {
            foreach (FilePacket ipacket in incomingPacketsLog)
            {
                if (ipacket.transfer_uid.Equals(packet.transfer_uid) && ipacket.packet_number == packet.packet_number && ipacket.added)
                {
                    return true;
                }
            }
            return false;
        }

        public static void removePacketsForFileTransfer(string uid)
        {
            incomingPacketsLog.RemoveAll(item => item.transfer_uid == uid);
        }

        public static bool sendFileData(Friend friend, string uid, ulong packet_number)
        {
            FileTransfer transfer = TransferManager.getOutgoingTransfer(uid);
            if (transfer == null)
                return false;



            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(uid);

                    writer.Write(packet_number);

                    Logging.info("Fetching packet #{0}", packet_number);
                    byte[] data = transfer.getPacketData(packet_number);
                    
                    // Write the data
                    if (data != null)
                    {
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                }

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.fileData, m.ToArray());


                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message, false, false, false);

            }

            return true;
        }

        public static bool receiveFileData(byte[] data, byte[] sender)
        {
            Logging.info("Received File Data");

            byte[] file_data = null;
            try
            {
                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        string uid = reader.ReadString();
                        ulong packet_number = reader.ReadUInt64();

                        FilePacket packet = new FilePacket(uid, packet_number);
                        if (checkPacketLog(packet))
                            return false;

                        int data_length = reader.ReadInt32();
                        if (data_length > 0)
                            file_data = reader.ReadBytes(data_length);

                        Logging.info("File Uid: {0} Packet #{1}", uid, packet_number);

                        FileTransfer transfer = TransferManager.getIncomingTransfer(uid);
                        if (transfer == null)
                            return false;

                        // Check if the transfer is already completed
                        if (transfer.completed)
                            return false;

                        // Check if this is the next packet to process
                        if (transfer.lastPacket != packet_number)
                            return false;

                        incomingPacketsLog.Add(packet);

                        transfer.fileStream.Seek(Config.packetDataSize * (int)packet_number, SeekOrigin.Begin);
                        transfer.fileStream.Write(file_data, 0, file_data.Length);

                        transfer.updateActivity(packet_number + 1);

                        ulong new_packet_number = packet_number + 1;
                        if (new_packet_number * (ulong)Config.packetDataSize > transfer.fileSize + (ulong)Config.packetDataSize)
                        {
                            transfer.fileStream.Dispose();
                            transfer.completed = true;
                            removePacketsForFileTransfer(uid);
                            completeFileTransfer(sender, uid);
                            return true;
                        }
                        requestFileData(sender, uid, new_packet_number);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while receiving file data from bytes: " + e);
            }

            return true;
        }

        public static bool receiveAcceptFile(Friend friend, string uid)
        {
            FileTransfer transfer = TransferManager.getOutgoingTransfer(uid);
            if (transfer == null)
                return false;
          
            // Send first packet
            return TransferManager.sendFileData(friend, uid, 0);
        }


        public static FileTransfer getOutgoingTransfer(string uid)
        {
            FileTransfer transfer = outgoingTransfers.Where(x => x.uid.Contains(uid)).FirstOrDefault();
            return transfer;
        }

        public static FileTransfer getIncomingTransfer(string uid)
        {
            FileTransfer transfer = incomingTransfers.Where(x => x.uid.Contains(uid)).FirstOrDefault();
            return transfer;
        }

        public static void completeFileTransfer(byte[] sender, string uid)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
                return;

            FriendMessage fm = friend.messages.Find(x => x.transferId == uid);
            fm.completed = true;

            Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);

            if (friend.chat_page != null)
            {
                friend.chat_page.updateFile(uid, "100", true);
            }
        }

        public static void requestFileData(byte[] sender, string uid, ulong packet_number)
        {
            Logging.info("Requesting File Data, packet #{0}", packet_number);
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
                return;

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(uid);

                    writer.Write(packet_number);

                }

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestFileData, m.ToArray());

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message, false, false, false);

                if (friend.chat_page != null)
                {
                    FileTransfer transfer = TransferManager.getIncomingTransfer(uid);
                    if (transfer == null)
                        return;

                    ulong totalPackets = transfer.fileSize / (ulong)Config.packetDataSize;
                    ulong fp = 100 / totalPackets * (packet_number-1);
                    friend.chat_page.updateFile(uid, fp.ToString(), false);
                }
            }
        }

        public static void acceptFile(Friend friend, string uid)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(uid);
                }

                FileTransfer transfer = getIncomingTransfer(uid);
                if (transfer == null)
                    return;

                transfer.filePath = String.Format("{0}/Downloads/{1}", Config.spixiUserFolder, transfer.fileName);
                transfer.fileStream = File.Create(transfer.filePath);
                transfer.fileStream.SetLength((long)transfer.fileSize);

                FriendMessage fm = friend.messages.Find(x => x.transferId == uid);
                fm.filePath = transfer.filePath;
                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.acceptFile, m.ToArray());

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message);
            }
        }

    }
}
