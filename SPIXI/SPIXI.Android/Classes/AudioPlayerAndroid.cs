using Android.Media;
using IXICore.Meta;
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
            availableBuffers.Clear();
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
                format.SetInteger(MediaFormat.KeyBitRate, 8850);
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
        if (audioPlayer != null && running)
        {
            lock (pendingFrames)
            {
                if(pendingFrames.Count > 5)
                {
                    pendingFrames.RemoveAt(0);
                }

                pendingFrames.Add(audio_data);

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

    public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
    {
    }
}