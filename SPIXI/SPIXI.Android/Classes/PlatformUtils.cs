using Android.Content;
using Android.Media;
using SPIXI.Droid;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]


public class PlatformUtils : IPlatformUtils
{
    Ringtone ringtone = null;
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

        ringtone = RingtoneManager.GetRingtone(MainActivity.Instance, RingtoneManager.GetDefaultUri(RingtoneType.Ringtone));
        ringtone.Looping = true;
        ringtone.Play();
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
    }

    public void startDialtone(DialtoneType type)
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
                duration = 60000;
                break;
            case DialtoneType.error:
                tone_type = Tone.SupError;
                duration = 5000;
                break;
            default:
                return;
        }
        AudioManager am = (AudioManager)MainActivity.Instance.GetSystemService(Context.AudioService);
        toneGenerator = new ToneGenerator(Stream.VoiceCall, am.GetStreamVolume(Stream.VoiceCall));
        toneGenerator.StartTone(tone_type, duration);
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