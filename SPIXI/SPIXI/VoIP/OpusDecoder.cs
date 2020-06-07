using Concentus.Enums;
using Concentus.Structs;
using System;

namespace SPIXI.VoIP
{
    public class OpusDecoder : IAudioDecoder
    {
        Concentus.Structs.OpusDecoder decoder = null;
        bool running = false;

        int samples;
        int bitRate;
        int channels;

        int frameSize;

        IAudioDecoderCallback decodedDataCallback = null;

        public OpusDecoder(int samples, int bit_rate, int channels, IAudioDecoderCallback decoder_callback)
        {
            this.samples = samples;
            bitRate = bit_rate;
            this.channels = channels;
            frameSize = bitRate * 20 / 1000;
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
            for(int offset = 0, packet_size = 0; offset < data.Length; offset += packet_size)
            {
                packet_size = BitConverter.ToInt16(data, offset) + 2;
                int decoded_size = decoder.Decode(data, offset + 2, data.Length - (offset + 2), output_buffer, 0, frameSize * 100, false);
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
