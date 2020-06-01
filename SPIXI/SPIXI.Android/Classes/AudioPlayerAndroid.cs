using Android.Content;
using Android.Media;
using IXICore.Meta;
using SPIXI.Droid;
using SPIXI.Droid.Codecs;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayerAndroid))]

public class AudioPlayerAndroid :  IAudioPlayer, IAudioDecoderCallback
{
    private AudioTrack audioPlayer = null;
    private IAudioDecoder audioDecoder = null;

    private bool running = false;

    List<byte[]> pendingFrames = new List<byte[]>();
    List<int> availableBuffers = new List<int>();

    int bufferSize = 0;

    int delay = 5;

    Thread playThread = null;

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

        delay = 5;

        lock (pendingFrames)
        {
            pendingFrames.Clear();
            availableBuffers.Clear();
        }

        initPlayer();
        initDecoder(codec);

        playThread = new Thread(playLoop);
        playThread.Start();
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

        bufferSize = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, encoding) * 10;

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
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrNb);
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb":
                mime_type = MediaFormat.MimetypeAudioAmrWb;
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrWb);
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 18250);
                break;
        }

        if (mime_type != null)
        {
            format.SetInteger(MediaFormat.KeyChannelCount, 1);
            format.SetInteger(MediaFormat.KeyMaxInputSize, bufferSize);
            audioDecoder = new HwDecoder(mime_type, format, this);
        }
    }

    private void initOpusDecoder()
    {
        audioDecoder = new OpusCodec(48000, 12000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
    }

    private void playLoop()
    {
        while(running)
        {
            Thread.Sleep(10);
            if (delay > 0)
            {
                continue;
            }
            int buffer_index = -1;
            byte[] frame = null;
            lock (pendingFrames)
            {
                if (availableBuffers.Count > 0 && pendingFrames.Count > 0)
                {
                    buffer_index = availableBuffers[0];
                    frame = pendingFrames[0];
                    pendingFrames.RemoveAt(0);
                    availableBuffers.RemoveAt(0);
                }
            }
            if (buffer_index > -1)
            {
                decode(buffer_index, frame);
            }
        }
        playThread = null;
    }

    public int write(byte[] audio_data, int offset_in_bytes, int size_in_bytes)
    {
        if (audioPlayer != null && running)
        {
            lock (pendingFrames)
            {
                if(pendingFrames.Count > 10)
                {
                    pendingFrames.RemoveAt(0);
                }

                pendingFrames.Add(audio_data);

                if (delay > 0)
                {
                    delay--;
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

    private void decode(int buffer_index, byte[] data)
    {
        byte[] decoded_bytes = audioDecoder.decode(data, 0, data.Length);
        if(decoded_bytes != null)
        {
            onDecodedData(decoded_bytes);
        }
    }

    public void onDecodedData(byte[] data)
    {
        if (audioPlayer.Write(data, 0, data.Length) == 0)
        {
            // TODO drop frames
        }
    }
}