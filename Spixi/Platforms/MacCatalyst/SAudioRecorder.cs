﻿using AudioToolbox;
using AVFoundation;
using Foundation;
using IXICore.Meta;
using SPIXI.VoIP;
using System.Runtime.InteropServices;

namespace Spixi
{
    public class SAudioRecorder : IAudioRecorder, IAudioEncoderCallback
    {
        private Action<byte[]> OnSoundDataReceived;

        private AVAudioEngine audioRecorder = null;
        private IAudioEncoder audioEncoder = null;

        bool running = false;

        List<byte[]> outputBuffers = new List<byte[]>();

        Thread recordThread = null;

        int sampleRate = SPIXI.Meta.Config.VoIP_sampleRate;
        int bitRate = SPIXI.Meta.Config.VoIP_bitRate;
        int channels = SPIXI.Meta.Config.VoIP_channels;

        private static SAudioRecorder _singletonInstance;
        public static SAudioRecorder Instance()
        {
            if (_singletonInstance == null)
            {
                _singletonInstance = new SAudioRecorder();
            }
            return _singletonInstance;
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

            initEncoder(codec);
            initRecorder();

            recordThread = new Thread(recordLoop);
            recordThread.Start();
        }

        private void initRecorder()
        {
            audioRecorder = new AVAudioEngine();
            NSError error = new NSError();
            if (!AVAudioSession.SharedInstance().SetPreferredSampleRate(sampleRate, out error))
            {
                throw new Exception("Error setting preffered sample rate for recorder: " + error);
            }
            AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.InterruptSpokenAudioAndMixWithOthers);
            AVAudioSession.SharedInstance().SetActive(true);
            AVAudioFormat recording_format = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, sampleRate, (uint)channels, false);
            uint buffer_size = (uint)CodecTools.getPcmFrameByteSize(sampleRate, bitRate, channels) * 1000;
            audioRecorder.InputNode.InstallTapOnBus(0, buffer_size, recording_format, onDataAvailable);
            audioRecorder.Prepare();
            if (!audioRecorder.StartAndReturnError(out error))
            {
                throw new Exception("Error starting recording audio engine: " + error);
            }
        }

        private void onDataAvailable(AVAudioPcmBuffer buffer, AVAudioTime when)
        {
            AudioBuffer audioBuffer = buffer.AudioBufferList[0];
            byte[] data = new byte[audioBuffer.DataByteSize];
            Marshal.Copy(audioBuffer.Data, data, 0, audioBuffer.DataByteSize);

            encode(data, 0, data.Length);
        }

        private void initEncoder(string codec)
        {
            switch (codec)
            {
                case "opus":
                    initOpusEncoder();
                    break;

                default:
                    throw new Exception("Unknown recorder codec selected " + codec);
            }
        }

        private void initOpusEncoder()
        {
            audioEncoder = new OpusEncoder(sampleRate, 24000, channels, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP, this);
            audioEncoder.start();
        }

        public void stop()
        {
            if (!running)
            {
                return;
            }
            running = false;

            AVAudioSession.SharedInstance().SetActive(false);

            if (audioRecorder != null)
            {
                try
                {
                    audioRecorder.InputNode.RemoveTapOnBus(0);
                    audioRecorder.Stop();
                    audioRecorder.Reset();
                }
                catch (Exception)
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

            lock (outputBuffers)
            {
                outputBuffers.Clear();
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

        public void setOnSoundDataReceived(Action<byte[]> on_sound_data_received)
        {
            OnSoundDataReceived = on_sound_data_received;
        }

        private void recordLoop()
        {
            while (running)
            {
                try
                {
                    sendAvailableData();
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured while recording audio stream: " + e);
                }
                Thread.Sleep(10);
            }
            recordThread = null;
        }

        private void encode(byte[] buffer, int offset, int size)
        {
            if (!running)
            {
                return;
            }
            if (size > 0)
            {
                try
                {
                    audioEncoder.encode(buffer, offset, size);
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in encode loop: " + e);
                }
            }
        }

        private void sendAvailableData()
        {
            if (!running)
            {
                return;
            }
            byte[] data_to_send = null;
            lock (outputBuffers)
            {
                int total_size = 0;
                foreach (var buf in outputBuffers)
                {
                    total_size += buf.Length;
                }

                if (total_size >= 300)
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
            if (!running)
            {
                return;
            }
            lock (outputBuffers)
            {
                outputBuffers.Add(data);
            }
        }

    }
}
