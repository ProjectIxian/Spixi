using System;

namespace SPIXI.VoIP
{
    public interface IAudioEncoder : IDisposable
    {
        void start();
        void stop();
        byte[] encode(byte[] data, int offset, int size);
    }
}
