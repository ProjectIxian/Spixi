using IXICore.Meta;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderWPF))]

public class AudioRecorderWPF : IAudioRecorder
{
    private Action<byte[], int, int> OnSoundDataReceived;

    bool stopRecording = false;
    bool running = false;

    public AudioRecorderWPF()
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

        Thread recordingThread = new Thread(readLoopAsync);
        recordingThread.Start();
    }

    void readLoopAsync()
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

    public void setOnSoundDataReceived(Action<byte[], int, int> on_sound_data_received)
    {
        OnSoundDataReceived = on_sound_data_received;
    }
}