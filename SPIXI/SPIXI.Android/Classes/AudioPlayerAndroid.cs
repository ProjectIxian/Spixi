using Android.Content;
using Android.Media;
using IXICore.Meta;
using SPIXI.Droid;
using SPIXI.Droid.Codecs;
using SPIXI.VoIP;
using System;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerAndroid))]

public class AudioPlayerAndroid :  IAudioPlayer, IAudioDecoderCallback
{
    private AudioTrack audioPlayer = null;
    private IAudioDecoder audioDecoder = null;

    private bool running = false;

    int bufferSize = 0;

    int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
    int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
    int channels = SPIXI.Meta.Config.VoIP_channels;

    public AudioPlayerAndroid()
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
        Encoding encoding = Encoding.Pcm16bit;

        // Prepare player
        AudioAttributes aa = new AudioAttributes.Builder()
                                                .SetContentType(AudioContentType.Speech)
                                                .SetLegacyStreamType(Stream.VoiceCall)
                                                .SetFlags(AudioFlags.LowLatency)
                                                .SetUsage(AudioUsageKind.VoiceCommunication)
                                                .Build();

        AudioFormat af = new AudioFormat.Builder()
                                        .SetSampleRate(sampleRate)
                                        .SetChannelMask(ChannelOut.Mono)
                                        .SetEncoding(encoding)
                                        .Build();

        bufferSize = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Mono, encoding) * 10;

        audioPlayer = new AudioTrack(aa, af, bufferSize, AudioTrackMode.Stream, 0);

        // TODO implement dynamic volume control
        AudioManager am = (AudioManager) MainActivity.Instance.GetSystemService(Context.AudioService);
        audioPlayer.SetVolume(am.GetStreamVolume(Stream.VoiceCall));

        audioPlayer.Play();


    }

    private void initDecoder(string codec)
    {
        switch (codec)
        {
            case "amrnb":
            case "amrwb":
                initHwDecoder(codec);
                break;

            case "opus":
                initOpusDecoder();
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }
    }

    private void initHwDecoder(string codec)
    {
        MediaFormat format = new MediaFormat();

        string mime_type = null;

        switch (codec)
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
            audioDecoder = new HwDecoder(mime_type, format, this);
            audioDecoder.start();
        }
    }

    private void initOpusDecoder()
    {
        audioDecoder = new OpusDecoder(48000, 24000, 1, this);
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
                audioPlayer.Release();
            }
            catch (Exception)
            {

            }

            audioPlayer.Dispose();
            audioPlayer = null;
        }

        if(audioDecoder != null)
        {
            audioDecoder.stop();
            audioDecoder.Dispose();
            audioDecoder = null;
        }

        bufferSize = 0;
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
        if(decoded_bytes != null)
        {
            onDecodedData(decoded_bytes);
        }
    }

    public void onDecodedData(byte[] data)
    {
        if (!running)
        {
            return;
        }
        if (audioPlayer.Write(data, 0, data.Length) == 0)
        {
            // TODO drop frames
        }
    }
}