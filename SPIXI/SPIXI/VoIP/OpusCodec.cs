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

        int samples = 48000;
        int bitRate = 24000;
        int channels = 1;
        OpusApplication opusApplication = OpusApplication.OPUS_APPLICATION_AUDIO;

        public OpusCodec(int samples, int bit_rate, int channels, OpusApplication application)
        {
            this.samples = samples;
            bitRate = bit_rate;
            this.channels = channels;
            opusApplication = application;
        }

        public byte[] decode(byte[] data, int offset, int size)
        {
            if (!running)
            {
                return null;
            }

            int frame_size = 960;
            short[] output_buffer = new short[frame_size];

            frame_size = decoder.Decode(data, 0, size, output_buffer, 0, frame_size, false);

            byte[] bytes = new byte[frame_size * 2];
            Array.Copy(output_buffer, 0, bytes, 0, frame_size);

            return bytes;
        }

        public byte[] encode(byte[] data, int offset, int size)
        {
            if (!running)
            {
                return null;
            }

            short[] shorts = new short[size/2];
            Array.Copy(data, offset, shorts, 0, size);

            byte[] output_buffer = new byte[1275];
            int frame_size = 960;

            int packet_size = encoder.Encode(shorts, 0, frame_size, output_buffer, 0, output_buffer.Length);

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
