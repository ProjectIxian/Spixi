using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.VoIP
{
    public interface IAudioDecoder : IDisposable
    {
        void start();
        void stop();
        byte[] decode(byte[] data, int offset, int size);
    }
}
