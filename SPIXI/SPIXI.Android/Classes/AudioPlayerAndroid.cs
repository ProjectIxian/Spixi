using Android.Media;
using IXICore.Meta;
using SPIXI.VoIP;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerAndroid))]

public class AudioPlayerAndroid : IAudioPlayer
{
    static object audioPlayerLock = new object();

    private AudioTrack audioPlayer = null;

    private bool running = false;

    public AudioPlayerAndroid()
    {
        /*audioPlayer = new AudioTrack(
            // Stream type
            Android.Media.Stream.VoiceCall,
            // Frequency
            11025,
            // Mono or stereo
            ChannelOut.Mono,
            // Audio encoding
            Android.Media.Encoding.Pcm16bit,
            // Length of the audio clip.
            -1,
            // Mode. Stream or static.
            AudioTrackMode.Stream);*/


    }

    public void start()
    {
        if (running)
        {
            Logging.warn("Audio player is already running.");
            return;
        }

        running = true;

        AudioAttributes aa = new AudioAttributes.Builder()
                                                .SetContentType(AudioContentType.Speech)
                                                .SetLegacyStreamType(Stream.VoiceCall)
                                                .SetFlags(AudioFlags.LowLatency)
                                                .SetUsage(AudioUsageKind.VoiceCommunication)
                                                .Build();
        AudioFormat af = new AudioFormat.Builder()
                                        .SetSampleRate(11025)
                                        .SetChannelMask(ChannelOut.Mono)
                                        .SetEncoding(Encoding.Pcm16bit)
                                        .Build();

        audioPlayer = new AudioTrack(aa, af, AudioTrack.GetMinBufferSize(11025, ChannelOut.Mono, Encoding.Pcm16bit) * 100, AudioTrackMode.Stream, 0);

        audioPlayer.SetVolume(0.8f);

        audioPlayer.Play();
    }

    public async Task<int> writeAsync(byte[] audio_data, int offset_in_bytes, int size_in_bytes)
    {
        return await audioPlayer.WriteAsync(audio_data, offset_in_bytes, size_in_bytes);
    }

    public void pause()
    {
        if(audioPlayer == null)
        {
            return;
        }

        audioPlayer.Pause();
    }

    public void stop()
    {
        if (audioPlayer == null)
        {
            return;
        }
        audioPlayer.Stop();
        audioPlayer.Release();
        audioPlayer.Dispose();
        audioPlayer = null;
        running = false;
    }

    public void Dispose()
    {
        stop();
    }

    public bool isRunning()
    {
        return running;
    }
}