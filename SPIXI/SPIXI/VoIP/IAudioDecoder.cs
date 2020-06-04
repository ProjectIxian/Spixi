using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.VoIP
{
    public interface IAudioDecoderCallback
    {
        void onDecodedData(byte[] data);
    }

    public interface IAudioDecoder : IDisposable
    {
        void start();
        void stop();
        byte[] decode(byte[] data);
    }
}
