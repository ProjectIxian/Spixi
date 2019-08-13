using IXICore.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SPIXI
{
    class FileTransfer
    {
        public string uid;
        public string name;
        public byte[] preview;

        public FileTransfer()
        {
            uid = Guid.NewGuid().ToString("N");
            name = "New File";
            preview = null;
        }

        public FileTransfer(string in_name, byte[] in_preview = null)
        {
            uid = Guid.NewGuid().ToString("N");
            name = in_name;
            preview = in_preview;
        }

        public FileTransfer(string in_uid, string in_name, byte[] in_preview = null)
        {
            uid = in_uid;
            name = in_name;
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
                        name = reader.ReadString();

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

                    if (name != null)
                        writer.Write(name);

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

    }



    class TransferManager
    {
        static List<FileTransfer> transfers = new List<FileTransfer>();


        public static FileTransfer prepareFileTransfer(string filename, byte[] data)
        {
            FileTransfer transfer = new FileTransfer(filename, data);
            transfers.Add(transfer);

            return transfer;
        }




    }
}
