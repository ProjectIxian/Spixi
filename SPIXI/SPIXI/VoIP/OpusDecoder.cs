using Concentus.Enums;
using IXICore.Meta;
using System;

namespace SPIXI.VoIP
{
    public class OpusDecoder : IAudioDecoder
    {
        Concentus.Structs.OpusDecoder decoder = null;
        bool running = false;

        int samples;
        int channels;

        int frameSize;

        IAudioDecoderCallback decodedDataCallback = null;
        bool returnFloat = false;

        public OpusDecoder(int samples, int channels, IAudioDecoderCallback decoder_callback, bool return_float = false)
        {
            this.samples = samples;
            this.channels = channels;
            frameSize = samples * 40 / 1000;
            decodedDataCallback = decoder_callback;
            returnFloat = return_float;
        }

        private byte[] shortsToBytes(short[] input, int offset, int size)
        {
            byte[] output = new byte[size * 2];
            for (int c = 0; c < size; c++)
            {
                output[(c * 2)] = (byte)(input[c + offset] & 0xFF);
                output[(c * 2) + 1] = (byte)((input[c + offset] >> 8) & 0xFF);
            }
            return output;
        }

        public void decode(byte[] data)
        {
            if (!running)
            {
                return;
            }
            if (returnFloat)
            {
                float[] output_buffer = new float[frameSize * 4 * 10];
                for (int offset = 0, packet_size = 0; offset < data.Length; offset += packet_size + 2)
                {
                    packet_size = BitConverter.ToInt16(data, offset);
                    int decoded_size = decoder.Decode(data, offset + 2, packet_size, output_buffer, 0, output_buffer.Length, false);
                    float[] send_buffer = new float[decoded_size];
                    Array.Copy(output_buffer, send_buffer, send_buffer.Length);
                    decodedDataCallback.onDecodedData(send_buffer);
                }
            }
            else
            {
                short[] output_buffer = new short[frameSize * 2 * 10];
                for (int offset = 0, packet_size = 0; offset < data.Length; offset += packet_size + 2)
                {
                    packet_size = BitConverter.ToInt16(data, offset);
                    int decoded_size = decoder.Decode(data, offset + 2, packet_size, output_buffer, 0, output_buffer.Length, false);
                    decodedDataCallback.onDecodedData(shortsToBytes(output_buffer, 0, decoded_size));
                }
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
            decoder = Concentus.Structs.OpusDecoder.Create(samples, channels);
        }

        public void stop()
        {
            if (!running)
            {
                return;
            }
            running = false;
            decoder = null;
        }

        public void Dispose()
        {
            stop();
        }
    }
}
