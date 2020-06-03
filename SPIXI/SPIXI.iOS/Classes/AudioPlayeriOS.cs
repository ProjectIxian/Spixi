using IXICore.Meta;
using SPIXI.VoIP;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayeriOS))]

public class AudioPlayeriOS : IAudioPlayer
{
    private bool running = false;

    public AudioPlayeriOS()
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

    public void start(string codec)
    {
        if (running)
        {
            Logging.warn("Audio player is already running.");
            return;
        }

        running = true;

        // TODO Init player
    }

    public void pause()
    {
        /*if (audioPlayer == null)
        {
            return;
        }

        audioPlayer.Pause();*/
    }

    public void stop()
    {
        /*if (audioPlayer == null)
        {
            return;
        }
        audioPlayer.Stop();
        audioPlayer.Release();
        audioPlayer.Dispose();
        audioPlayer = null;
        running = false;*/
    }

    public void Dispose()
    {
        stop();
    }

    public bool isRunning()
    {
        return running;
    }

    public int write(byte[] audio_data)
    {
        //return await audioPlayer.WriteAsync(audio_data, offset_in_bytes, size_in_bytes);
        return 0;
    }
}