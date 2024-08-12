using IXICore;
using IXICore.Meta;
using System;
using System.IO;

namespace SPIXI.Network
{
    class PendingMessage
    {
        public string filePath = null;

        public StreamMessage streamMessage = null;
        public bool sendToServer = false;
        public bool sendPushNotification = false;
        public bool removeAfterSending = false;

        public PendingMessage(StreamMessage stream_message, bool send_to_server, bool send_push_notification, bool remove_after_sending)
        {
            streamMessage = stream_message;
            sendToServer = send_to_server;
            sendPushNotification = send_push_notification;
            removeAfterSending = remove_after_sending;
        }

        public PendingMessage(string file_path)
        {
            filePath = file_path;
            fromBytes(File.ReadAllBytes(file_path));
        }

        private void fromBytes(byte[] bytes)
        {
            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    try
                    {
                        int sm_length = reader.ReadInt32();
                        byte[] sm_bytes = reader.ReadBytes(sm_length);
                        streamMessage = new StreamMessage(sm_bytes);

                        sendToServer = reader.ReadBoolean();
                        sendPushNotification = reader.ReadBoolean();
                        removeAfterSending = reader.ReadBoolean();
                    }
                    catch (Exception e)
                    {
                        Logging.error("Cannot create pending message from bytes: {0}", e);
                        throw e;
                    }
                }
            }
        }

        private byte[] toBytes()
        {
            using (MemoryStream m = new MemoryStream(5120))
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    byte[] data = streamMessage.getBytes();
                    writer.Write(data.Length);
                    writer.Write(data);

                    writer.Write(sendToServer);
                    writer.Write(sendPushNotification);
                    writer.Write(removeAfterSending);
                }
                return m.ToArray();
            }
        }

        public void save(string root_path)
        {
            string friend_path = Path.Combine(root_path, streamMessage.recipient.ToString());
            if (filePath == null)
            {
                filePath = Path.Combine(friend_path, Clock.getTimestampMillis() + "-" + Crypto.hashToString(streamMessage.id));
            }
            if(!Directory.Exists(friend_path))
            {
                Directory.CreateDirectory(friend_path);
            }
            File.WriteAllBytes(filePath, toBytes());
        }
    }
}
