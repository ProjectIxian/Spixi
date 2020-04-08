using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPIXI.VoIP
{
    public interface IAudioPlayer : IDisposable
    {
        void start();

        void stop();

        bool isRunning();

        Task<int> writeAsync(byte[] audio_data, int offset_in_bytes, int size_in_bytes);
    }
}
