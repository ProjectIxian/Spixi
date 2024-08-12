using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPIXI.VoIP
{
    public interface IAudioPlayer : IDisposable
    {
        void start(string codec);

        void stop();

        bool isRunning();

        int write(byte[] audio_data);

        void setVolume(float volume);
    }
}
