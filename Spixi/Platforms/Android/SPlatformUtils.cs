using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Net.Wifi;
using Android.OS;
using IXICore.Meta;
using Microsoft.Extensions.Logging.Abstractions;
using SPIXI.Interfaces;

namespace Spixi
{
    public class SPlatformUtils
    {
        static MediaPlayer ringtone = null;
        static ToneGenerator toneGenerator = null;

        public static System.IO.Stream getAsset(string path)
        {
            //return MainActivity.Instance.Assets.Open(path);
            /*#if WINDOWS
                        var stream = await Microsoft.Maui.Essentials.FileSystem.OpenAppPackageFileAsync("Assets/" + filePath);
            #else
                        var stream = await Microsoft.Maui.Essentials.FileSystem.OpenAppPackageFileAsync(filePath);
            #endif*/
            Task<System.IO.Stream> task = Task.Run<System.IO.Stream>(async () => await FileSystem.Current.OpenAppPackageFileAsync(path));
            return task.Result;
        }

        public static string getAssetsBaseUrl()
        {
            return "file:///android_asset/";
        }

        public static string getAssetsPath()
        {
            throw new System.NotImplementedException();
        }

        public static string getHtmlBaseUrl()
        {
            return SPIXI.Meta.Config.spixiUserFolder + "/html/";
        }

        public static string getHtmlPath()
        {
            return SPIXI.Meta.Config.spixiUserFolder + "/html";
        }

        public static void startRinging()
        {
            if (ringtone != null)
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

                MainActivity.Instance.VolumeControlStream = Android.Media.Stream.Ring;
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
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in startRinging: " + e);
                ringtone = null;
            }
        }

        public static void stopRinging()
        {
            if (ringtone == null)
            {
                return;
            }

            ringtone.Stop();
            ringtone.Dispose();
            ringtone = null;
            MainActivity.Instance.VolumeControlStream = Android.Media.Stream.NotificationDefault;
        }

        public static void startDialtone(DialtoneType type)
        {
            try
            {
                stopDialtone();
                Tone tone_type;
                int duration = -1;
                switch (type)
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
                toneGenerator = new ToneGenerator(Android.Media.Stream.VoiceCall, am.GetStreamVolume(Android.Media.Stream.VoiceCall) * 50 / am.GetStreamMaxVolume(Android.Media.Stream.VoiceCall));
                toneGenerator.StartTone(tone_type, duration);
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in startDialtone: " + e);
                toneGenerator = null;
            }
        }

        public static void stopDialtone()
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

}
