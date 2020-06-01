using System;
using Android.Media;
using IXICore.Meta;
using SPIXI.VoIP;

namespace SPIXI.Droid.Codecs
{
    public interface IAudioDecoderCallback
    {
        void onDecodedData(byte[] data);
    }

    class HwDecoder : MediaCodec.Callback, IAudioDecoder
    {
        MediaCodec audioDecoder = null;
        string encoderMimeType = null;
        MediaFormat mediaFormat = null;
        IAudioDecoderCallback decodedDataCallback = null;

        bool running = false;

        public HwDecoder(string mime_type, MediaFormat format, IAudioDecoderCallback decoded_data_callback)
        {
            encoderMimeType = mime_type;
            mediaFormat = format;
            decodedDataCallback = decoded_data_callback;
        }

        public byte[] decode(byte[] data, int offset, int size)
        {
            if (!running)
            {
                return null;
            }
            try
            {
                /*var ib = audioDecoder.GetInputBuffer(buffer_index);
                ib.Clear();

                ib.Put(data);

                audioDecoder.QueueInputBuffer(buffer_index, 0, data.Length, 0, 0);*/
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in audio decoder: " + e);
            }
            return null;
        }

        public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
        {
            Logging.error("Error occured in AudioPlayerAndroid callback: " + e);
        }

        public override void OnInputBufferAvailable(MediaCodec codec, int index)
        {
            if (!running)
            {
                return;
            }
            /*lock (pendingFrames)
            {
                availableBuffers.Add(index);
            }*/
        }

        public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
        {
            if (!running)
            {
                return;
            }
            try
            {
                var ob = audioDecoder.GetOutputBuffer(index);

                ob.Position(info.Offset);
                ob.Limit(info.Offset + info.Size);

                byte[] decoded_data = new byte[info.Size];
                ob.Get(decoded_data);

                audioDecoder.ReleaseOutputBuffer(index, false);

                decodedDataCallback.onDecodedData(decoded_data);
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while playing audio stream: " + e);
            }
        }

        public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
        {
        }

        public void start()
        {
            if (running)
            {
                return;
            }
            running = true;
            audioDecoder = MediaCodec.CreateEncoderByType(encoderMimeType);
            audioDecoder.SetCallback(this);
            audioDecoder.Configure(mediaFormat, null, null, MediaCodecConfigFlags.None);
            audioDecoder.Start();
        }

        public void stop()
        {
            if (!running)
            {
                return;
            }
            running = false;
            if (audioDecoder != null)
            {
                try
                {
                    audioDecoder.Stop();
                    audioDecoder.Release();
                }
                catch (Exception)
                {

                }
                audioDecoder.Dispose();
                audioDecoder = null;
            }
        }

        public new void Dispose()
        {
            stop();
            base.Dispose();
        }
    }
}