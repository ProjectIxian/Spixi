using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPIXI.VoIP
{
    public interface IAudioRecorder : IDisposable
    {
        void setOnSoundDataReceived(Action<byte[], int, int> on_sound_data_received);

        void start();

        void stop();

        bool isRunning();
    }
}
