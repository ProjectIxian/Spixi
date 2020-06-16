using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using IXICore.Meta;
using SPIXI.Droid;
using SPIXI.Interfaces;
using System;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]


public class PlatformUtils : IPlatformUtils
{
    MediaPlayer ringtone = null;
    ToneGenerator toneGenerator = null;

    public System.IO.Stream getAsset(string path)
    {
        return MainActivity.Instance.Assets.Open(path);
    }

    public string getAssetsBaseUrl()
    {
        return "file:///android_asset/";
    }

    public string getAssetsPath()
    {
        throw new System.NotImplementedException();
    }

    public string getHtmlBaseUrl()
    {
        return SPIXI.Meta.Config.spixiUserFolder + "/html/";
    }

    public string getHtmlPath()
    {
        return SPIXI.Meta.Config.spixiUserFolder + "/html";
    }

    public void startRinging()
    {
        if(ringtone != null)
        {
            return;
        }

        try
        {
            bool ring = true;

            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                NotificationManager nm = (NotificationManager)MainActivity.Instance.GetSystemService(Context.NotificationService);
                InterruptionFilter int_filter = nm.CurrentInterruptionFilter;
                if (int_filter != InterruptionFilter.Priority && int_filter != InterruptionFilter.All)
                {
                    ring = false;
                }
            }

            AudioManager am = (AudioManager)MainActivity.Instance.GetSystemService(Context.AudioService);
            if (am.RingerMode != RingerMode.Normal)
            {
                ring = false;
            }

            MainActivity.Instance.VolumeControlStream = Stream.Ring;
            if (ring)
            {
                Android.Net.Uri rt_url = RingtoneManager.GetDefaultUri(RingtoneType.Ringtone);
                AudioAttributes aa = new AudioAttributes.Builder()
                                                        .SetUsage(AudioUsageKind.NotificationRingtone)
                                                        .Build();

                ringtone = MediaPlayer.Create(MainActivity.Instance, rt_url, null, aa, 0);
                ringtone.Looping = true;
                ringtone.Start();
            }
        }catch(Exception e)
        {
            Logging.error("Exception occured in startRinging: " + e);
            ringtone = null;
        }
    }

    public void stopRinging()
    {
        if (ringtone == null)
        {
            return;
        }

        ringtone.Stop();
        ringtone.Dispose();
        ringtone = null;
        MainActivity.Instance.VolumeControlStream = Stream.NotificationDefault;
    }

    public void startDialtone(DialtoneType type)
    {
        try
        {
            stopDialtone();
            Tone tone_type;
            int duration = -1;
            switch(type)
            {
                case DialtoneType.busy:
                    tone_type = Tone.SupBusy;
                    duration = 5000;
                    break;
                case DialtoneType.dialing:
                    tone_type = Tone.SupRingtone;
                    break;
                case DialtoneType.error:
                    tone_type = Tone.SupError;
                    duration = 5000;
                    break;
                default:
                    return;
            }
            AudioManager am = (AudioManager)MainActivity.Instance.GetSystemService(Context.AudioService);
            toneGenerator = new ToneGenerator(Stream.VoiceCall, am.GetStreamVolume(Stream.VoiceCall) * 50 / am.GetStreamMaxVolume(Stream.VoiceCall));
            toneGenerator.StartTone(tone_type, duration);
        }
        catch (Exception e)
        {
            Logging.error("Exception occured in startDialtone: " + e);
            toneGenerator = null;
        }
    }

    public void stopDialtone()
    {
        if (toneGenerator != null)
        {
            toneGenerator.StopTone();
            toneGenerator.Release();
            toneGenerator.Dispose();
            toneGenerator = null;
        }
    }
}