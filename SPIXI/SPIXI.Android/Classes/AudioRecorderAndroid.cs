using Android.Media;
using IXICore.Meta;
using SPIXI.VoIP;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioRecorderAndroid))]

public class AudioRecorderAndroid : MediaCodec.Callback, IAudioRecorder
{
    private Action<byte[]> OnSoundDataReceived;

    private AudioRecord audioRecorder = null;
    private MediaCodec audioEncoder = null;

    bool running = false;

    byte[] buffer = null;


    byte[] outputBuffer = null;
    int outputBufferFrameCount = 0;


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

        Encoding encoding = Encoding.Pcm16bit;

        MediaFormat format = new MediaFormat();

        switch (codec)
        {
            case "amrnb":
            case "amrwb":
                audioEncoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAmrNb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrNb);
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb1":
                audioEncoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAmrWb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrWb);
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 8850);
                break;

            case "ilbc":
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }

        int buffer_size = AudioTrack.GetMinBufferSize(44100, ChannelOut.Mono, encoding);
        buffer = new byte[buffer_size];

        format.SetInteger(MediaFormat.KeyChannelCount, 1);
        format.SetInteger(MediaFormat.KeyMaxInputSize, buffer_size);
        audioEncoder.SetCallback(this);
        audioEncoder.Configure(format, null, null, MediaCodecConfigFlags.Encode);
        audioEncoder.Start();

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
            buffer_size
        );

        audioRecorder.StartRecording(); 
    }

    private void cleanUp()
    {
        running = false;

        if (audioEncoder != null)
        {
            audioEncoder.Stop();
            audioEncoder.Release();
            audioEncoder.Dispose();
            audioEncoder = null;
        }

        if (audioRecorder != null)
        {
            audioRecorder.Stop();
            audioRecorder.Release();
            audioRecorder.Dispose();
            audioRecorder = null;
        }

        buffer = null;
        if (outputBuffer != null)
        {
            lock (outputBuffer)
            {
                outputBuffer = null;
                outputBufferFrameCount = 0;
            }
        }
    }

    public void stop()
    {
        cleanUp();
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

    public void setOnSoundDataReceived(Action<byte[]> on_sound_data_received)
    {
        OnSoundDataReceived = on_sound_data_received;
    }

    public override void OnError(MediaCodec codec, MediaCodec.CodecException e)
    {
        Logging.error("Error occured in AudioRecorderAndroid callback: " + e);
    }

    public override void OnInputBufferAvailable(MediaCodec codec, int index)
    {
        if(!running)
        {
            return;
        }
        try
        {
            int num_bytes = 0;
            if (audioRecorder != null)
            {
                num_bytes = audioRecorder.Read(buffer, 0, buffer.Length, 0);
            }else
            {
                return;
            }

            var ib = audioEncoder.GetInputBuffer(index);
            ib.Clear();

            ib.Put(buffer);

            audioEncoder.QueueInputBuffer(index, 0, num_bytes, 0, 0);
        }
        catch (Exception e)
        {
            Logging.error("Exception occured while recording audio stream: " + e);
        }
    }

    public override void OnOutputBufferAvailable(MediaCodec codec, int index, MediaCodec.BufferInfo info)
    {
        if (!running)
        {
            return;
        }
        var ob = audioEncoder.GetOutputBuffer(index);

        ob.Position(info.Offset);
        ob.Limit(info.Offset + info.Size);


        if (outputBuffer == null)
        {
            outputBuffer = new byte[info.Size * 10];
            outputBufferFrameCount = 0;
        }

        byte[] data_to_send = null;

        lock (outputBuffer)
        {
            ob.Get(outputBuffer, outputBufferFrameCount * info.Size, info.Size);
            audioEncoder.ReleaseOutputBuffer(index, false);

            outputBufferFrameCount++;

            if (outputBufferFrameCount == 10)
            {
                data_to_send = (byte[])outputBuffer.Clone();
                outputBufferFrameCount = 0;
            }
        }
        if (data_to_send != null)
        {
            Task.Run(() =>
            {
                OnSoundDataReceived(data_to_send);
            });
        }
    }

    public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
    {
    }
}