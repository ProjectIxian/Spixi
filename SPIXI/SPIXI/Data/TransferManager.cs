using IXICore.Meta;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SPIXI
{
    class FileTransfer
    {
        public string uid = null;
        public string filename = null;
        public ulong filesize = 0;
        public byte[] preview = null;


        public bool incoming = false;

        public string filepath = null;

        private bool completed = false;
        public Stream fileStream = null;

        public FileTransfer()
        {
            uid = Guid.NewGuid().ToString("N");
            filename = "New File";
            filesize = 0;
            preview = null;
        }

     /*   public FileTransfer(string in_name, byte[] in_preview = null)
        {
            uid = Guid.NewGuid().ToString("N");
            filename = in_name;
            filesize = 0;
            preview = in_preview;
        }
        */
        public FileTransfer(string in_name, Stream in_stream)
        {
            uid = Guid.NewGuid().ToString("N");
            filename = in_name;
            fileStream = in_stream;
            filesize = (ulong)fileStream.Length;
            preview = null;
        }

        public FileTransfer(string in_uid, string in_name, ulong in_size, byte[] in_preview = null)
        {
            uid = in_uid;
            filename = in_name;
            filesize = in_size;
            preview = in_preview;
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
                        filename = reader.ReadString();
                        filesize = reader.ReadUInt64();

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
                    if (uid != null)
                        writer.Write(uid);

                    if (filename != null)
                        writer.Write(filename);

                    writer.Write(filesize);

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

            if ((ulong)Config.packetDataSize * packet_num > filesize + (ulong)Config.packetDataSize)
                return data;

            fileStream.Seek((int)packet_num * Config.packetDataSize, SeekOrigin.Begin);
            fileStream.Read(data, 0, Config.packetDataSize);

            return data;
        }

    }

    class TransferManager
    {
        static List<FileTransfer> outgoingTransfers = new List<FileTransfer>();
        static List<FileTransfer> incomingTransfers = new List<FileTransfer>();

        public static FileTransfer prepareFileTransfer(string filename, Stream stream)
        {
            FileTransfer transfer = new FileTransfer(filename, stream);
            outgoingTransfers.Add(transfer);

            return transfer;
        }

        public static FileTransfer prepareIncomingFileTransfer(byte[] data)
        {
            FileTransfer transfer = new FileTransfer(data);
            transfer.incoming = true;
            incomingTransfers.Add(transfer);

            Logging.info("File Transfer Size: {0} {1}", transfer.filename, transfer.filesize);

            return transfer;
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
                    if (uid != null)
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

                SpixiMessage spixi_message = new SpixiMessage(Guid.NewGuid().ToByteArray(), SpixiMessageCode.fileData, m.ToArray());


                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message);

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

                        int data_length = reader.ReadInt32();
                        if (data_length > 0)
                            file_data = reader.ReadBytes(data_length);

                        Logging.info("File Uid: {0} Packet #{1}", uid, packet_number);

                        FileTransfer transfer = TransferManager.getIncomingTransfer(uid);
                        if (transfer == null)
                            return false;

                        //transfer.fileStream.Position = 0;
                        //transfer.fileStream.Write(file_data, Config.packetDataSize * (int)packet_number, file_data.Length);
                        transfer.fileStream.Write(file_data, 0, file_data.Length);

                        ulong new_packet_number = packet_number + 1;
                        if (new_packet_number * (ulong)Config.packetDataSize > transfer.filesize + (ulong)Config.packetDataSize)
                        {
                            transfer.fileStream.Dispose();
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

        public static void requestFileData(byte[] sender, string uid, ulong packet_number)
        {
            Logging.info("Requesting File Data, packet #{0}", packet_number);
            Friend friend = FriendList.getFriend(sender);

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    if (uid != null)
                        writer.Write(uid);

                    writer.Write(packet_number);

                }

                SpixiMessage spixi_message = new SpixiMessage(Guid.NewGuid().ToByteArray(), SpixiMessageCode.requestFileData, m.ToArray());

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

        public static void acceptFile(Friend friend, string uid)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    if (uid != null)
                        writer.Write(uid);
                }

                FileTransfer transfer = getIncomingTransfer(uid);
                if (transfer == null)
                    return;

                transfer.filepath = String.Format("{0}/Downloads/{1}", Config.spixiUserFolder, transfer.filename);
                transfer.fileStream = File.Create(transfer.filepath);


                SpixiMessage spixi_message = new SpixiMessage(Guid.NewGuid().ToByteArray(), SpixiMessageCode.acceptFile, m.ToArray());

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
