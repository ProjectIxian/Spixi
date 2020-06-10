using Android.Media;
using Android.Media.Audiofx;
using IXICore.Meta;
using SPIXI.Droid.Codecs;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderAndroid))]

public class AudioRecorderAndroid : IAudioRecorder, IAudioEncoderCallback
{
    private Action<byte[]> OnSoundDataReceived;

    private AudioRecord audioRecorder = null;
    private IAudioEncoder audioEncoder = null;
    private AcousticEchoCanceler echoCanceller = null;
    private NoiseSuppressor noiseSuppressor = null;

    bool running = false;

    int bufferSize = 0;
    byte[] buffer = null;


    List<byte[]> outputBuffers = new List<byte[]>();

    Thread recordThread = null;

    int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
    int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
    int channels = SPIXI.Meta.Config.VoIP_channels;

    public AudioRecorderAndroid()
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

        bufferSize = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Mono, Encoding.Pcm16bit);
        initEncoder(codec);
        initRecorder();

        recordThread = new Thread(recordLoop);
        recordThread.Start();
    }

    private void initRecorder()
    {
        Encoding encoding = Encoding.Pcm16bit;

        buffer = new byte[bufferSize];

        audioRecorder = new AudioRecord(
            // Hardware source of recording.
            AudioSource.VoiceCommunication,
            // Frequency
            sampleRate,
            // Mono or stereo
            ChannelIn.Mono,
            // Audio encoding
            encoding,
            // Length of the audio clip.
            bufferSize * 5
        );

        audioRecorder.StartRecording();

        echoCanceller = AcousticEchoCanceler.Create(audioRecorder.AudioSessionId);
        noiseSuppressor = NoiseSuppressor.Create(audioRecorder.AudioSessionId);
    }

    private void initEncoder(string codec)
    {
        switch (codec)
        {
            case "amrnb":
            case "amrwb":
                initHwEncoder(codec);
                break;

            case "opus":
                initOpusEncoder();
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }
    }

    private void initHwEncoder(string codec)
    {
        MediaFormat format = new MediaFormat();

        string mime_type = null;

        switch(codec)
        {
            case "amrnb":
                mime_type = MediaFormat.MimetypeAudioAmrNb;
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb":
                mime_type = MediaFormat.MimetypeAudioAmrWb;
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 18250);
                break;
        }

        if (mime_type != null)
        {
            format.SetString(MediaFormat.KeyMime, mime_type);
            format.SetInteger(MediaFormat.KeyChannelCount, 1);
            format.SetInteger(MediaFormat.KeyMaxInputSize, bufferSize);
            format.SetInteger(MediaFormat.KeyLatency, 1);
            format.SetInteger(MediaFormat.KeyPriority, 0);
            audioEncoder = new HwEncoder(mime_type, format, this);
            audioEncoder.start();
        }
    }

    private void initOpusEncoder()
    {
        audioEncoder = new OpusEncoder(sampleRate, 24000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY, this);
        audioEncoder.start();
    }

    public void stop()
    {
        if (!running)
        {
            return;
        }
        running = false;

        if (echoCanceller != null)
        {
            try
            {
                echoCanceller.Release();
                echoCanceller.Dispose();
            }
            catch (Exception)
            {

            }
            echoCanceller = null;
        }

        if (noiseSuppressor != null)
        {
            try
            {
                noiseSuppressor.Release();
                noiseSuppressor.Dispose();
            }
            catch (Exception)
            {

            }
            noiseSuppressor = null;
        }

        if (audioRecorder != null)
        {
            try
            {
                audioRecorder.Stop();
                audioRecorder.Release();
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

        buffer = null;
        bufferSize = 0;
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
                int num_bytes = 0;
                if (audioRecorder != null)
                {
                    num_bytes = audioRecorder.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    stop();
                }
                encode(num_bytes);
                sendAvailableData();
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while recording audio stream: " + e);
            }
        }
        recordThread = null;
    }

    private void encode(int num_bytes)
    {
        if (!running)
        {
            return;
        }
        if (num_bytes > 0)
        {
            byte[] encoded_bytes = audioEncoder.encode(buffer, 0, num_bytes);
            if(encoded_bytes != null)
            {
                onEncodedData(encoded_bytes);
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

            if (total_size >= 400)
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