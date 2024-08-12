﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPIXI.VoIP
{
    public interface IAudioRecorder : IDisposable
    {
        void setOnSoundDataReceived(Action<byte[]> on_sound_data_received);

        void start(string codec);

        void stop();

        bool isRunning();
    }
}
