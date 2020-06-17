using Concentus.Enums;
using Concentus.Structs;
using IXICore.Meta;
using System;
using System.Threading;

namespace SPIXI.VoIP
{
    public class OpusEncoder : IAudioEncoder
    {
        Concentus.Structs.OpusEncoder encoder = null;
        bool running = false;

        int samples;
        int bitRate;
        int channels;
        OpusApplication opusApplication = OpusApplication.OPUS_APPLICATION_AUDIO;

        int frameSize;

        IAudioEncoderCallback encodedDataCallback = null;

        Thread encodeThread = null;

        byte[] inputBuffer = null;
        int inputBufferPos = 0;

        public OpusEncoder(int samples, int bit_rate, int channels, OpusApplication application, IAudioEncoderCallback encoder_callback)
        {
            this.samples = samples;
            bitRate = bit_rate;
            this.channels = channels;
            opusApplication = application;
            frameSize = samples * 40 / 1000;
            encodedDataCallback = encoder_callback;
        }

        private short[] bytesToShorts(byte[] data, int offset, int size)
        {
            short[] output = new short[size / 2];
            for (int c = 0; c < output.Length; c++)
            {
                output[c] = (short)(((int)data[(c * 2) + offset]) << 0);
                output[c] += (short)(((int)data[(c * 2) + 1 + offset]) << 8);
            }

            return output;
        }

        public void encode(byte[] data, int offset, int size)
        {
            if (!running)
            {
                return;
            }

            lock(inputBuffer)
            {
                if(size > inputBuffer.Length - inputBufferPos)
                {
                    size = inputBuffer.Length - inputBufferPos;
                }
                if(size == 0)
                {
                    return;
                }
                Array.Copy(data, offset, inputBuffer, inputBufferPos, size);
                inputBufferPos += size;
            }

            return;
        }

        public void start()
        {
            if (running)
            {
                return;
            }
            running = true;

            inputBuffer = new byte[frameSize * 2 * 100];
            inputBufferPos = 0;
            
            encoder = Concentus.Structs.OpusEncoder.Create(samples, channels, opusApplication);
            encoder.Bitrate = bitRate;

            encodeThread = new Thread(encodeLoop);
            encodeThread.Start();
        }

        public void stop()
        {
            if (!running)
            {
                return;
            }
            running = false;

            lock (inputBuffer)
            {
                encoder = null;

                inputBuffer = null;
                inputBufferPos = 0;
            }
        }

        public void Dispose()
        {
            stop();
        }

        private byte[] encodeFrame(byte[] data, int offset)
        {
            byte[] output_buffer = new byte[1275];
            int byte_frame_size = frameSize * 2;
            short[] shorts = bytesToShorts(data, offset, byte_frame_size);
            int packet_size = encoder.Encode(shorts, 0, frameSize, output_buffer, 0, output_buffer.Length);

            byte[] trimmed_buffer = new byte[packet_size + 2];

            byte[] packet_size_bytes = BitConverter.GetBytes((short)packet_size);            
            trimmed_buffer[0] = packet_size_bytes[0];
            trimmed_buffer[1] = packet_size_bytes[1];
            
            Array.Copy(output_buffer, 0, trimmed_buffer, 2, packet_size);

            return trimmed_buffer;
        }

        private void encodeLoop()
        {
            while (running)
            {
                lock (inputBuffer)
                {
                    int b_frame_size = frameSize * 2;
                    int encoded_bytes = 0;
                    while (encoded_bytes + b_frame_size <= inputBufferPos)
                    {
                        try
                        {
                            byte[] data = encodeFrame(inputBuffer, encoded_bytes);
                            encodedDataCallback.onEncodedData(data);
                        }
                        catch (Exception e)
                        {
                            Logging.error("Exception occured while encoding audio stream: " + e);
                        }
                        encoded_bytes += b_frame_size;
                    }
                    if (encoded_bytes > 0)
                    {
                        inputBufferPos = inputBufferPos - encoded_bytes;
                        if (inputBufferPos > 0)
                        {
                            Array.Copy(inputBuffer, encoded_bytes, inputBuffer, 0, inputBufferPos);
                        }
                    }
                }
                Thread.Sleep(5);
            }
            encodeThread = null;
        }
    }
}
