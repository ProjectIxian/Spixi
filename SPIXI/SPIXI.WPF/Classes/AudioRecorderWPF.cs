using IXICore.Meta;
using NAudio.Wave;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderWPF))]

public class AudioRecorderWPF : IAudioRecorder, IAudioEncoderCallback
{
    private Action<byte[]> OnSoundDataReceived;

    private WaveIn audioRecorder = null;
    private IAudioEncoder audioEncoder = null;

    bool running = false;

    List<byte[]> outputBuffers = new List<byte[]>();

    Thread recordThread = null;

    int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
    int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
    int channels = SPIXI.Meta.Config.VoIP_channels;

    public AudioRecorderWPF()
    {

    }

    public void start(string codec)
    {
        if (running)
        {
            Logging.warn("Audio recorder is already running.");
            return;
        }
        running = true;

        lock (outputBuffers)
        {
            outputBuffers.Clear();
        }

        initEncoder(codec);
        initRecorder();

        recordThread = new Thread(recordLoop);
        recordThread.Start();
    }

    private void initRecorder()
    {
        audioRecorder = new WaveIn(WaveCallbackInfo.FunctionCallback());
        audioRecorder.WaveFormat = new WaveFormat(sampleRate, bitRate, channels);
        audioRecorder.DataAvailable += onDataAvailable;

        audioRecorder.BufferMilliseconds = 40;
        audioRecorder.NumberOfBuffers = 4;
        audioRecorder.DeviceNumber = 0;
        audioRecorder.StartRecording();
    }

    private void onDataAvailable(object obj, WaveInEventArgs wave_event)
    {
        encode(wave_event.Buffer, 0, wave_event.BytesRecorded);
    }

    private void initEncoder(string codec)
    {
        switch (codec)
        {
            case "opus":
                initOpusEncoder();
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }
    }

    private void initOpusEncoder()
    {
        audioEncoder = new OpusEncoder(sampleRate, 24000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP, this);
        audioEncoder.start();
    }

    public void stop()
    {
        if (!running)
        {
            return;
        }
        running = false;

        if (audioRecorder != null)
        {
            try
            {
                audioRecorder.StopRecording();
            }
            catch (Exception)
            {

            }
            audioRecorder.Dispose();
            audioRecorder = null;
        }

        if (audioEncoder != null)
        {
            audioEncoder.stop();
            audioEncoder.Dispose();
            audioEncoder = null;
        }

        lock (outputBuffers)
        {
            outputBuffers.Clear();
        }
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

    private void recordLoop()
    {
        while (running)
        {
            Thread.Sleep(10);

            try
            {
                sendAvailableData();
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while recording audio stream: " + e);
            }
        }
        recordThread = null;
    }

    private void encode(byte[] buffer, int offset, int size)
    {
        if (!running)
        {
            return;
        }
        if (size > 0)
        {
            try
            {
                byte[] encoded_bytes = audioEncoder.encode(buffer, offset, size);
                if (encoded_bytes != null)
                {
                    onEncodedData(encoded_bytes);
                }
            }catch(Exception e)
            {
                Logging.error("Exception occured in encode loop: " + e);
            }
        }
    }

    private void sendAvailableData()
    {
        if (!running)
        {
            return;
        }
        byte[] data_to_send = null;
        lock (outputBuffers)
        {
            int total_size = 0;
            foreach (var buf in outputBuffers)
            {
                total_size += buf.Length;
            }

            if (total_size >= 300)
            {
                data_to_send = new byte[total_size];
                int data_written = 0;
                foreach (var buf in outputBuffers)
                {
                    Array.Copy(buf, 0, data_to_send, data_written, buf.Length);
                    data_written += buf.Length;
                }
                outputBuffers.Clear();
            }
        }
        if (data_to_send != null)
        {
            OnSoundDataReceived(data_to_send);
        }
    }

    public void onEncodedData(byte[] data)
    {
        if (!running)
        {
            return;
        }
        lock (outputBuffers)
        {
            outputBuffers.Add(data);
        }
    }
}