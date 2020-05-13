using Android.Content;
using Android.Media;
using IXICore.Meta;
using SPIXI.Droid;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerAndroid))]

public class AudioPlayerAndroid : MediaCodec.Callback, IAudioPlayer
{
    private AudioTrack audioPlayer = null;
    private MediaCodec audioDecoder = null;

    private bool running = false;

    List<byte[]> pendingFrames = new List<byte[]>();
    List<int> availableBuffers = new List<int>();

    int bufferSize = 0;

    int delay = 10;

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

        delay = 10;

        lock (pendingFrames)
        {
            pendingFrames.Clear();
            availableBuffers.Clear();
        }

        initPlayer();
        initDecoder(codec);
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
                                        .SetSampleRate(44100)
                                        .SetChannelMask(ChannelOut.Mono)
                                        .SetEncoding(encoding)
                                        .Build();

        bufferSize = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, encoding);
        Console.WriteLine("Playback buffer size is " + bufferSize);

        audioPlayer = new AudioTrack(aa, af, bufferSize * 5, AudioTrackMode.Stream, 0);

        // TODO implement dynamic volume control
        AudioManager am = (AudioManager) MainActivity.Instance.GetSystemService(Context.AudioService);
        audioPlayer.SetVolume(am.GetStreamVolume(Stream.VoiceCall));

        audioPlayer.Play();


    }

    private void initDecoder(string codec)
    {
        MediaFormat format = new MediaFormat();

        switch (codec)
        {
            case "amrnb":
                audioDecoder = MediaCodec.CreateDecoderByType(MediaFormat.MimetypeAudioAmrNb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrNb);
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb":
                audioDecoder = MediaCodec.CreateDecoderByType(MediaFormat.MimetypeAudioAmrWb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrWb);
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 18250);
                break;

            default:
                throw new Exception("Unknown playback codec selected " + codec);
        }
        format.SetInteger(MediaFormat.KeyChannelCount, 1);
        format.SetInteger(MediaFormat.KeyMaxInputSize, bufferSize);
        audioDecoder.SetCallback(this);
        audioDecoder.Configure(format, null, null, MediaCodecConfigFlags.None);
        audioDecoder.Start();
    }

    public int write(byte[] audio_data, int offset_in_bytes, int size_in_bytes)
    {
        if (audioPlayer != null && running)
        {
            lock (pendingFrames)
            {
                if(pendingFrames.Count > 20)
                {
                    pendingFrames.RemoveAt(0);
                }

                pendingFrames.Add(audio_data);

                if (delay > 0)
                {
                    delay--;
                    return audio_data.Length;
                }

                while (availableBuffers.Count > 0 && pendingFrames.Count > 0)
                {
                    decode(availableBuffers[0], pendingFrames[0]);
                    pendingFrames.RemoveAt(0);
                    availableBuffers.RemoveAt(0);
                }

                return audio_data.Length;
            }
        }
        return 0;
    }

    public void stop()
    {
        running = false;

        lock (pendingFrames)
        {
            pendingFrames.Clear();
            availableBuffers.Clear();
        }

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
            try
            {
                audioDecoder.Stop();
                audioDecoder.Release();
            }
            catch (Exception)
            {

            }
            audioDecoder.Dispose();
            audioDecoder = null;
        }

        bufferSize = 0;
    }

    public new void Dispose()
    {
        stop();
        base.Dispose();
    }

    public bool isRunning()
    {
        return running;
    }

    public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
    {
        Logging.error("Error occured in AudioPlayerAndroid callback: " + e);
    }

    private void decode(int buffer_index, byte[] data)
    {
        if(!running)
        {
            return;
        }
        try
        {
            var ib = audioDecoder.GetInputBuffer(buffer_index);
            ib.Clear();

            ib.Put(data);

            audioDecoder.QueueInputBuffer(buffer_index, 0, data.Length, 0, 0);
        }
        catch (Exception e)
        {
            Logging.error("Exception occured in audio decoder: " + e);
        }
    }

    public override void OnInputBufferAvailable(MediaCodec codec, int index)
    {
        if (!running)
        {
            return;
        }
        lock (pendingFrames)
        {
            availableBuffers.Add(index);
        }
    }

    public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
    {
        if (!running)
        {
            return;
        }
        try
        {
            var ob = audioDecoder.GetOutputBuffer(index);

            ob.Position(info.Offset);
            ob.Limit(info.Offset + info.Size);

            byte[] decoded_data = new byte[info.Size];
            ob.Get(decoded_data);

            if (audioPlayer.Write(decoded_data, 0, decoded_data.Length) == 0)
            {
                // TODO drop frames
            }

            audioDecoder.ReleaseOutputBuffer(index, false);
        }
        catch (Exception e)
        {
            Logging.error("Exception occured while playing audio stream: " + e);
        }
    }

    public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
    {
    }
}