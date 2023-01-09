using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.VoIP
{
    public interface IAudioDecoderCallback
    {
        void onDecodedData(byte[] data);
        void onDecodedData(short[] data);
        void onDecodedData(float[] data);
    }

    public interface IAudioDecoder : IDisposable
    {
        void start();
        void stop();
        void decode(byte[] data);
    }
}
