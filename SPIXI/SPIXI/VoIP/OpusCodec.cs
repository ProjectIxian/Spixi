using Concentus.Enums;
using Concentus.Structs;
using System;

namespace SPIXI.VoIP
{
    public class OpusCodec : IAudioEncoder, IAudioDecoder
    {
        OpusEncoder encoder = null;
        OpusDecoder decoder = null;
        bool running = false;

        int samples;
        int bitRate;
        int channels;
        OpusApplication opusApplication = OpusApplication.OPUS_APPLICATION_AUDIO;

        int outputBufferSize;
        int frameSize;

        IAudioDecoderCallback decodedDataCallback = null;

        public OpusCodec(int output_buffer_size, int samples, int bit_rate, int channels, OpusApplication application, IAudioDecoderCallback decoder_callback)
        {
            this.samples = samples;
            bitRate = bit_rate;
            this.channels = channels;
            opusApplication = application;
            outputBufferSize = output_buffer_size;
            frameSize = bitRate * 20 / 1000;
            decodedDataCallback = decoder_callback;
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

            int num_samples = OpusPacketInfo.GetNumSamples(data, 0, data.Length, decoder.SampleRate);
            short[] output_buffer = new short[num_samples * decoder.NumChannels];

            //for(int offset = 0; offset < data.Length; offset += frameSize)
            {
                int decoded_size = decoder.Decode(data, 0, data.Length, output_buffer, 0, num_samples, false);

                decodedDataCallback.onDecodedData(shortsToBytes(output_buffer, 0, decoded_size));
            }

            return null;
        }

        public byte[] encode(byte[] data, int offset, int size)
        {
            if (!running)
            {
                return null;
            }

            short[] shorts = bytesToShorts(data, offset, size);

            byte[] output_buffer = new byte[frameSize];

            int packet_size = encoder.Encode(shorts, 0, frameSize, output_buffer, 0, output_buffer.Length);

            byte[] trimmed_buffer = new byte[packet_size];
            Array.Copy(output_buffer, trimmed_buffer, packet_size);

            return trimmed_buffer;
        }

        public void start()
        {
            if (running)
            {
                return;
            }
            running = true;
            encoder = OpusEncoder.Create(samples, channels, opusApplication);
            encoder.Bitrate = bitRate;

            decoder = OpusDecoder.Create(samples, channels);
        }

        public void stop()
        {
            if (!running)
            {
                return;
            }
            running = false;
            encoder = null;
            decoder = null;
        }

        public void Dispose()
        {
            stop();
        }
    }
}
