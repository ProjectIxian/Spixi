using System;

namespace SPIXI.VoIP
{
    public interface IAudioEncoderCallback
    {
        void onEncodedData(byte[] data);
    }

    public interface IAudioEncoder : IDisposable
    {
        void start();
        void stop();
        void encode(byte[] data, int offset, int size);
        void encode(short[] data, int offset, int size);
    }
}
