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

    int bufferSize = 0;
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

        initRecorder();
        initEncoder(codec);
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
            bufferSize * 2
        );

        audioRecorder.StartRecording();
    }

    private void initEncoder(string codec)
    {
        MediaFormat format = new MediaFormat();

        switch (codec)
        {
            case "amrnb":
                audioEncoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAmrNb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrNb);
                format.SetInteger(MediaFormat.KeySampleRate, 8000);
                format.SetInteger(MediaFormat.KeyBitRate, 7950);
                break;

            case "amrwb":
                audioEncoder = MediaCodec.CreateEncoderByType(MediaFormat.MimetypeAudioAmrWb);
                format.SetString(MediaFormat.KeyMime, MediaFormat.MimetypeAudioAmrWb);
                format.SetInteger(MediaFormat.KeySampleRate, 16000);
                format.SetInteger(MediaFormat.KeyBitRate, 18250);
                break;

            default:
                throw new Exception("Unknown recorder codec selected " + codec);
        }

        format.SetInteger(MediaFormat.KeyChannelCount, 1);
        format.SetInteger(MediaFormat.KeyMaxInputSize, bufferSize);
        audioEncoder.SetCallback(this);
        audioEncoder.Configure(format, null, null, MediaCodecConfigFlags.Encode);
        audioEncoder.Start();
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
            try
            {
                audioEncoder.Stop();
                audioEncoder.Release();
            }
            catch (Exception)
            {

            }
            audioEncoder.Dispose();
            audioEncoder = null;
        }

        buffer = null;
        bufferSize = 0;
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
        stop();
        base.Dispose();
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
        try
        {
            var ob = audioEncoder.GetOutputBuffer(index);

            ob.Position(info.Offset);
            ob.Limit(info.Offset + info.Size);


            if (outputBuffer == null)
            {
                outputBuffer = new byte[info.Size * 3];
                outputBufferFrameCount = 0;
            }

            byte[] data_to_send = null;

            lock (outputBuffer)
            {
                ob.Get(outputBuffer, outputBufferFrameCount * info.Size, info.Size);
                audioEncoder.ReleaseOutputBuffer(index, false);

                outputBufferFrameCount++;

                if (outputBufferFrameCount == 3)
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
        catch (Exception e)
        {
            Logging.error("Exception occured while recording audio stream: " + e);
        }
    }

    public override void OnOutputFormatChanged(MediaCodec codec, MediaFormat format)
    {
    }
}