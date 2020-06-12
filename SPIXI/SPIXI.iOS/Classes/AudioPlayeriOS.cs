using AVFoundation;
using Foundation;
using IXICore.Meta;
using SPIXI.VoIP;
using System;
using System.Runtime.InteropServices;
using Xamarin.Forms;

[assembly: Dependency(typeof(AudioPlayeriOS))]

public class AudioPlayeriOS : IAudioPlayer, IAudioDecoderCallback
{
    private AVAudioEngine audioEngine = null;
    private AVAudioPlayerNode audioPlayer = null;
    private AVAudioConverter audioConverter = null;
    private AVAudioFormat inputAudioFormat = null;
    private AVAudioFormat outputAudioFormat = null;

    private IAudioDecoder audioDecoder = null;

    private bool running = false;

    int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
    int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
    int channels = SPIXI.Meta.Config.VoIP_channels;

    public AudioPlayeriOS()
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
        audioEngine = new AVAudioEngine();
        NSError error = new NSError();
        if(!AVAudioSession.SharedInstance().SetPreferredSampleRate(sampleRate, out error))
        {
            throw new Exception("Error setting preffered sample rate for player: " + error);
        }
        AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.InterruptSpokenAudioAndMixWithOthers);
        AVAudioSession.SharedInstance().SetActive(true);

        audioPlayer = new AVAudioPlayerNode();
        inputAudioFormat = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, sampleRate, (uint)channels, false);
        outputAudioFormat = new AVAudioFormat(AVAudioCommonFormat.PCMFloat32, sampleRate, (uint)channels, false);

        audioConverter = new AVAudioConverter(inputAudioFormat, outputAudioFormat);

        audioEngine.AttachNode(audioPlayer);
        audioEngine.Connect(audioPlayer, audioEngine.MainMixerNode, outputAudioFormat);
        audioEngine.Prepare();
        if(!audioEngine.StartAndReturnError(out error))
        {
            throw new Exception("Error starting playback audio engine: " + error);
        }
        audioPlayer.Play();
    }

    private void initDecoder(string codec)
    {
        switch (codec)
        {
            case "opus":
                initOpusDecoder();
                break;

            default:
                throw new Exception("Unknown player codec selected " + codec);
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

        AVAudioSession.SharedInstance().SetActive(false);

        if (audioPlayer != null)
        {
            try
            {
                audioPlayer.Stop();
                audioPlayer.Reset();
            }
            catch (Exception)
            {

            }

            audioPlayer.Dispose();
            audioPlayer = null;
        }

        if (audioDecoder != null)
        {
            audioDecoder.stop();
            audioDecoder.Dispose();
            audioDecoder = null;
        }

        if (audioEngine != null)
        {
            try
            {
                audioEngine.Stop();
                audioEngine.Reset();
            }
            catch (Exception)
            {

            }

            audioEngine.Dispose();
            audioEngine = null;
        }

        if(audioConverter != null)
        {
            try
            {
                audioConverter.Reset();
            }
            catch (Exception)
            {

            }

            audioConverter.Dispose();
            audioConverter = null;
        }
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
        byte[] decoded_bytes = audioDecoder.decode(data);
        if (decoded_bytes != null)
        {
            onDecodedData(decoded_bytes);
        }
    }

    public void onDecodedData(byte[] data)
    {
        if (!running)
        {
            return;
        }
        if (audioPlayer != null)
        {
            AVAudioPcmBuffer buffer = new AVAudioPcmBuffer(inputAudioFormat, (uint)data.Length);
            IntPtr int_data = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, int_data, data.Length);

            buffer.AudioBufferList.SetData(0, int_data, data.Length);
            buffer.FrameLength = (uint)data.Length/2;
            AVAudioPcmBuffer output_buffer = new AVAudioPcmBuffer(outputAudioFormat, (uint)data.Length);
            NSError error = new NSError();

            if (audioConverter.ConvertToBuffer(output_buffer, buffer, out error))
            {
                audioPlayer.ScheduleBuffer(output_buffer, null);
            }else
            {
                Logging.error("Error converting audio packet: " + error);
            }
            Marshal.FreeHGlobal(int_data);
            buffer.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}