using IXICore.Meta;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderiOS))]

public class AudioRecorderiOS : IAudioRecorder
{
    private Action<byte[]> OnSoundDataReceived;

    bool stopRecording = false;
    bool running = false;

    public AudioRecorderiOS()
    {

    }

    public void start()
    {
        if (running)
        {
            Logging.warn("Audio recorder is already running.");
            return;
        }
        stopRecording = false;
        running = true;

        // TODO Init recorder

        Thread recordingThread = new Thread(readLoop);
        recordingThread.Start();
    }

    void readLoop()
    {
        byte[] buffer = new byte[8192];
        while (!stopRecording)
        {
            try
            {
                //int num_bytes = await audioRecorder.ReadAsync(buffer, 0, buffer.Length);
                //OnSoundDataReceived(buffer, 0, num_bytes);
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while recording audio stream: " + e);
                break;
            }
            Thread.Sleep(10);
        }

        // TODO Stop recorder and cleanup


        running = false;
    }
    public void stop()
    {
        stopRecording = true;
    }

    public void Dispose()
    {
        stop();
    }

    public bool isRunning()
    {
        return running;
    }

    public void setOnSoundDataReceived(Action<byte[]> on_sound_data_received)
    {
        OnSoundDataReceived = on_sound_data_received;
    }
}