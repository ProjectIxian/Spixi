using Android.Media;
using IXICore.Meta;
using SPIXI;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerAndroid))]

public class AudioPlayerAndroid : MediaCodec.Callback, IAudioPlayer
{
    private AudioTrack audioPlayer = null;
    private MediaCodec audioDecoder = null;

    private bool running = false;

    List<byte[]> pendingFrames = new List<byte[]>();
    List<int> availableBuffers = new List<int>();

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

        lock (pendingFrames)
        {
            pendingFrames.Clear();
        }

        Encoding encoding = Encoding.Pcm16bit;

        // Prepare player
        AudioAttributes aa = new AudioAttributes.Builder()
                                                .SetContentType(AudioContentType.Speech)
                                                .SetLegacyStreamType(Stream.VoiceCall)
                                                .SetFlags(AudioFlags.LowLatency)
                                                .SetUsage(AudioUsageKind.VoiceCommunication)
                                                .Build();

        AudioFormat af = new AudioFormat.Builder()
                                        .SetSampleRate(44100)
                                        .SetChannelMask(ChannelOut.Mono)
                                        .SetEncoding(encoding)
                                        .Build();

        int buffer_size = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, encoding);
        Console.WriteLine("Playback buffer size is " + buffer_size);

        audioPlayer = new AudioTrack(aa, af, buffer_size, AudioTrackMode.Stream, 0);

        audioPlayer.SetVolume(0.8f);

        audioPlayer.Play();

        MediaFormat format = new MediaFormat();

        switch (codec)
        {
            case "amrnb":
            case "amrwb":
                audioDecoder = MediaCodec.CreateDecoderByType(MediaFormat.MimetypeAudioAmrNb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrNb);
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb1":
                audioDecoder = MediaCodec.CreateDecoderByType(MediaFormat.MimetypeAudioAmrWb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrWb);
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 6600);
                break;

            case "ilbc":
                break;

            default:
                throw new Exception("Unknown playback codec selected " + codec);
        }
        format.SetInteger(MediaFormat.KeyChannelCount, 1);
        format.SetInteger(MediaFormat.KeyMaxInputSize, buffer_size);
        audioDecoder.SetCallback(this);
        audioDecoder.Configure(format, null, null, MediaCodecConfigFlags.None);
        audioDecoder.Start();
    }

    public int write(byte[] audio_data, int offset_in_bytes, int size_in_bytes)
    {
        if (audioPlayer != null)
        {
            lock (pendingFrames)
            {
                Console.WriteLine("Received encoded bytes: " + audio_data.Length);

                if(pendingFrames.Count > 50)
                {
                    pendingFrames.RemoveAt(0);
                }

                pendingFrames.Add(audio_data);

                if (availableBuffers.Count > 0)
                {
                    OnInputBufferAvailable(null, availableBuffers[0]);
                    availableBuffers.RemoveAt(0);
                }

                return audio_data.Length;
            }
        }
        return 0;
    }

    public void pause()
    {
        if (audioPlayer == null)
        {
            audioPlayer.Pause();
        }
    }

    public void stop()
    {
        lock (pendingFrames)
        {
            pendingFrames.Clear();
        }

        if (audioPlayer != null)
        {
            audioPlayer.Stop();
            audioPlayer.Release();
            audioPlayer.Dispose();
            audioPlayer = null;
        }

        if(audioDecoder != null)
        {
            audioDecoder.Stop();
            audioDecoder.Release();
            audioDecoder.Dispose();
            audioDecoder = null;
        }
        
        running = false;
    }

    public new void Dispose()
    {
        base.Dispose();
        stop();
    }

    public bool isRunning()
    {
        return running;
    }

    public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
    {
        Logging.error("Error occured in AudioPlayerAndroid callback: " + e);
    }

    public override void OnInputBufferAvailable(MediaCodec codec, int index)
    {
        byte[] data = null;
        lock (pendingFrames)
        {
            if (pendingFrames.Count == 0)
            {
                availableBuffers.Add(index);
                return;
            }

            data = pendingFrames[0];
            pendingFrames.RemoveAt(0);
        }

        int bytes_processed = 0;
        try
        {
            var ib = audioDecoder.GetInputBuffer(index);
            ib.Clear();

            ib.Put(data);

            audioDecoder.QueueInputBuffer(index, 0, data.Length, 0, 0);

            bytes_processed += data.Length;
        }
        catch (Exception e)
        {
            Logging.error("Exception occured in audio decoder: " + e);
        }
    }

    public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
    {
        var ob = audioDecoder.GetOutputBuffer(index);

        ob.Position(info.Offset);
        ob.Limit(info.Offset + info.Size);

        byte[] encoded_data = new byte[info.Size];
        ob.Get(encoded_data);

        Console.WriteLine("Decoded bytes: " + encoded_data.Length);

        if (audioPlayer.Write(encoded_data, 0, encoded_data.Length) == 0)
        {
            // TODO drop frames
            return;
        }

        // TODO implement frame skipping for audioPlayer - this will probably be handled by the custom codec wrapper
        //((SpixiContentPage)App.Current.MainPage.Navigation.NavigationStack.Last()).displayCallBar(VoIPManager.currentCallSessionId, "pos: " + audioPlayer.PlaybackHeadPosition + " uc: " + audioPlayer.UnderrunCount);
        audioPlayer.Write(encoded_data, 0, encoded_data.Length);

        audioDecoder.ReleaseOutputBuffer(index, false);
    }

    public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
    {
    }
}