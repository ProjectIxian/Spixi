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

    int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
    int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
    int channels = SPIXI.Meta.Config.VoIP_channels;

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

        initPlayer();
        initDecoder(codec);
    }

    private void initPlayer()
    {
        provider = new BufferedWaveProvider(new WaveFormat(sampleRate, bitRate, channels));
        provider.BufferLength = sampleRate * channels * 40 * 50 / 1000;
        audioPlayer = new WaveOut(WaveCallbackInfo.FunctionCallback());
        audioPlayer.Init(provider);
        audioPlayer.Play();
    }

    private void initDecoder(string codec)
    {
        switch (codec)
        {
            case "opus":
                initOpusDecoder();
                break;

            default:
                throw new Exception("Unknown player codec selected " + codec);
        }
    }

    private void initOpusDecoder()
    {
        audioDecoder = new OpusDecoder(sampleRate, 1, this);
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
        audioDecoder.decode(data);
    }

    public void onDecodedData(byte[] data)
    {
        if (!running)
        {
            return;
        }
        if (provider != null)
        {
            provider.AddSamples(data, 0, data.Length);
        }
    }

    public void setVolume(float volume)
    {
        throw new NotImplementedException();
    }

    public void onDecodedData(float[] data)
    {
        throw new NotImplementedException();
    }
}