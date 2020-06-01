using Android.Media;
using IXICore.Meta;
using SPIXI.Droid.Codecs;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderAndroid))]

public class AudioRecorderAndroid : IAudioRecorder, IAudioEncoderCallback
{
    private Action<byte[]> OnSoundDataReceived;

    private AudioRecord audioRecorder = null;
    private IAudioEncoder audioEncoder = null;

    bool running = false;

    int bufferSize = 0;
    byte[] buffer = null;


    List<byte[]> outputBuffers = new List<byte[]>();

    Thread recordThread = null;

    public AudioRecorderAndroid()
    {

    }

    public void start(string codec)
    {
        if (running)
        {
            Logging.warn("Audio recorder is already running.");
            return;
        }
        running = true;

        lock (outputBuffers)
        {
            outputBuffers.Clear();
        }

        initRecorder();
        initEncoder(codec);

        recordThread = new Thread(recordLoop);
        recordThread.Start();
    }

    private void initRecorder()
    {
        Encoding encoding = Encoding.Pcm16bit;

        bufferSize = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, encoding);
        buffer = new byte[bufferSize];

        audioRecorder = new AudioRecord(
            // Hardware source of recording.
            AudioSource.Mic,
            // Frequency
            44100,
            // Mono or stereo
            ChannelIn.Mono,
            // Audio encoding
            encoding,
            // Length of the audio clip.
            bufferSize * 5
        );

        audioRecorder.StartRecording();
    }

    private void initEncoder(string codec)
    {
        switch (codec)
        {
            case "amrnb":
            case "amrwb":
                initHwEncoder(codec);
                break;

            case "opus":
                initOpusEncoder();
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }
    }

    private void initHwEncoder(string codec)
    {
        MediaFormat format = new MediaFormat();

        string mime_type = null;

        switch(codec)
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
            audioEncoder = new HwEncoder(mime_type, format, this);
        }
    }

    private void initOpusEncoder()
    {
        audioEncoder = new OpusCodec(48000, 12000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
    }

    private void cleanUp()
    {
        running = false;

        if (audioRecorder != null)
        {
            try
            {
                audioRecorder.Stop();
                audioRecorder.Release();
            }catch(Exception)
            {

            }
            audioRecorder.Dispose();
            audioRecorder = null;
        }

        if (audioEncoder != null)
        {
            audioEncoder.stop();
            audioEncoder.Dispose();
            audioEncoder = null;
        }

        buffer = null;
        bufferSize = 0;
        lock(outputBuffers)
        {
            outputBuffers.Clear();
        }
    }

    public void stop()
    {
        cleanUp();
    }

    public void Dispose()
    {
        stop();
    }

    public bool isRunning()
    {
        return running;
    }

    public void setOnSoundDataReceived(Action<byte[]> on_sound_data_received)
    {
        OnSoundDataReceived = on_sound_data_received;
    }

    private void recordLoop()
    {
        while (running)
        {
            Thread.Sleep(10);

            try
            {
                int num_bytes = 0;
                if (audioRecorder != null)
                {
                    num_bytes = audioRecorder.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    stop();
                }
                encode(num_bytes);
                sendAvailableData();
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while recording audio stream: " + e);
            }
        }
        recordThread = null;
    }

    private void encode(int num_bytes)
    {
        if (num_bytes > 0)
        {
            byte[] encoded_bytes = audioEncoder.encode(buffer, 0, num_bytes);
            if(encoded_bytes != null)
            {
                outputBuffers.Add(encoded_bytes);
            }
        }
    }

    private void sendAvailableData()
    {
        byte[] data_to_send = null;
        lock (outputBuffers)
        {
            int total_size = 0;
            foreach (var buf in outputBuffers)
            {
                total_size += buf.Length;
            }

            if (total_size >= 400)
            {
                data_to_send = new byte[total_size];
                int data_written = 0;
                foreach (var buf in outputBuffers)
                {
                    Array.Copy(buf, 0, data_to_send, data_written, buf.Length);
                    data_written += buf.Length;
                }
                outputBuffers.Clear();
            }
        }
        if (data_to_send != null)
        {
            OnSoundDataReceived(data_to_send);
        }
    }

    public void onEncodedData(byte[] data)
    {
        lock (outputBuffers)
        {
            outputBuffers.Add(buffer);
        }
    }
}