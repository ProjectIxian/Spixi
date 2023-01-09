using AVFoundation;

namespace Spixi
{
    public class SSpixiPermissions
    {
        public static void requestAudioRecordingPermissions()
        {
            AVAudioSession av_session = AVAudioSession.SharedInstance();
            if (av_session != null)
            {
                av_session.RequestRecordPermission(delegate (bool granted)
                {

                });
            }
        }
    }
}
