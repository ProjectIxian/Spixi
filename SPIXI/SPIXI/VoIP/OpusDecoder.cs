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

        public OpusDecoder(int samples, int channels, IAudioDecoderCallback decoder_callback)
        {
            this.samples = samples;
            this.channels = channels;
            frameSize = samples * 20 / 1000;
            decodedDataCallback = decoder_callback;
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

        public byte[] decode(byte[] data)
        {
            if (!running)
            {
                return null;
            }

            short[] output_buffer = new short[frameSize * 2 * 10];
            for(int offset = 0, packet_size = 0; offset < data.Length; offset += packet_size + 2)
            {
                packet_size = BitConverter.ToInt16(data, offset);
                int decoded_size = decoder.Decode(data, offset + 2, packet_size, output_buffer, 0, output_buffer.Length, false);
                decodedDataCallback.onDecodedData(shortsToBytes(output_buffer, 0, decoded_size));
            }

            return null;
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
