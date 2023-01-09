using Android;
using AndroidX.Core.Content;
using IXICore.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spixi
{
    public class SSpixiPermissions
    {
        static int recordAudioPermissionRequest = 1;

        public static void requestAudioRecordingPermissions()
        {
            try
            {
                if (PermissionChecker.CheckSelfPermission(MainActivity.Instance, Manifest.Permission.RecordAudio) != PermissionChecker.PermissionGranted)
                {
                    MainActivity.Instance.RequestPermissions(new string[] { Manifest.Permission.RecordAudio }, recordAudioPermissionRequest);
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while requesting permissions for audio recording: " + e);
            }
        }
    }
}
