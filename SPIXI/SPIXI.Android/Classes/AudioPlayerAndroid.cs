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

        initPlayer();
        initDecoder(codec);
    }

    private void initPlayer()
    {
        Encoding encoding = Encoding.Pcm16bit;

        bufferSize = AudioTrack.GetMinBufferSize(sampleRate, ChannelOut.Mono, Encoding.Pcm16bit);
        Logging.info("Min. buffer size " + bufferSize);
        int new_buffer_size = CodecTools.getPcmFrameByteSize(sampleRate, bitRate, channels) * 200;
        if (bufferSize < new_buffer_size)
        {
            bufferSize = (int)(Math.Ceiling((decimal)new_buffer_size / bufferSize) * bufferSize);
        }
        Logging.info("Final buffer size " + bufferSize);

        // Prepare player
        AudioAttributes aa = new AudioAttributes.Builder()
                                                .SetContentType(AudioContentType.Speech)
                                                .SetFlags(AudioFlags.LowLatency)
                                                .SetUsage(AudioUsageKind.VoiceCommunication)
                                                .Build();

        AudioFormat af = new AudioFormat.Builder()
                                        .SetSampleRate(sampleRate)
                                        .SetChannelMask(ChannelOut.Mono)
                                        .SetEncoding(encoding)
                                        .Build();

        audioPlayer = new AudioTrack(aa, af, bufferSize, AudioTrackMode.Stream, 0);

        MainActivity.Instance.VolumeControlStream = Stream.VoiceCall;

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
                throw new Exception("Unknown player codec selected " + codec);
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
            format.SetInteger(MediaFormat.KeyLatency, 1);
            format.SetInteger(MediaFormat.KeyPriority, 0);
            audioDecoder = new HwDecoder(mime_type, format, this);
            audioDecoder.start();
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

        MainActivity.Instance.VolumeControlStream = Stream.NotificationDefault;

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
        audioDecoder.decode(data);
    }

    public void onDecodedData(byte[] data)
    {
        if (!running)
        {
            return;
        }
        audioPlayer.Write(data, 0, data.Length);
    }

    public void setVolume(float volume)
    {
        // do nothing
    }

    public void onDecodedData(float[] data)
    {
        // not needed
        throw new NotImplementedException();
    }
}