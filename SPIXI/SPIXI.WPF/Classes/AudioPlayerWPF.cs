using IXICore.Meta;
using NAudio.Wave;
using SPIXI.VoIP;
using System;
using System.Threading;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerWPF))]


public class AudioPlayerWPF : IAudioPlayer, IAudioDecoderCallback
{
    private WaveOut audioPlayer = null;
    private IAudioDecoder audioDecoder = null;

    private bool running = false;

    private BufferedWaveProvider provider = null;

    int sampleRate = 48000;
    int bitRate = 16;
    int channels = 1;

    public AudioPlayerWPF()
    {
    }

    public void start(string codec)
    {
        if (running)
        {
            Logging.warn("Audio player is already running.");
            return;
        }

        running = true;

        initDecoder(codec);
        initPlayer();
    }

    private void initPlayer()
    {
        provider = new BufferedWaveProvider(new WaveFormat(sampleRate, bitRate, channels));
        audioPlayer = new WaveOut(WaveCallbackInfo.FunctionCallback());
        audioPlayer.Init(provider);
        audioPlayer.Play();

        var audioRecorder = new WaveIn(WaveCallbackInfo.FunctionCallback());
        audioRecorder.WaveFormat = new WaveFormat(sampleRate, bitRate, channels);
        audioRecorder.DataAvailable += (obj, wave_event) => {
            decode(((OpusCodec)audioDecoder).encode(wave_event.Buffer, 0, wave_event.BytesRecorded));
            //provider.AddSamples(wave_event.Buffer, 0, wave_event.BytesRecorded);
        };
        audioRecorder.BufferMilliseconds = 20;
        audioRecorder.NumberOfBuffers = 4;
        audioRecorder.DeviceNumber = 0;
        audioRecorder.StartRecording();
        while (1 == 1)
        {
            Thread.Sleep(10);
        }
    }

    private void initDecoder(string codec)
    {
        switch (codec)
        {
            case "opus":
                initOpusDecoder();
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }
    }

    private void initOpusDecoder()
    {
        int buffer_size = 1000;
        audioDecoder = new OpusCodec(buffer_size, 48000, 12000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP, this);
        audioDecoder.start();
    }

    public int write(byte[] audio_data)
    {
        if (!running)
        {
            return 0;
        }
        if (audioPlayer != null && running)
        {
            decode(audio_data);
            return audio_data.Length;
        }
        return 0;
    }

    public void stop()
    {
        if (!running)
        {
            return;
        }

        running = false;

        if (audioPlayer != null)
        {
            try
            {
                audioPlayer.Stop();
            }
            catch (Exception)
            {

            }

            audioPlayer.Dispose();
            audioPlayer = null;
        }

        if (audioDecoder != null)
        {
            audioDecoder.stop();
            audioDecoder.Dispose();
            audioDecoder = null;
        }

        if(provider != null)
        {
            provider.ClearBuffer();
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

    private void decode(byte[] data)
    {
        if (!running)
        {
            return;
        }
        byte[] decoded_bytes = audioDecoder.decode(data);
        if (decoded_bytes != null)
        {
            onDecodedData(decoded_bytes);
        }
    }

    public void onDecodedData(byte[] data)
    {
        provider.AddSamples(data, 0, data.Length);
    }
}